using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace mutant_server
{
    class Listener
    {
        Socket listenSocket;
        public EventHandler<SocketAsyncEventArgs> Accept_Callback;

        public delegate void AcceptDelegate(SocketAsyncEventArgs e);
        public AcceptDelegate myDelegate;
        public Listener(IPEndPoint localEndPoint)
        {
            this.Init(localEndPoint);
        }
        private void Init(IPEndPoint localEndPoint)
        {
            // create the socket which listens for incoming connections
            listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            listenSocket.Bind(localEndPoint);
            // start the server with a listen backlog of 100 connections
            listenSocket.Listen(100);
        }
        public void StartAccept(SocketAsyncEventArgs acceptEventArg)
        {
            if (acceptEventArg == null)
            {
                acceptEventArg = new SocketAsyncEventArgs();
                acceptEventArg.Completed += Accept_Callback;
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
                myDelegate(acceptEventArg);
            }
        }

    }
}
