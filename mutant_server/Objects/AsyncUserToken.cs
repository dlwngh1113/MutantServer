using System;
using System.Collections.Generic;
using System.Net.Sockets;

namespace mutant_server
{
    public class AsyncUserToken
    {
        public delegate void SendCallback(SocketAsyncEventArgs e);
        public delegate void RecvCallback(SocketAsyncEventArgs e);
        public delegate void CloseMethod(SocketAsyncEventArgs e);

        private MessageResolver _messageResolver = new MessageResolver();
        private Queue<MutantPacket> sendQueue = new Queue<MutantPacket>();

        public Socket socket = null;
        public int userID;
        public SocketAsyncEventArgs readEventArgs = null;
        public SocketAsyncEventArgs writeEventArgs = null;
        public RecvCallback recvCallback;
        public RecvCallback sendCallback;
        public CloseMethod closeMethod;
        public AsyncUserToken()
        {

        }
        public AsyncUserToken(Socket s)
        {
            this.socket = s;
        }

        public byte[] ResolveMessage()
        {
            byte[] data = _messageResolver.ResolveMessage(readEventArgs.Buffer, readEventArgs.Offset, readEventArgs.BytesTransferred);
            if(data != null)
            { 
                return data; 
            }
            return null;
        }

        public void ClearMessageBuffer()
        {
            _messageResolver.CleanVariables();
        }

        public void SendData(MutantPacket packet)
        {
            lock(this.sendQueue)
            {
                if(this.sendQueue.Count <= 0)
                {
                    this.sendQueue.Enqueue(packet);
                    StartSend();
                    return;
                }

                this.sendQueue.Enqueue(packet);
            }
        }

        private void StartSend()
        {
            lock(this.sendQueue)
            {
                var packet = this.sendQueue.Peek();

                Array.Copy(packet.ary, packet.startPos, this.writeEventArgs.Buffer, this.writeEventArgs.Offset, packet.offset);

                try
                {
                    bool willRaise = this.socket.SendAsync(this.writeEventArgs);
                    if (!willRaise)
                    {
                        sendCallback(this.writeEventArgs);
                    }
                }
                catch (Exception ex) { }
            }
        }

        public void SendEnd()
        {
            lock(this.sendQueue)
            {
                this.sendQueue.Dequeue();

                if(this.sendQueue.Count > 0)
                {
                    StartSend();
                }
            }
        }
    }
}
