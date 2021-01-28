using mutant_server;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using System;
using System.Threading;

namespace StressClient
{
    class NetworkModule
    {
        int m_numConnectedSockets;
        Dictionary<Socket, Client> clients;
        public NetworkModule()
        {
            m_numConnectedSockets = 0;
            clients = new Dictionary<Socket, Client>();
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
            lock (clients)
            {
                clients.Remove(token.socket);
            }
            token.socket.Close();

            // decrement the counter keeping track of the total number of clients connected to the server
            Interlocked.Decrement(ref m_numConnectedSockets);
        }
        public void Run()
        {
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            SocketAsyncEventArgs args = new SocketAsyncEventArgs();
            args.RemoteEndPoint = new IPEndPoint(IPAddress.Loopback, MutantGlobal.PORT);
            args.Completed += new EventHandler<SocketAsyncEventArgs>(ConnectArgs_Completed);
            socket.ConnectAsync(args);
        }
        private void ConnectArgs_Completed(object sender, SocketAsyncEventArgs e)
        {
            ProcessConnect(e);
        }
        private void ProcessConnect(SocketAsyncEventArgs e)
        {

        }
    }
}