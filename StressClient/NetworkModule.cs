using mutant_server;
using mutant_server.Packets;
using mutant_server.Objects.Networking;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
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
            if (m_numConnectedSockets < 100)
            {
                Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                SocketAsyncEventArgs args = new SocketAsyncEventArgs();
                args.RemoteEndPoint = new IPEndPoint(IPAddress.Loopback, Defines.PORT);
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
            recv_event.SetBuffer(new byte[Defines.BUF_SIZE], 0, Defines.BUF_SIZE);
            recv_event.Completed += new EventHandler<SocketAsyncEventArgs>(RecvCompleted);
            recv_event.UserToken = token;

            SocketAsyncEventArgs send_event = new SocketAsyncEventArgs();
            send_event.SetBuffer(new byte[Defines.BUF_SIZE], 0, Defines.BUF_SIZE);
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

            LoginPacket p = new LoginPacket(new byte[Defines.BUF_SIZE], 0);
            p.name = player.name;
            p.id = player.id;
            p.time = 0;

            p.passwd = "1111";

            p.PacketToByteArray((byte)CTOS_OP.CTOS_LOGIN);

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
                switch((STOC_OP)token.readEventArgs.Buffer[e.Offset])
                {
                    case STOC_OP.STOC_CHAT:
                        break;
                    //case STOC_OP.STOC_ENTER:
                    //    break;
                    //case STOC_OP.STOC_LEAVE:
                    //    break;
                    case STOC_OP.STOC_LOGIN_FAIL:
                        ProcessLoginFail(token);
                        break;
                    case STOC_OP.STOC_LOGIN_OK:
                        ProcessLoginOK(token);
                        break;
                    case STOC_OP.STOC_STATUS_CHANGE:
                        ProcessStatus(token);
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

        private void ProcessLoginFail(AsyncUserToken token)
        { 

        }
        private void ProcessLoginOK(AsyncUserToken token)
        {
            PlayerStatusPacket packet = new PlayerStatusPacket(token.readEventArgs.Buffer, token.readEventArgs.Offset);
            packet.ByteArrayToPacket();

            clients[token.socket].position = packet.position;
            clients[token.socket].rotation = packet.rotation;
        }

        private void ProcessStatus(AsyncUserToken token)
        {
            PlayerStatusPacket packet = new PlayerStatusPacket(token.readEventArgs.Buffer, token.readEventArgs.Offset);
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