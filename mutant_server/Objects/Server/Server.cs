﻿using mutant_server.Objects.Networking;
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

        public static List<Room> _roomsInServer;
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

            _roomsInServer = new List<Room>();
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
                    case CTOS_OP.CTOS_GET_HISTORY:
                        ProcessUserInfo(token);
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
                foreach(var tuple in _players)
                {
                    if(tuple.Value.userName == packet.name)
                    {
                        return false;
                    }
                }
                return true;
            }
            return false;
        }

        private void ProcessInRoom(AsyncUserToken token)
        {
            foreach(var i in _roomsInServer)
            {
                if (i.IsHavePlayer(token.userID))
                {
                    i.ResolveMessage(token);
                    return;
                }
            }
        }

        private void ProcessUserInfo(AsyncUserToken token)
        {
            MutantPacket packet = new MutantPacket(token.readEventArgs.Buffer, token.readEventArgs.Offset);
            packet.ByteArrayToPacket();

            UserInfoPacket sendPacket = new UserInfoPacket(new byte[Defines.BUF_SIZE], 0);
            sendPacket.id = packet.id;
            sendPacket.name = _players[packet.id].userName;
            sendPacket.time = 0;
            sendPacket.winCountTrator = _players[packet.id].winCountTrator;
            sendPacket.winCountTanker = _players[packet.id].winCountTanker;
            sendPacket.winCountResearcher = _players[packet.id].winCountResearcher;
            sendPacket.winCountPsychy = _players[packet.id].winCountPsychy;
            sendPacket.winCountNocturn = _players[packet.id].winCountNocturn;

            sendPacket.playCountTrator = _players[packet.id].playCountTrator;
            sendPacket.playCountTanker = _players[packet.id].playCountTanker;
            sendPacket.playCountResearcher = _players[packet.id].playCountResearcher;
            sendPacket.playCountPsychy = _players[packet.id].playCountPsychy;
            sendPacket.playCountNocturn = _players[packet.id].playCountNocturn;

            sendPacket.PacketToByteArray((byte)STOC_OP.STOC_PROVISION_HISTORY);

            token.SendData(sendPacket);
        }

        private void ProcessCreateUser(AsyncUserToken token)
        {
            LoginPacket packet = new LoginPacket(token.readEventArgs.Buffer, token.readEventArgs.Offset);
            packet.ByteArrayToPacket();

            DBPacket sendPacket = new DBPacket(new byte[Defines.BUF_SIZE], 0);

            sendPacket.id = packet.id;
            sendPacket.name = packet.name;
            sendPacket.time = 0;

            if ( _dBConnector.InsertData(packet))
            {
                sendPacket.isSuccess = true;
                sendPacket.message = "계정 생성이 정상적으로 처리되었습니다!";
            }
            else
            {
                sendPacket.isSuccess = false;
                sendPacket.message = "이미 존재하는 아이디입니다!";
            }

            token.SendData(sendPacket);
        }

        private void ProcessLogin(AsyncUserToken token)
        {
            LoginPacket packet = new LoginPacket(token.readEventArgs.Buffer, token.readEventArgs.Offset);
            packet.ByteArrayToPacket();

            MutantPacket sendPacket = new MutantPacket(new byte[Defines.BUF_SIZE], 0);
            //서버에서 사용중이거나 잘못된 아이디, 비밀번호
            if (!IsValidUser(packet))
            {
                sendPacket.id = packet.id;
                sendPacket.name = packet.name;
                sendPacket.time = 0;

                sendPacket.PacketToByteArray((byte)STOC_OP.STOC_LOGIN_FAIL);

                token.SendData(sendPacket);
                return;
            }

            //정상 접속한 유저
            Client client = _dBConnector.GetUserData(packet.name, packet.passwd);
            client.asyncUserToken = token;

            token.userID = client.userID;
            
            lock(_players)
            {
                _players[client.userID] = client;
            }

            Console.WriteLine("{0} client has {1} id, login request!", client.userName, client.userID);

            sendPacket.id = client.userID;
            sendPacket.name = client.userName;
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

            Console.WriteLine("A client - {0} has been disconnected from the server. There are {1} clients connected to the server", token.userID, _numConnectedSockets);
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

            foreach(var room in _roomsInServer)
            {
                sendPacket.names.Add(room.RoomTitle);
                sendPacket.numOfPlayers.Add(room.PlayerNum);
                sendPacket.gameState.Add(room.GameState);
            }

            sendPacket.PacketToByteArray((byte)STOC_OP.STOC_ROOM_REFRESHED);
            token.SendData(sendPacket);
        }

        private void ProcessSelectRoom(AsyncUserToken token)
        {
            RoomPacket packet = new RoomPacket(token.readEventArgs.Buffer, token.readEventArgs.Offset);
            packet.ByteArrayToPacket();

            Room room = null;
            foreach (var v in _roomsInServer)
            {
                //패킷에 온 방 제목이 서버의 방 제목과 일치하는가?
                if(v.RoomTitle == packet.names[0])
                {
                    room = v;
                    break;
                }
            }
            if(room == null)
            {
                Console.WriteLine("room title is wrong(Server.cs)");
                return;
            }

            if(room.PlayerNum < 5)
            {
                room.AddPlayer(packet.id, _players[packet.id]);

                RoomPacket sendPacket = new RoomPacket(new byte[Defines.BUF_SIZE], 0);
                sendPacket.id = packet.id;
                sendPacket.name = packet.name;
                sendPacket.time = 0;

                sendPacket.names.Add(packet.names[0]);
                sendPacket.numOfPlayers.Add(room.PlayerNum);
                sendPacket.gameState.Add(Defines.ROOM_WAIT);
                sendPacket.PacketToByteArray((byte)STOC_OP.STOC_PLAYER_ENTER);

                token.SendData(sendPacket);
            }
            else
            {
                RoomPacket sendPacket = new RoomPacket(new byte[Defines.BUF_SIZE], 0);
                sendPacket.id = packet.id;
                sendPacket.name = packet.name;
                sendPacket.time = 0;

                sendPacket.names.Add(packet.names[0]);
                sendPacket.numOfPlayers.Add(room.PlayerNum);
                sendPacket.gameState.Add(Defines.ROOM_WAIT);
                sendPacket.PacketToByteArray((byte)STOC_OP.STOC_ROOM_ENTER_FAIL);

                token.SendData(sendPacket);
            }
        }

        private void ProcessCreateRoom(AsyncUserToken token)
        {
            RoomPacket packet = new RoomPacket(token.readEventArgs.Buffer, token.readEventArgs.Offset);
            packet.ByteArrayToPacket();

            Room room = new Room();
            room.closeMethod = CloseClientSocket;
            room.SetRoomTitle(packet.names[0]);
            lock (_roomsInServer)
            {
                _roomsInServer.Add(room);
            }

            room.AddPlayer(packet.id, _players[packet.id]);

            MutantPacket sendPacket = new MutantPacket(new byte[Defines.BUF_SIZE], 0);
            sendPacket.id = packet.id;
            sendPacket.name = packet.name;
            sendPacket.time = packet.time;

            sendPacket.PacketToByteArray((byte)STOC_OP.STOC_ROOM_CREATE_SUCCESS);

            token.SendData(sendPacket);
        }
    }
}