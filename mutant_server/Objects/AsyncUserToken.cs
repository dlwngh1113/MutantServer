using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using System.Linq;
using mutant_server.Packets;

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

                Array.Copy(packet.ary, packet.startPos, this.writeEventArgs.Buffer, this.writeEventArgs.Offset, Defines.BUF_SIZE);

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

        private void ProcessEnterPlayer(byte[] data)
        {
            MutantPacket packet = new MutantPacket(readEventArgs.Buffer, readEventArgs.Offset);
            packet.ByteArrayToPacket();

            PlayerStatusPacket sendPacket = new PlayerStatusPacket(new byte[Defines.BUF_SIZE], 0);
            sendPacket.id = packet.id;
            sendPacket.name = packet.name;
            sendPacket.time = packet.time;
            sendPacket.position = Server._players[packet.id].position;
            sendPacket.rotation = Server._players[packet.id].rotation;
            sendPacket.playerJob = Server._players[packet.id].job;

            sendPacket.PacketToByteArray((byte)STOC_OP.STOC_PLAYER_ENTER);

            SendData(sendPacket);

            foreach (var tuple in Server._players)
            {
                if (tuple.Key != sendPacket.id)
                {
                    var tmpToken = tuple.Value.asyncUserToken;

                    //기존의 플레이어에게 새로운 플레이어 정보 전달
                    PlayerStatusPacket curPacket = new PlayerStatusPacket(new byte[Defines.BUF_SIZE], 0);
                    curPacket.id = sendPacket.id;
                    curPacket.name = sendPacket.name;
                    curPacket.time = sendPacket.time;
                    curPacket.position = Server._players[sendPacket.id].position;
                    curPacket.rotation = Server._players[sendPacket.id].rotation;

                    curPacket.PacketToByteArray((byte)STOC_OP.STOC_PLAYER_ENTER);

                    tmpToken.SendData(curPacket);

                    //새로운 플레이어에게 기존의 플레이어 정보 전달
                    PlayerStatusPacket otherPacket = new PlayerStatusPacket(new byte[Defines.BUF_SIZE], 0);
                    otherPacket.id = tuple.Key;
                    otherPacket.name = tuple.Value.userName;
                    otherPacket.time = sendPacket.time;
                    otherPacket.position = tuple.Value.position;
                    otherPacket.rotation = tuple.Value.rotation;

                    otherPacket.PacketToByteArray((byte)STOC_OP.STOC_PLAYER_ENTER);

                    SendData(otherPacket);
                }
            }
        }
    }
}
