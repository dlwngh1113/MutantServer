using System;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Threading;

namespace mutant_server
{
    class Server
    {
        private int m_numConnections;   // the maximum number of connections the sample is designed to handle simultaneously
        private int m_receiveBufferSize;// buffer size to use for each socket I/O operation
        BufferManager m_bufferManager;  // represents a large reusable set of buffers for all socket operations
        const int opsToPreAlloc = 2;    // read, write (don't alloc buffer space for accepts)
        Listener listener;

        // pool of reusable SocketAsyncEventArgs objects for write, read and accept socket operations
        SocketAsyncEventArgsPool m_readPool;
        SocketAsyncEventArgsPool m_writePool;
        int m_totalBytesRead;           // counter of the total # bytes received by the server
        int m_numConnectedSockets;      // the total number of clients connected to the server

        Dictionary<Socket, Client> players;

        // Create an uninitialized server instance.
        // To start the server listening for connection requests
        // call the Init method followed by Start method
        //
        // <param name="numConnections">the maximum number of connections the sample is designed to handle simultaneously</param>
        // <param name="receiveBufferSize">buffer size to use for each socket I/O operation</param>
        public Server(int numConnections, int receiveBufferSize)
        {
            m_totalBytesRead = 0;
            m_numConnectedSockets = 0;
            m_numConnections = numConnections;
            m_receiveBufferSize = receiveBufferSize;
            players = new Dictionary<Socket, Client>();
            // allocate buffers such that the maximum number of sockets can have one outstanding read and
            //write posted to the socket simultaneously
            m_bufferManager = new BufferManager(receiveBufferSize * numConnections * opsToPreAlloc,
                receiveBufferSize);

            m_readPool = new SocketAsyncEventArgsPool(numConnections);
            m_writePool = new SocketAsyncEventArgsPool(numConnections);
        }

        // Initializes the server by preallocating reusable buffers and
        // context objects.  These objects do not need to be preallocated
        // or reused, but it is done this way to illustrate how the API can
        // easily be used to create reusable objects to increase server performance.
        //
        public void Init()
        {
            // Allocates one large byte buffer which all I/O operations use a piece of.  This gaurds
            // against memory fragmentation
            m_bufferManager.InitBuffer();

            // preallocate pool of SocketAsyncEventArgs objects
            for (int i = 0; i < m_numConnections; i++)
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
                    m_bufferManager.SetBuffer(readEventArg);

                    // add SocketAsyncEventArg to the pool
                    m_readPool.Push(readEventArg);
                }

                //write pool
                {
                    SocketAsyncEventArgs writeEventArg;
                    //Pre-allocate a set of reusable SocketAsyncEventArgs
                    writeEventArg = new SocketAsyncEventArgs();
                    writeEventArg.Completed += new EventHandler<SocketAsyncEventArgs>(SendCompleted);
                    writeEventArg.UserToken = token;

                    // assign a byte buffer from the buffer pool to the SocketAsyncEventArg object
                    m_bufferManager.SetBuffer(writeEventArg);

                    // add SocketAsyncEventArg to the pool
                    m_writePool.Push(writeEventArg);
                }
            }
        }

        // Starts the server such that it is listening for
        // incoming connection requests.
        //
        // <param name="localEndPoint">The endpoint which the server will listening
        // for connection requests on</param>
        public void Start(IPEndPoint localEndPoint)
        {
            listener = new Listener(localEndPoint);
            listener.Accept_Callback = new EventHandler<SocketAsyncEventArgs>(AcceptEventArg_Completed);
            listener.myDelegate = new Listener.AcceptDelegate(ProcessAccept);
            listener.StartAccept(null);

            //Console.WriteLine("{0} connected sockets with one outstanding receive posted to each....press any key", m_outstandingReadCount);
            Console.WriteLine("Press any key to terminate the server process....");
            Console.ReadKey();
        }
        void AcceptEventArg_Completed(object sender, SocketAsyncEventArgs e)
        {
            ProcessAccept(e);
        }

        private void ProcessAccept(SocketAsyncEventArgs e)
        {
            Interlocked.Increment(ref m_numConnectedSockets);
            Console.WriteLine("Client connection accepted. There are {0} clients connected to the server",
                m_numConnectedSockets);

            SocketAsyncEventArgs recv_event = m_readPool.Pop();
            SocketAsyncEventArgs send_event = m_writePool.Pop();
            BeginIO(e.AcceptSocket, recv_event, send_event);

            listener.StartAccept(e);
        }

        private void BeginIO(Socket socket, SocketAsyncEventArgs recv_event, SocketAsyncEventArgs send_event)
        {
            // Get the socket for the accepted client connection and put it into the
            //ReadEventArg object user token
            Client player = new Client(MutantGlobal.id);
            Interlocked.Increment(ref MutantGlobal.id);

            AsyncUserToken token = recv_event.UserToken as AsyncUserToken;
            token.socket = socket;
            token.readEventArgs = recv_event;
            token.writeEventArgs = send_event;
            player.asyncUserToken = token;

            lock (players)
            {
                players.Add(socket, player);
            }

            // As soon as the client is connected, post a receive to the connection
            bool willRaiseEvent = socket.ReceiveAsync(recv_event);
            if (!willRaiseEvent)
            {
                ProcessReceive(recv_event);
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
                switch (token.readEventArgs.Buffer[e.Offset])
                {
                    case MutantGlobal.CTOS_LOGIN:
                        ProcessLogin(e);
                        break;
                    case MutantGlobal.CTOS_STATUS_CHANGE:
                        ProcessStatus(e);
                        break;
                    case MutantGlobal.CTOS_ATTACK:
                        ProcessAttack(e);
                        break;
                    case MutantGlobal.CTOS_CHAT:
                        ProcessChatting(e);
                        break;
                    case MutantGlobal.CTOS_LOGOUT:
                        ProcessLogout(e);
                        break;
                    default:
                        throw new Exception("operation from client is not valid\n");
                }
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

                bool willRaiseEvent = token.socket.ReceiveAsync(token.readEventArgs);
                if (!willRaiseEvent)
                {
                    ProcessReceive(token.readEventArgs);
                }
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
                token.socket.Shutdown(SocketShutdown.Send);
            }
            // throws if client process has already closed
            catch (Exception) { }
            lock(players)
            {
                players.Remove(token.socket);
            }
            token.socket.Close();

            // decrement the counter keeping track of the total number of clients connected to the server
            Interlocked.Decrement(ref m_numConnectedSockets);

            // Free the SocketAsyncEventArg so they can be reused by another client
            m_readPool.Push(token.readEventArgs);
            m_writePool.Push(token.writeEventArgs);

            Console.WriteLine("A client has been disconnected from the server. There are {0} clients connected to the server", m_numConnectedSockets);
        }

        private void ProcessStatus(SocketAsyncEventArgs e)
        {
            AsyncUserToken token = e.UserToken as AsyncUserToken;

            PlayerStatusPacket packet = new PlayerStatusPacket(token.writeEventArgs.Buffer, token.writeEventArgs.Offset);
            packet.ByteArrayToPacket();

            if(0 < packet.position.X + packet.posVel.X &&
                packet.position.X + packet.posVel.X < 800)
            {
                packet.position.X += packet.posVel.X;
            }
            if(0 < packet.position.Z + packet.posVel.Z &&
                packet.position.Z + packet.posVel.Z < 600)
            {
                packet.position.Z += packet.posVel.Z;
            }

            packet.PacketToByteArray(MutantGlobal.STOC_STATUS_CHANGE);
            bool willRaise = token.socket.SendAsync(token.writeEventArgs);
            if(!willRaise)
            {
                ProcessSend(token.writeEventArgs);
            }
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
            Console.WriteLine("{0} client has {1} id, login request!",
                packet.name, packet.id);

            MutantPacket sendPacket = new MutantPacket(token.writeEventArgs.Buffer, token.writeEventArgs.Offset);
            sendPacket.id = m_numConnectedSockets;
            sendPacket.name = packet.name;
            sendPacket.time = packet.time;
            sendPacket.PacketToByteArray(MutantGlobal.STOC_LOGIN_OK);

            bool willRaise = token.socket.SendAsync(token.writeEventArgs);
            if (!willRaise)
            {
                ProcessSend(token.writeEventArgs);
            }
        }

        private void ProcessAttack(SocketAsyncEventArgs e)
        {
            AsyncUserToken token = (AsyncUserToken)e.UserToken;
            //누가 어떤 플레이어를 공격했는가?
            //공격당한 플레이어를 죽게 하고 공격한 플레이어 타이머 리셋
            bool willRaise = token.socket.ReceiveAsync(e);
            if(!willRaise)
            {
                ProcessReceive(e);
            }
        }

        private void ProcessChatting(SocketAsyncEventArgs e)
        {
            AsyncUserToken token = (AsyncUserToken)e.UserToken;
            //클라이언트에서 온 메세지를 모든 클라이언트에 전송
            byte[] tmp = e.Buffer;
            tmp[e.Offset] = MutantGlobal.STOC_CHAT;
            token.writeEventArgs.SetBuffer(token.writeEventArgs.Offset, token.writeEventArgs.BytesTransferred);

            bool willRaise = token.socket.SendAsync(token.writeEventArgs);
            if (!willRaise)
            {
                ProcessSend(token.writeEventArgs);
            }
        }

        private void ProcessLogout(SocketAsyncEventArgs e)
        {
            AsyncUserToken token = (AsyncUserToken)e.UserToken;
            //현재까지의 게임 정보를 DB에 업데이트 후 접속 종료
            CloseClientSocket(e);
        }
    }
}