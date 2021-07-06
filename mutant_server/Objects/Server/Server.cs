using mutant_server.Objects.Networking;
using mutant_server.Packets;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace mutant_server
{
    class Server
    {
        private int _numConnections;   // the maximum number of connections the sample is designed to handle simultaneously
        private int _receiveBufferSize;// buffer size to use for each socket I/O operation
        private BufferManager _bufferManager;  // represents a large reusable set of buffers for all socket operations
        private const int opsToPreAlloc = 2;    // read, write (don't alloc buffer space for accepts)
        private Listener _listener;

        // pool of reusable SocketAsyncEventArgs objects for write, read and accept socket operations
        private SocketAsyncEventArgsPool _readPool;
        private SocketAsyncEventArgsPool _writePool;
        private int _numConnectedSockets;      // the total number of clients connected to the server

        private int _roomCount;
        private Dictionary<int, Room> _roomsInServer;
        private DBConnector _dBConnector;
        private Dictionary<int, Client> _players;

        public Server(int numConnections, int receiveBufferSize)
        {
            _numConnectedSockets = 0;
            _numConnections = numConnections;
            _receiveBufferSize = receiveBufferSize;
            _bufferManager = new BufferManager(_receiveBufferSize * numConnections * opsToPreAlloc,
                _receiveBufferSize);

            _readPool = new SocketAsyncEventArgsPool(numConnections);
            _writePool = new SocketAsyncEventArgsPool(numConnections);

            _roomCount = 0;
            _roomsInServer = new Dictionary<int, Room>();
            _dBConnector = new DBConnector();
            _players = new Dictionary<int, Client>();
        }

        public void Init()
        {
            _bufferManager.InitBuffer();

            for (int i = 0; i < _numConnections; i++)
            {
                AsyncUserToken token = new AsyncUserToken();
                //read pool
                {
                    SocketAsyncEventArgs readEventArg;
                    //Pre-allocate a set of reusable SocketAsyncEventArgs
                    readEventArg = new SocketAsyncEventArgs();
                    readEventArg.Completed += new EventHandler<SocketAsyncEventArgs>(ReceiveCompleted);
                    readEventArg.UserToken = token;

                    // assign a byte buffer from the buffer pool to the SocketAsyncEventArg object
                    _bufferManager.SetBuffer(readEventArg);

                    // add SocketAsyncEventArg to the pool
                    _readPool.Push(readEventArg);
                }

                //write pool
                {
                    SocketAsyncEventArgs writeEventArg;
                    //Pre-allocate a set of reusable SocketAsyncEventArgs
                    writeEventArg = new SocketAsyncEventArgs();
                    writeEventArg.Completed += new EventHandler<SocketAsyncEventArgs>(SendCompleted);
                    writeEventArg.UserToken = token;

                    // assign a byte buffer from the buffer pool to the SocketAsyncEventArg object
                    _bufferManager.SetBuffer(writeEventArg);

                    // add SocketAsyncEventArg to the pool
                    _writePool.Push(writeEventArg);
                }
            }
        }

        public void Start(IPEndPoint localEndPoint)
        {
            _listener = new Listener(localEndPoint);
            _listener.Accept_Callback = new EventHandler<SocketAsyncEventArgs>(AcceptEventArg_Completed);
            _listener.myDelegate = ProcessAccept;
            _listener.StartAccept(null);

            Console.WriteLine("Press any key to terminate the server process....");
            Console.ReadKey();
        }
        void AcceptEventArg_Completed(object sender, SocketAsyncEventArgs e)
        {
            ProcessAccept(e);
        }

        private void ProcessAccept(SocketAsyncEventArgs e)
        {
            Interlocked.Increment(ref _numConnectedSockets);
            Console.WriteLine("Client connection accepted. There are {0} clients connected to the server",
                _numConnectedSockets);

            SocketAsyncEventArgs recv_event = _readPool.Pop();
            SocketAsyncEventArgs send_event = _writePool.Pop();

            BeginIO(e.AcceptSocket, recv_event, send_event);

            _listener.StartAccept(e);
        }
        private void BeginIO(Socket socket, SocketAsyncEventArgs recv_event, SocketAsyncEventArgs send_event)
        {
            AsyncUserToken token = recv_event.UserToken as AsyncUserToken;
            token.socket = socket;
            token.userID = 0;
            token.readEventArgs = recv_event;
            token.writeEventArgs = send_event;
            token.recvCallback = ProcessReceive;
            token.sendCallback = ProcessSend;
            token.closeMethod = CloseClientSocket;
            
            bool willRaiseEvent = token.socket.ReceiveAsync(token.readEventArgs);
            if (!willRaiseEvent)
            {
                ProcessReceive(token.readEventArgs);
            }
        }

        private void ReceiveCompleted(object sender, SocketAsyncEventArgs e)
        {
            ProcessReceive(e);
        }

        private void ProcessReceive(SocketAsyncEventArgs e)
        {
            AsyncUserToken token = (AsyncUserToken)e.UserToken;
            if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success)
            {
                switch((CTOS_OP)e.Buffer[e.Offset])
                {
                    case CTOS_OP.CTOS_LOGIN:
                        ProcessLogin(token);
                        break;
                    case CTOS_OP.CTOS_LOGOUT:
                        ProcessLogout(token);
                        break;
                    case CTOS_OP.CTOS_CREATE_ROOM:
                        ProcessCreateRoom(token);
                        break;
                    case CTOS_OP.CTOS_SELECT_ROOM:
                        ProcessSelectRoom(token);
                        break;
                    case CTOS_OP.CTOS_REFRESH_ROOMS:
                        ProcessRefreshRooms(token);
                        break;
                    case CTOS_OP.CTOS_CREATE_USER_INFO:
                        ProcessCreateUser(token);
                        break;
                    default:
                        ProcessInRoom(token);
                        break;
                }
                try
                {
                    bool willRaise = token.socket.ReceiveAsync(token.readEventArgs);
                    if (!willRaise)
                    {
                        ProcessReceive(token.readEventArgs);
                    }
                }
                catch (Exception ex) { }
            }
            else
            {
                CloseClientSocket(e);
            }
        }

        bool IsValidUser(LoginPacket packet)
        {
            if(_dBConnector.isValidData(packet))
            {
                return true;
            }
            return false;
        }
        private Client InitClient(AsyncUserToken token)
        {
            Interlocked.Increment(ref Defines.id);

            Client client = new Client(Defines.id);
            client.asyncUserToken = token;
            client.userID = Defines.id;
            token.userID = Defines.id;

            lock (_players)
            {
                _players[client.userID] = client;
            }

            return client;
        }

        private void ProcessInRoom(AsyncUserToken token)
        {
            foreach (var tuple in _roomsInServer)
            {
                if(tuple.Value.IsHavePlayer(token.userID))
                {
                    tuple.Value.ResolveMessge(token);
                    return;
                }
            }
        }

        private void ProcessCreateUser(AsyncUserToken token)
        {
            LoginPacket packet = new LoginPacket(token.readEventArgs.Buffer, token.readEventArgs.Offset);
            packet.ByteArrayToPacket();

            if( _dBConnector.InsertData(packet))
            {
                DBPacket sendPacket = new DBPacket(new byte[Defines.BUF_SIZE], 0);

                sendPacket.id = packet.id;
                sendPacket.name = packet.name;
                sendPacket.time = 0;

                sendPacket.isSuccess = true;
                sendPacket.message = "계정 생성이 정상적으로 처리되었습니다!";

                _players[packet.id].asyncUserToken.SendData(sendPacket);
            }
            else
            {
                DBPacket sendPacket = new DBPacket(new byte[Defines.BUF_SIZE], 0);
                sendPacket.id = packet.id;
                sendPacket.name = packet.name;
                sendPacket.time = 0;

                sendPacket.isSuccess = false;
                sendPacket.message = "이미 존재하는 아이디입니다!";

                _players[packet.id].asyncUserToken.SendData(sendPacket);
            }
        }

        private void ProcessLogin(AsyncUserToken token)
        {
            //if (DB에서 이미 플레이어의 이름이 존재하고, 서버에서 사용중이지 않다면)
            //해당 정보로 로그인 login ok 클라이언트로 전송
            //else if 서버, DB에 모두 존재하지 않는 이름이라면
            //새롭게 DB에 유저 정보를 입력하고 login ok 전송
            //else
            //login fail과 이미
            LoginPacket packet = new LoginPacket(token.readEventArgs.Buffer, token.readEventArgs.Offset);
            packet.ByteArrayToPacket();

            if (!IsValidUser(packet))
            {
                return;
            }

            Client c = InitClient(token);
            c.userName = packet.name;
            Console.WriteLine("{0} client has {1} id, login request!", c.userName, c.userID);

            MutantPacket sendPacket = new MutantPacket(new byte[Defines.BUF_SIZE], 0);
            sendPacket.id = c.userID;
            sendPacket.name = packet.name;
            sendPacket.time = packet.time;

            sendPacket.PacketToByteArray((byte)STOC_OP.STOC_LOGIN_OK);

            token.SendData(sendPacket);
        }
        private void CloseClientSocket(SocketAsyncEventArgs e)
        {
            AsyncUserToken token = e.UserToken as AsyncUserToken;

            // close the socket associated with the client
            try
            {
                token.socket.Shutdown(SocketShutdown.Both);
            }
            // throws if client process has already closed
            catch (Exception) { }
            lock (_players)
            {
                _players.Remove(token.userID);
            }
            token.socket.Close();

            // decrement the counter keeping track of the total number of clients connected to the server
            Interlocked.Decrement(ref _numConnectedSockets);

            // Free the SocketAsyncEventArg so they can be reused by another client
            _readPool.Push(token.readEventArgs);
            _writePool.Push(token.writeEventArgs);

            Console.WriteLine("A client has been disconnected from the server. There are {0} clients connected to the server", _numConnectedSockets);
        }

        private void SendCompleted(object sender, SocketAsyncEventArgs e)
        {
            ProcessSend(e);
        }
        private void ProcessSend(SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success)
            {
                AsyncUserToken token = (AsyncUserToken)e.UserToken;

                token.SendEnd();
            }
            else
            {
                CloseClientSocket(e);
            }
        }
        private void ProcessLogout(AsyncUserToken token)
        {
            //현재까지의 게임 정보를 DB에 업데이트 후 접속 종료

            //지금 게임에 존재하는 유저들에게 해당 유저가 게임을 종료했음을 알림
            //지금 게임을 같이 하고 있는 유저들을 어떻게 구분할 것인가?
            MutantPacket packet = new MutantPacket(token.readEventArgs.Buffer, token.readEventArgs.Offset);
            packet.ByteArrayToPacket();

            _dBConnector.UpdateData(_players[packet.id]);

            CloseClientSocket(token.readEventArgs);
        }

        private void ProcessRefreshRooms(AsyncUserToken token)
        {
            MutantPacket packet = new MutantPacket(token.readEventArgs.Buffer, token.readEventArgs.Offset);
            packet.ByteArrayToPacket();

            RoomPacket sendPacket = new RoomPacket(new byte[Defines.BUF_SIZE], 0);
            sendPacket.id = packet.id;
            sendPacket.name = packet.name;
            sendPacket.time = packet.time;

            foreach(var tuple in _roomsInServer)
            {
                sendPacket.roomList.Add(new KeyValuePair<int, int>(tuple.Key, tuple.Value.PlayerNum));
            }

            sendPacket.PacketToByteArray((byte)STOC_OP.STOC_ROOM_REFRESHED);
            token.SendData(sendPacket);
        }

        private void ProcessSelectRoom(AsyncUserToken token)
        {
            RoomPacket packet = new RoomPacket(token.readEventArgs.Buffer, token.readEventArgs.Offset);
            packet.ByteArrayToPacket();

            Room room = _roomsInServer[packet.roomList[0].Key];

            if(room.PlayerNum < 5)
            {
                room.AddPlayer(packet.id, _players[packet.id]);
                _players.Remove(packet.id);

                RoomPacket sendPacket = new RoomPacket(new byte[Defines.BUF_SIZE], 0);
                sendPacket.id = packet.id;
                sendPacket.name = packet.name;
                sendPacket.time = 0;

                sendPacket.roomList.Add(new KeyValuePair<int, int>(packet.roomList[0].Key, room.PlayerNum));
                sendPacket.PacketToByteArray((byte)STOC_OP.STOC_PLAYER_ENTER);

                token.SendData(sendPacket);
            }
            else
            {
                RoomPacket sendPacket = new RoomPacket(new byte[Defines.BUF_SIZE], 0);
                sendPacket.id = packet.id;
                sendPacket.name = packet.name;
                sendPacket.time = 0;

                sendPacket.roomList.Add(new KeyValuePair<int, int>(packet.roomList[0].Key, room.PlayerNum));
                sendPacket.PacketToByteArray((byte)STOC_OP.STOC_ENTER_FAIL);

                token.SendData(sendPacket);
            }
        }

        private void ProcessCreateRoom(AsyncUserToken token)
        {
            MutantPacket packet = new MutantPacket(token.readEventArgs.Buffer, token.readEventArgs.Offset);
            packet.ByteArrayToPacket();

            Room room = new Room();
            room.closeMethod = CloseClientSocket;
            lock (_roomsInServer)
            {
                _roomsInServer.Add(++_roomCount, room);
            }

            room.AddPlayer(packet.id, _players[packet.id]);
            _players.Remove(packet.id);

            RoomPacket sendPacket = new RoomPacket(new byte[Defines.BUF_SIZE], 0);
            sendPacket.id = packet.id;
            sendPacket.name = packet.name;
            sendPacket.time = 0;

            sendPacket.roomList.Add(new KeyValuePair<int, int>(_roomCount, 1));
            sendPacket.PacketToByteArray((byte)STOC_OP.STOC_ROOM_CREATE_SUCCESS);

            token.SendData(sendPacket);
        }
    }
}