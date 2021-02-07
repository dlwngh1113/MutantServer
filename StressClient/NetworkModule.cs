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
        public Dictionary<Socket, Client> clients;
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

        public void Clear()
        {
            foreach(var s in clients)
            {
                
            }
        }

        public void Run()
        {
            if (this.m_numConnectedSockets < 2)
            {
                Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                SocketAsyncEventArgs args = new SocketAsyncEventArgs();
                args.RemoteEndPoint = new IPEndPoint(IPAddress.Loopback, MutantGlobal.PORT);
                args.Completed += new EventHandler<SocketAsyncEventArgs>(ConnectArgs_Completed);
                socket.ConnectAsync(args);
            }
        }
        private void ConnectArgs_Completed(object sender, SocketAsyncEventArgs e)
        {
            ProcessConnect(e);
        }
        private void ProcessConnect(SocketAsyncEventArgs e)
        {
            AsyncUserToken token = new AsyncUserToken();

            SocketAsyncEventArgs recv_event = new SocketAsyncEventArgs();
            recv_event.SetBuffer(new byte[MutantGlobal.BUF_SIZE], 0, MutantGlobal.BUF_SIZE);
            recv_event.Completed += new EventHandler<SocketAsyncEventArgs>(RecvCompleted);
            recv_event.UserToken = token;

            SocketAsyncEventArgs send_event = new SocketAsyncEventArgs();
            send_event.SetBuffer(new byte[MutantGlobal.BUF_SIZE], 0, MutantGlobal.BUF_SIZE);
            send_event.Completed += new EventHandler<SocketAsyncEventArgs>(SendCompleted);
            send_event.UserToken = token;

            BeginIO(e.ConnectSocket, recv_event, send_event);
            Interlocked.Increment(ref m_numConnectedSockets);

            Run();
        }
        private void BeginIO(Socket socket, SocketAsyncEventArgs recv_event, SocketAsyncEventArgs send_event)
        {
            Client player = new Client(m_numConnectedSockets);

            AsyncUserToken token = recv_event.UserToken as AsyncUserToken;
            token.socket = socket;
            token.readEventArgs = recv_event;
            token.writeEventArgs = send_event;
            player.asyncUserToken = token;

            lock (clients)
            {
                clients.Add(socket, player);
            }

            MutantPacket p = new MutantPacket(send_event.Buffer, 0);
            p.name = player.name;
            p.id = player.id;
            p.time = MutantGlobal.GetCurrentMilliseconds();
            p.PacketToByteArray(MutantGlobal.CTOS_LOGIN);

            bool willRaise = socket.SendAsync(send_event);
            if(!willRaise)
            {
                ProcessSend(send_event);
            }
        }
        
        private void RecvCompleted(object sender, SocketAsyncEventArgs e)
        {
            ProcessReceive(e);
        }

        private void ProcessReceive(SocketAsyncEventArgs e)
        {
            if(e.SocketError == SocketError.Success && e.BytesTransferred > 0)
            {
                AsyncUserToken token = e.UserToken as AsyncUserToken;
                switch(token.readEventArgs.Buffer[e.Offset])
                {
                    case MutantGlobal.STOC_CHAT:
                        break;
                    case MutantGlobal.STOC_ENTER:
                        break;
                    case MutantGlobal.STOC_LEAVE:
                        break;
                    case MutantGlobal.STOC_LOGIN_FAIL:
                        break;
                    case MutantGlobal.STOC_LOGIN_OK:
                        ProcessLoginOK(e);
                        break;
                    case MutantGlobal.STOC_STATUS_CHANGE:
                        ProcessStatus(e);
                        break;
                    default:
                        throw new Exception("Unknown Packet from " + clients[token.socket].name);
                }
                clients[token.socket].RandomBehaviour();
            }
            else
            {
                CloseClientSocket(e);
            }
        }

        private void ProcessLoginOK(SocketAsyncEventArgs e)
        {
            AsyncUserToken token = e.UserToken as AsyncUserToken;
            MutantPacket packet = new MutantPacket(e.Buffer, e.Offset);
            packet.ByteArrayToPacket();
        }

        private void ProcessStatus(SocketAsyncEventArgs e)
        {
            AsyncUserToken token = e.UserToken as AsyncUserToken;
            PlayerStatusPacket packet = new PlayerStatusPacket(e.Buffer, e.Offset);
            packet.ByteArrayToPacket();
            clients[token.socket].position = packet.position;
            clients[token.socket].rotation = packet.rotation;
        }

        private void SendCompleted(object sender, SocketAsyncEventArgs e)
        {
            ProcessSend(e);
        }

        private void ProcessSend(SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success)
            {
                // done echoing data back to the client
                AsyncUserToken token = (AsyncUserToken)e.UserToken;

                bool willRaise = token.socket.ReceiveAsync(token.readEventArgs);
                if(!willRaise)
                {
                    ProcessReceive(token.readEventArgs);
                }
            }
            else
            {
                CloseClientSocket(e);
            }
        }
    }
}