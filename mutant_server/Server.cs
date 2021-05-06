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

        static public Dictionary<int, Client> players = new Dictionary<int, Client>();
        static public byte[] jobArray = Defines.GenerateRandomJobs();
        static public byte jobOffset = 0;

        public Server(int numConnections, int receiveBufferSize)
        {
            _numConnectedSockets = 0;
            _numConnections = numConnections;
            _receiveBufferSize = receiveBufferSize;
            _bufferManager = new BufferManager(_receiveBufferSize * numConnections * opsToPreAlloc,
                _receiveBufferSize);

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
                token.ResolveMessage(e.Buffer, e.Offset, e.BytesTransferred);

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
            lock (players)
            {
                players.Remove(token.userID);
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

    }
}