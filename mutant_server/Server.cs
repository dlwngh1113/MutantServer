using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
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
        const int opsToPreAlloc = 2;    // read, write (don't alloc buffer space for accepts)
        private Listener _listener;

        // pool of reusable SocketAsyncEventArgs objects for write, read and accept socket operations
        private SocketAsyncEventArgsPool _readPool;
        private SocketAsyncEventArgsPool _writePool;
        private int _numConnectedSockets;      // the total number of clients connected to the server

        private ConcurrentDictionary<int, Client> _players;

        public Server(int numConnections, int receiveBufferSize)
        {
            _numConnectedSockets = 0;
            _numConnections = numConnections;
            _receiveBufferSize = receiveBufferSize;
            _players = new ConcurrentDictionary<int, Client>();
            _bufferManager = new BufferManager(receiveBufferSize * numConnections * opsToPreAlloc,
                receiveBufferSize);

            _readPool = new SocketAsyncEventArgsPool(numConnections);
            _writePool = new SocketAsyncEventArgsPool(numConnections);
        }

        public void Init()
        {
            // Allocates one large byte buffer which all I/O operations use a piece of.  This gaurds
            // against memory fragmentation
            _bufferManager.InitBuffer();

            // preallocate pool of SocketAsyncEventArgs objects
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
            _listener.myDelegate = new Listener.AcceptDelegate(ProcessAccept);
            _listener.StartAccept();

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
        }
        private void BeginIO(Socket socket, SocketAsyncEventArgs recv_event, SocketAsyncEventArgs send_event)
        {
            AsyncUserToken token = recv_event.UserToken as AsyncUserToken;
            token.socket = socket;
            token.userID = 0;
            token.readEventArgs = recv_event;
            token.writeEventArgs = send_event;
            
            bool willRaiseEvent = socket.ReceiveAsync(token.readEventArgs);
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
                token.ResolveMessage(e.Buffer, e.Offset, e.BytesTransferred);
            }
            else
            {
                CloseClientSocket(e);
            }
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

                //try
                //{
                //    bool willRaiseEvent = token.socket.ReceiveAsync(token.readEventArgs);
                //    if (!willRaiseEvent)
                //    {
                //        ProcessReceive(token.readEventArgs);
                //    }
                //}
                //catch(Exception ex)
                //{
                //    Console.WriteLine(ex.ToString());
                //}
            }
            else
            {
                CloseClientSocket(e);
            }
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
            lock(_players)
            {
                Client tmp;
                _players.TryRemove(token.userID, out tmp);
            }
            token.socket.Close();

            // decrement the counter keeping track of the total number of clients connected to the server
            Interlocked.Decrement(ref _numConnectedSockets);

            // Free the SocketAsyncEventArg so they can be reused by another client
            _readPool.Push(token.readEventArgs);
            _writePool.Push(token.writeEventArgs);

            Console.WriteLine("A client has been disconnected from the server. There are {0} clients connected to the server", _numConnectedSockets);
        }

        private void ProcessStatus(SocketAsyncEventArgs e)
        {
            AsyncUserToken token = e.UserToken as AsyncUserToken;

            PlayerStatusPacket packet = new PlayerStatusPacket(token.readEventArgs.Buffer, token.readEventArgs.Offset);
            packet.ByteArrayToPacket();

            _players[packet.id].position = packet.position;
            _players[packet.id].rotation = packet.rotation;

            foreach (var tuple in _players)
            {
                if (tuple.Key != packet.id)
                {
                    var tmpToken = tuple.Value.asyncUserToken;
                    lock (tmpToken)
                    {
                        PlayerStatusPacket sendPacket = new PlayerStatusPacket(tmpToken.writeEventArgs.Buffer, tmpToken.writeEventArgs.Offset);
                        sendPacket.id = packet.id;
                        sendPacket.name = packet.name;
                        sendPacket.playerMotion = packet.playerMotion;
                        sendPacket.time = Defines.GetCurrentMilliseconds();
                        sendPacket.position = _players[packet.id].position;
                        sendPacket.rotation = _players[packet.id].rotation;

                        Console.WriteLine("id = {0} x = {1} y = {2} z = {3}", packet.id, packet.position.x, packet.position.y, packet.position.z);

                        sendPacket.PacketToByteArray(Defines.STOC_STATUS_CHANGE);

                        try
                        {
                            bool willRaise = tmpToken.socket.SendAsync(tmpToken.writeEventArgs);
                            if (!willRaise)
                            {
                                ProcessSend(tmpToken.writeEventArgs);
                            }
                        }
                        catch (Exception ex) { }
                    }
                }
            }

            try
            {
                bool willRaiseEvent = token.socket.ReceiveAsync(token.readEventArgs);
                if (!willRaiseEvent)
                {
                    ProcessReceive(token.readEventArgs);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        private void ProcessItemEvent(SocketAsyncEventArgs e)
        {
            AsyncUserToken token = e.UserToken as AsyncUserToken;
            ItemEventPacket packet = new ItemEventPacket(e.Buffer, e.Offset);
            packet.ByteArrayToPacket();
            var cnt = 0;
            //인벤토리에 존재하는 아이템의 개수 구하기
            foreach (var tuple in _players[packet.id].inventory)
            {
                cnt += tuple.Value;
            }

            ItemEventPacket sendPacket = new ItemEventPacket(token.writeEventArgs.Buffer, token.writeEventArgs.Offset);

            //만약 아이템의 총 개수가 3개보다 많다면 획득하지 못함
            if (cnt > 2)
            {
                sendPacket.id = packet.id;
                sendPacket.name = packet.name;
                sendPacket.itemName = packet.itemName;
                sendPacket.inventory = packet.inventory;
                sendPacket.canGainItem = false;
                sendPacket.PacketToByteArray(Defines.STOC_ITEM_DENIED);
            }
            //그렇지 않은 경우는 아이템 획득 가능
            else
            {
                //SendPacket.inventory Null Reference Exception
                if (_players[packet.id].inventory.ContainsKey(packet.itemName))
                {
                    _players[packet.id].inventory[packet.itemName] += 1;
                }
                else
                {
                    _players[packet.id].inventory.Add(packet.itemName, 1);
                }
                sendPacket.id = packet.id;
                sendPacket.name = packet.name;
                sendPacket.itemName = packet.itemName;
                sendPacket.inventory = _players[packet.id].inventory;
                sendPacket.canGainItem = true;
                sendPacket.PacketToByteArray(Defines.STOC_ITEM_GAIN);
            }

            foreach (var tuple in sendPacket.inventory)
            {
                Console.Write("key - {0}, value - {1}", tuple.Key, tuple.Value);
            }
            Console.WriteLine("");

            bool willRaiseEvent = token.socket.SendAsync(token.writeEventArgs);
            if (!willRaiseEvent)
            {
                ProcessSend(token.writeEventArgs);
            }

            try
            {
                willRaiseEvent = token.socket.ReceiveAsync(token.readEventArgs);
                if (!willRaiseEvent)
                {
                    ProcessReceive(token.readEventArgs);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        private Client InitClient(AsyncUserToken token)
        {
            if (_numConnectedSockets != (Defines.id + 1))
                return null;
            Interlocked.Increment(ref Defines.id);
            token.userID = Defines.id;

            Client client = new Client(Defines.id);
            client.position = new MyVector3(95.09579f, 4.16f, 42.68918f);
            client.rotation = new MyVector3();
            client.asyncUserToken = token;

            lock (_players)
            {
                _players[client.userID] = client;
            }

            return client;
        }

        private void ProcessLogin(SocketAsyncEventArgs e)
        {
            AsyncUserToken token = (AsyncUserToken)e.UserToken;
            //if (DB에서 이미 플레이어의 이름이 존재하고, 서버에서 사용중이지 않다면)
            //해당 정보로 로그인 login ok 클라이언트로 전송
            //else if 서버, DB에 모두 존재하지 않는 이름이라면
            //새롭게 DB에 유저 정보를 입력하고 login ok 전송
            //else
            //login fail과 이미
            MutantPacket packet = new MutantPacket(e.Buffer, e.Offset);
            packet.ByteArrayToPacket();

            Client c = InitClient(token);
            if(c == null)
                return;
            Console.WriteLine("{0} client has {1} id, login request!", c.userName, c.userID);

            PlayerStatusPacket sendPacket = new PlayerStatusPacket(token.writeEventArgs.Buffer, token.writeEventArgs.Offset);
            sendPacket.id = c.userID;
            sendPacket.name = packet.name;
            sendPacket.time = packet.time;
            sendPacket.position = c.position;

            sendPacket.PacketToByteArray(Defines.STOC_LOGIN_OK);

            bool willRaise = token.socket.SendAsync(token.writeEventArgs);
            if (!willRaise)
            {
                ProcessSend(token.writeEventArgs);
            }

            foreach (var tuple in _players)
            {
                if (tuple.Key != sendPacket.id)
                {
                    var tmpToken = tuple.Value.asyncUserToken;

                    //기존의 플레이어에게 새로운 플레이어 정보 전달
                    lock (tmpToken)
                    {
                        PlayerStatusPacket curPacket = new PlayerStatusPacket(tmpToken.writeEventArgs.Buffer, tmpToken.writeEventArgs.Offset);
                        curPacket.id = sendPacket.id;
                        curPacket.name = sendPacket.name;
                        curPacket.time = sendPacket.time;
                        curPacket.position = _players[sendPacket.id].position;

                        curPacket.PacketToByteArray(Defines.STOC_PLAYER_ENTER);

                        try
                        {
                            bool willRaiseEvent = tmpToken.socket.SendAsync(tmpToken.writeEventArgs);
                            if (!willRaiseEvent)
                            {
                                ProcessSend(tmpToken.writeEventArgs);
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.ToString());
                        }
                    }

                    //새로운 플레이어에게 기존의 플레이어 정보 전달
                    lock (token)
                    {
                        PlayerStatusPacket otherPacket = new PlayerStatusPacket(token.writeEventArgs.Buffer, token.writeEventArgs.Offset);
                        otherPacket.id = tuple.Key;
                        otherPacket.name = tuple.Value.userName;
                        otherPacket.position = tuple.Value.position;
                        otherPacket.rotation = tuple.Value.rotation;

                        otherPacket.PacketToByteArray(Defines.STOC_PLAYER_ENTER);

                        try
                        {
                            bool willRaiseEvent = token.socket.SendAsync(token.writeEventArgs);
                            if (!willRaiseEvent)
                            {
                                ProcessSend(token.writeEventArgs);
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.ToString());
                        }
                    }
                }
            }
            try
            {
                bool willRaiseEvent = token.socket.ReceiveAsync(token.readEventArgs);
                if (!willRaiseEvent)
                {
                    ProcessReceive(token.readEventArgs);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        private void ProcessAttack(SocketAsyncEventArgs e)
        {
            AsyncUserToken token = (AsyncUserToken)e.UserToken;
            //누가 어떤 플레이어를 공격했는가?
            //공격당한 플레이어를 죽게 하고 공격한 플레이어 타이머 리셋
            MutantPacket packet = new MutantPacket(e.Buffer, e.Offset);
            packet.ByteArrayToPacket();

            foreach (var tuple in _players)
            {
                var tmpToken = tuple.Value.asyncUserToken;

                //기존의 플레이어에게 새로운 플레이어 정보 전달
                lock (tmpToken)
                {
                    PlayerStatusPacket sendPacket = new PlayerStatusPacket(tmpToken.writeEventArgs.Buffer, tmpToken.writeEventArgs.Offset);
                    sendPacket.id = packet.id;
                    sendPacket.name = packet.name;
                    sendPacket.time = Defines.GetCurrentMilliseconds();

                    sendPacket.position = _players[packet.id].position;
                    sendPacket.rotation = _players[packet.id].rotation;
                    sendPacket.playerMotion = Defines.PLAYER_HIT;
                    sendPacket.PacketToByteArray(Defines.STOC_KILLED);

                    try
                    {
                        bool willRaiseEvent = tmpToken.socket.SendAsync(tmpToken.writeEventArgs);
                        if (!willRaiseEvent)
                        {
                            ProcessSend(tmpToken.writeEventArgs);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.ToString());
                    }
                }
            }

            try
            {
                bool willRaiseEvent = token.socket.ReceiveAsync(token.readEventArgs);
                if (!willRaiseEvent)
                {
                    ProcessReceive(token.readEventArgs);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        private void ProcessChatting(SocketAsyncEventArgs e)
        {
            AsyncUserToken token = (AsyncUserToken)e.UserToken;
            //클라이언트에서 온 메세지를 모든 클라이언트에 전송
            ChattingPakcet recvPacket = new ChattingPakcet(e.Buffer, e.Offset);
            ChattingPakcet sendPacket = new ChattingPakcet(token.writeEventArgs.Buffer, token.writeEventArgs.Offset);
            sendPacket.Copy(recvPacket);

            //같은 게임을 진행중인 모든 클라이언트에게 전송해야 한다.
            bool willRaise = token.socket.SendAsync(token.writeEventArgs);
            if (!willRaise)
            {
                ProcessSend(token.writeEventArgs);
            }
            try
            {
                bool willRaiseEvent = token.socket.ReceiveAsync(token.readEventArgs);
                if (!willRaiseEvent)
                {
                    ProcessReceive(token.readEventArgs);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        private void ProcessLogout(SocketAsyncEventArgs e)
        {
            AsyncUserToken token = (AsyncUserToken)e.UserToken;
            //현재까지의 게임 정보를 DB에 업데이트 후 접속 종료

            //지금 게임에 존재하는 유저들에게 해당 유저가 게임을 종료했음을 알림
            //지금 게임을 같이 하고 있는 유저들을 어떻게 구분할 것인가?
            CloseClientSocket(e);
            try
            {
                bool willRaiseEvent = token.socket.ReceiveAsync(token.readEventArgs);
                if (!willRaiseEvent)
                {
                    ProcessReceive(token.readEventArgs);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
    }
}