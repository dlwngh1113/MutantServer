﻿using System;
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
        Socket listenSocket;            // the socket used to listen for incoming connection requests
                                        // pool of reusable SocketAsyncEventArgs objects for write, read and accept socket operations
        SocketAsyncEventArgsPool m_readWritePool;
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

            m_readWritePool = new SocketAsyncEventArgsPool(numConnections);
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
            SocketAsyncEventArgs readWriteEventArg;

            for (int i = 0; i < m_numConnections; i++)
            {
                //Pre-allocate a set of reusable SocketAsyncEventArgs
                readWriteEventArg = new SocketAsyncEventArgs();
                readWriteEventArg.Completed += new EventHandler<SocketAsyncEventArgs>(IO_Completed);
                readWriteEventArg.UserToken = new AsyncUserToken();

                // assign a byte buffer from the buffer pool to the SocketAsyncEventArg object
                m_bufferManager.SetBuffer(readWriteEventArg);

                // add SocketAsyncEventArg to the pool
                m_readWritePool.Push(readWriteEventArg);
            }
        }

        // Starts the server such that it is listening for
        // incoming connection requests.
        //
        // <param name="localEndPoint">The endpoint which the server will listening
        // for connection requests on</param>
        public void Start(IPEndPoint localEndPoint)
        {
            // create the socket which listens for incoming connections
            listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            listenSocket.Bind(localEndPoint);
            // start the server with a listen backlog of 100 connections
            listenSocket.Listen(100);

            // post accepts on the listening socket
            StartAccept(null);

            //Console.WriteLine("{0} connected sockets with one outstanding receive posted to each....press any key", m_outstandingReadCount);
            Console.WriteLine("Press any key to terminate the server process....");
            Console.ReadKey();
        }

        // Begins an operation to accept a connection request from the client
        //
        // <param name="acceptEventArg">The context object to use when issuing
        // the accept operation on the server's listening socket</param>
        public void StartAccept(SocketAsyncEventArgs acceptEventArg)
        {
            if (acceptEventArg == null)
            {
                acceptEventArg = new SocketAsyncEventArgs();
                acceptEventArg.Completed += new EventHandler<SocketAsyncEventArgs>(AcceptEventArg_Completed);
                acceptEventArg.UserToken = new AsyncUserToken(listenSocket);
            }
            else
            {
                // socket must be cleared since the context object is being reused
                acceptEventArg.AcceptSocket = null;
            }

            bool willRaiseEvent = listenSocket.AcceptAsync(acceptEventArg);
            if (!willRaiseEvent)
            {
                ProcessAccept(acceptEventArg);
            }
        }

        // This method is the callback method associated with Socket.AcceptAsync
        // operations and is invoked when an accept operation is complete
        //
        void AcceptEventArg_Completed(object sender, SocketAsyncEventArgs e)
        {
            ProcessAccept(e);
        }

        private void ProcessAccept(SocketAsyncEventArgs e)
        {
            Interlocked.Increment(ref m_numConnectedSockets);
            Console.WriteLine("Client connection accepted. There are {0} clients connected to the server",
                m_numConnectedSockets);

            // Get the socket for the accepted client connection and put it into the
            //ReadEventArg object user token
            Client player = new Client(MutantGlobal.id);
            Interlocked.Increment(ref MutantGlobal.id);
            player.socketAsyncEventArgs = m_readWritePool.Pop();
            ((AsyncUserToken)player.socketAsyncEventArgs.UserToken).socket = e.AcceptSocket;
            lock (players)
            {
                players.Add(e.AcceptSocket, player);
            }

            // As soon as the client is connected, post a receive to the connection
            bool willRaiseEvent = e.AcceptSocket.ReceiveAsync(player.socketAsyncEventArgs);
            if (!willRaiseEvent)
            {
                ProcessReceive(player.socketAsyncEventArgs);
            }

            // Accept the next connection request
            StartAccept(e);
        }

        // This method is called whenever a receive or send operation is completed on a socket
        //
        // <param name="e">SocketAsyncEventArg associated with the completed receive operation</param>
        void IO_Completed(object sender, SocketAsyncEventArgs e)
        {
            // determine which type of operation just completed and call the associated handler
            switch (e.LastOperation)
            {
                case SocketAsyncOperation.Receive:
                    ProcessReceive(e);
                    break;
                case SocketAsyncOperation.Send:
                    ProcessSend(e);
                    break;
                default:
                    throw new ArgumentException("The last operation completed on the socket was not a receive or send");
            }
        }

        // This method is invoked when an asynchronous receive operation completes.
        // If the remote host closed the connection, then the socket is closed.
        // If data was received then the data is echoed back to the client.
        private void ProcessReceive(SocketAsyncEventArgs e)
        {
            // check if the remote host closed the connection
            AsyncUserToken token = (AsyncUserToken)e.UserToken;
            if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success)
            {
                //increment the count of the total bytes receive by the server
                Interlocked.Add(ref m_totalBytesRead, e.BytesTransferred);
                Console.WriteLine("The server has read a total of {0} bytes", m_totalBytesRead);

                switch (token.operation)
                {
                    case MutantGlobal.CTOS_LOGIN:
                        ProcessLogin(e);
                        break;
                    case MutantGlobal.CTOS_STATE_CHANGE:
                        ProcessState(e);
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
        // This method is invoked when an asynchronous send operation completes.
        // The method issues another receive on the socket to read any additional
        // data sent from the client
        //
        // <param name="e"></param>
        private void ProcessSend(SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success)
            {
                // done echoing data back to the client
                AsyncUserToken token = (AsyncUserToken)e.UserToken;
                // read the next block of data send from the client
                bool willRaiseEvent = token.socket.ReceiveAsync(e);
                if (!willRaiseEvent)
                {
                    ProcessReceive(e);
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
            token.socket.Close();

            // decrement the counter keeping track of the total number of clients connected to the server
            Interlocked.Decrement(ref m_numConnectedSockets);

            // Free the SocketAsyncEventArg so they can be reused by another client
            m_readWritePool.Push(e);

            Console.WriteLine("A client has been disconnected from the server. There are {0} clients connected to the server", m_numConnectedSockets);
        }
        private void ProcessState(SocketAsyncEventArgs e)
        {
            AsyncUserToken token = (AsyncUserToken)e.UserToken;
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
            byte[] ary = new byte[e.BytesTransferred];
            Array.Copy(ary, 0, e.Buffer, e.Offset, e.BytesTransferred);
            string name = ary.ToString();
            players[token.socket].userName = name;

            token.operation = MutantGlobal.STOC_LOGIN_OK;
            ary = System.Text.Encoding.UTF8.GetBytes("로그인 성공!");
            e.SetBuffer(ary, 0, ary.Length);
            token.socket.SendAsync(e);
        }
        private void ProcessAttack(SocketAsyncEventArgs e)
        {
            AsyncUserToken token = (AsyncUserToken)e.UserToken;
            //누가 어떤 플레이어를 공격했는가?
            //공격당한 플레이어를 죽게 하고 공격한 플레이어 타이머 리셋
        }
        private void ProcessChatting(SocketAsyncEventArgs e)
        {
            AsyncUserToken token = (AsyncUserToken)e.UserToken;
            //클라이언트에서 온 메세지를 모든 클라이언트에 전송
        }
        private void ProcessLogout(SocketAsyncEventArgs e)
        {
            AsyncUserToken token = (AsyncUserToken)e.UserToken;
            //현재까지의 게임 정보를 DB에 업데이트 후 접속 종료
            CloseClientSocket(e);
        }
    }
}
