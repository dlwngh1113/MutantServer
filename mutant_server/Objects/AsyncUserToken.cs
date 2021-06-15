﻿using System;
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
        public void ResolveMessage(byte[] ary, int offset, int bytesTransferred)
        {
            byte[] data = _messageResolver.ResolveMessage(ary, offset, bytesTransferred);
            if (data != null)
            {
                RecvEventSelect(data);
            }
        }
        private void RecvEventSelect(byte[] data)
        {
            switch ((CTOS_OP)this.readEventArgs.Buffer[this.readEventArgs.Offset])
            {
                case CTOS_OP.CTOS_LOGIN:
                    ProcessLogin(data);
                    break;
                case CTOS_OP.CTOS_JOIN_GAME:
                    ProcessEnterPlayer(data);
                    break;
                case CTOS_OP.CTOS_STATUS_CHANGE:
                    ProcessStatus(data);
                    break;
                case CTOS_OP.CTOS_ATTACK:
                    ProcessAttack(data);
                    break;
                case CTOS_OP.CTOS_CHAT:
                    ProcessChatting(data);
                    break;
                case CTOS_OP.CTOS_LEAVE_GAME:
                case CTOS_OP.CTOS_LOGOUT:
                    ProcessLogout(data);
                    break;
                case CTOS_OP.CTOS_ITEM_CLICKED:
                    ProcessItemEvent(data);
                    break;
                case CTOS_OP.CTOS_ITEM_CRAFT_REQUEST:
                    ProcessItemCraft(data);
                    break;
                case CTOS_OP.CTOS_VOTE_SELECTED:
                    ProcessVote(data);
                    break;
                case CTOS_OP.CTOS_VOTE_REQUEST:
                    ProcessStartVote(data);
                    break;
                default:
                    throw new Exception("operation from client is not valid\n");
            }
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
        private Client InitClient(MutantPacket packet)
        {
            if(!IsValidUser(packet))
            {
                return null;
            }
            Interlocked.Increment(ref Defines.id);
            this.userID = Defines.id;

            Client client = new Client(Defines.id);
            client.userName = packet.name;
            client.position = new MyVector3(95.09579f, 5.16f, 42.68918f);
            client.rotation = new MyVector3();
            client.asyncUserToken = this;
            client.job = Server.jobArray[Server.globalOffset];
            client.InitPos = Server.initPosAry[Server.globalOffset];
            Server.globalOffset = (Server.globalOffset + 1) % Server.jobArray.Length;
            //Interlocked.CompareExchange(ref Server.globalOffset, (Server.globalOffset + 1) % Server.jobArray.Length, (Server.globalOffset + 1) % Server.jobArray.Length);

            lock (Server.players)
            {
                Server.players[client.userID] = client;
            }

            return client;
        }
        private bool IsValidUser(MutantPacket packet)
        {
            foreach(var tuple in Server.players)
            {
                if(packet.name == tuple.Value.userName)
                {
                    return false;
                }
                else if(packet.id == tuple.Key)
                {
                    return false;
                }
                else if(packet.name == " ")
                {
                    return false;
                }
                else if(packet.name == "")
                {
                    return false;
                }
            }

            return true;
        }

        private void ProcessLogin(byte[] data)
        {
            //if (DB에서 이미 플레이어의 이름이 존재하고, 서버에서 사용중이지 않다면)
            //해당 정보로 로그인 login ok 클라이언트로 전송
            //else if 서버, DB에 모두 존재하지 않는 이름이라면
            //새롭게 DB에 유저 정보를 입력하고 login ok 전송
            //else
            //login fail과 이미
            MutantPacket packet = new MutantPacket(readEventArgs.Buffer, readEventArgs.Offset);
            packet.ByteArrayToPacket();

            Client c = InitClient(packet);
            if(c == null)
            {
                return;
            }
            Console.WriteLine("{0} client has {1} id, login request!", c.userName, c.userID);

            MutantPacket sendPacket = new MutantPacket(new byte[Defines.BUF_SIZE], 0);
            sendPacket.id = c.userID;
            sendPacket.name = c.userName;
            sendPacket.time = 0;

            sendPacket.PacketToByteArray((byte)STOC_OP.STOC_LOGIN_OK);

            SendData(sendPacket);
        }

        private void ProcessEnterPlayer(byte[] data)
        {
            MutantPacket packet = new MutantPacket(readEventArgs.Buffer, readEventArgs.Offset);
            packet.ByteArrayToPacket();

            PlayerStatusPacket sendPacket = new PlayerStatusPacket(new byte[Defines.BUF_SIZE], 0);
            sendPacket.id = packet.id;
            sendPacket.name = packet.name;
            sendPacket.time = packet.time;
            sendPacket.position = Server.players[packet.id].position;
            sendPacket.rotation = Server.players[packet.id].rotation;
            sendPacket.playerJob = Server.players[packet.id].job;

            sendPacket.PacketToByteArray((byte)STOC_OP.STOC_PLAYER_ENTER);

            SendData(sendPacket);

            foreach (var tuple in Server.players)
            {
                if (tuple.Key != sendPacket.id)
                {
                    var tmpToken = tuple.Value.asyncUserToken;

                    //기존의 플레이어에게 새로운 플레이어 정보 전달
                    PlayerStatusPacket curPacket = new PlayerStatusPacket(new byte[Defines.BUF_SIZE], 0);
                    curPacket.id = sendPacket.id;
                    curPacket.name = sendPacket.name;
                    curPacket.time = sendPacket.time;
                    curPacket.position = Server.players[sendPacket.id].position;
                    curPacket.rotation = Server.players[sendPacket.id].rotation;

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

        private void ProcessStatus(byte[] data)
        {
            PlayerStatusPacket packet = new PlayerStatusPacket(readEventArgs.Buffer, readEventArgs.Offset);
            packet.ByteArrayToPacket();

            if(!(Server.players.ContainsKey(packet.id)))
            {
                return;
            }
            Server.players[packet.id].position = packet.position;
            Server.players[packet.id].rotation = packet.rotation;

            foreach (var tuple in Server.players)
            {
                if (tuple.Key != packet.id)
                {
                    var tmpToken = tuple.Value.asyncUserToken;
                    PlayerStatusPacket sendPacket = new PlayerStatusPacket(new byte[Defines.BUF_SIZE], 0);
                    sendPacket.id = packet.id;
                    sendPacket.name = packet.name;
                    sendPacket.time = 0;
                    sendPacket.playerMotion = packet.playerMotion;
                    sendPacket.time = 0;
                    sendPacket.position = Server.players[packet.id].position;
                    sendPacket.rotation = Server.players[packet.id].rotation;

                    //Console.WriteLine("id = {0} x = {1} y = {2} z = {3}", packet.id, packet.position.x, packet.position.y, packet.position.z);

                    sendPacket.PacketToByteArray((byte)STOC_OP.STOC_STATUS_CHANGE);

                    tmpToken.SendData(sendPacket);
                }
            }
        }

        private void ProcessItemEvent(byte[] data)
        {
            ItemEventPacket packet = new ItemEventPacket(readEventArgs.Buffer, readEventArgs.Offset);
            packet.ByteArrayToPacket();
            var cnt = 0;
            //인벤토리에 존재하는 아이템의 개수 구하기
            foreach (var tuple in Server.players[packet.id].inventory)
            {
                cnt += tuple.Value;
            }

            ItemEventPacket sendPacket = new ItemEventPacket(this.writeEventArgs.Buffer, this.writeEventArgs.Offset);

            //만약 아이템의 총 개수가 3개보다 많다면 획득하지 못함
            if (cnt > 2)
            {
                sendPacket.id = packet.id;
                sendPacket.name = packet.name;
                sendPacket.itemName = packet.itemName;
                sendPacket.inventory = Server.players[packet.id].inventory;
                sendPacket.canGainItem = false;
                sendPacket.PacketToByteArray((byte)STOC_OP.STOC_ITEM_DENIED);
            }
            //그렇지 않은 경우는 아이템 획득 가능
            else
            {
                //SendPacket.inventory Null Reference Exception
                if (Server.players[packet.id].inventory.ContainsKey(packet.itemName))
                {
                    Server.players[packet.id].inventory[packet.itemName] += 1;
                }
                else
                {
                    Server.players[packet.id].inventory.Add(packet.itemName, 1);
                }
                sendPacket.id = packet.id;
                sendPacket.name = packet.name;
                sendPacket.itemName = packet.itemName;
                sendPacket.inventory = Server.players[packet.id].inventory;
                sendPacket.canGainItem = true;
                sendPacket.PacketToByteArray((byte)STOC_OP.STOC_ITEM_GAIN);
            }

            foreach (var tuple in Server.players[packet.id].inventory)
            {
                Console.Write("key - {0}, value - {1}", tuple.Key, tuple.Value);
            }
            Console.WriteLine("");

            SendData(sendPacket);
        }

        private void AddItemInGlobal(string itemName)
        {
            if (itemName == "Axe")
            {
                return;
            }
            if (Server.globalItem.ContainsKey(itemName))
            {
                Server.globalItem[itemName]++;
            }
            else
            {
                Server.globalItem.Add(itemName, 1);
            }
        }

        private void ProcessItemCraft(byte[] data)
        {
            ItemCraftPacket packet = new ItemCraftPacket(readEventArgs.Buffer, readEventArgs.Offset);
            packet.ByteArrayToPacket();

            ItemCraftPacket sendPacket = new ItemCraftPacket(this.writeEventArgs.Buffer, this.writeEventArgs.Offset);
            sendPacket.id = packet.id;
            sendPacket.name = packet.name;
            sendPacket.time = packet.time;

            if (!Server.players.ContainsKey(packet.id))
            {
                return;
            }

            Console.WriteLine("packet.itemName - {0}", packet.itemName);
            switch(packet.itemName)
            {
                case "Axe":
                    Server.players[packet.id].inventory["Stick"] -= 1;
                    Server.players[packet.id].inventory["Rock"] -= 1;
                    if (!Server.players[packet.id].inventory.ContainsKey("Axe"))
                    {
                        Server.players[packet.id].inventory.Add("Axe", 1);
                    }
                    break;
                case "Plane":
                    Server.players[packet.id].inventory["Log"] -= 2;
                    break;
                case "Sail":
                    Server.players[packet.id].inventory["Log"] -= 1;
                    Server.players[packet.id].inventory["Rope"] -= 1;
                    break;
                case "Paddle":
                    Server.players[packet.id].inventory["Log"] -= 1;
                    break;
            }
            AddItemInGlobal(packet.itemName);

            sendPacket.inventory = Server.players[packet.id].inventory;
            sendPacket.itemName = packet.itemName;
            sendPacket.globalItem = Server.globalItem;

            sendPacket.PacketToByteArray((byte)STOC_OP.STOC_ITEM_CRAFTED);

            SendData(sendPacket);

            foreach (var tuple in Server.players[packet.id].inventory)
            {
                Console.Write("key - {0}, value - {1}", tuple.Key, tuple.Value);
            }
            Console.WriteLine("");

            foreach (var tuple in Server.globalItem)
            {
                Console.Write("key - {0}, value - {1}", tuple.Key, tuple.Value);
            }
            Console.WriteLine("");

            foreach (var tuple in Server.players)
            {
                if (tuple.Key != packet.id)
                {
                    var token = tuple.Value.asyncUserToken;

                    ItemCraftPacket otherPacket = new ItemCraftPacket(new byte[Defines.BUF_SIZE], 0);
                    otherPacket.id = sendPacket.id;
                    otherPacket.name = sendPacket.name;
                    otherPacket.time = sendPacket.time;

                    otherPacket.itemName = sendPacket.itemName;
                    otherPacket.inventory = sendPacket.inventory;
                    otherPacket.globalItem = sendPacket.globalItem;
                    otherPacket.PacketToByteArray((byte)STOC_OP.STOC_ITEM_CRAFTED);

                    token.SendData(otherPacket);
                }
            }
        }
        private void ProcessAttack(byte[] data)
        {
            //누가 어떤 플레이어를 공격했는가?
            //공격당한 플레이어를 죽게 하고 공격한 플레이어 타이머 리셋
            MutantPacket packet = new MutantPacket(readEventArgs.Buffer, readEventArgs.Offset);
            packet.ByteArrayToPacket();

            if(!Server.players.ContainsKey(packet.id))
            {
                return;
            }

            Console.WriteLine("id: {0} was killed", packet.id);

            foreach (var tuple in Server.players)
            {
                var tmpToken = tuple.Value.asyncUserToken;

                //기존의 플레이어에게 새로운 플레이어 정보 전달
                PlayerStatusPacket sendPacket = new PlayerStatusPacket(new byte[Defines.BUF_SIZE], 0);
                sendPacket.id = packet.id;
                sendPacket.name = packet.name;
                sendPacket.time = 0;

                sendPacket.position = Server.players[packet.id].position;
                sendPacket.rotation = Server.players[packet.id].rotation;
                sendPacket.playerMotion = Defines.PLAYER_HIT;
                sendPacket.PacketToByteArray((byte)STOC_OP.STOC_KILLED);

                tmpToken.SendData(sendPacket);
            }
        }

        private void ProcessChatting(byte[] data)
        {
            //클라이언트에서 온 메세지를 모든 클라이언트에 전송
            ChattingPakcet packet = new ChattingPakcet(readEventArgs.Buffer, readEventArgs.Offset);
            packet.ByteArrayToPacket();

            foreach (var tuple in Server.players)
            {
                if (packet.id != tuple.Key)
                {
                    var tmpToken = tuple.Value.asyncUserToken;

                    //기존의 플레이어에게 새로운 플레이어 정보 전달
                    ChattingPakcet sendPacket = new ChattingPakcet(new byte[Defines.BUF_SIZE], 0);
                    sendPacket.id = packet.id;
                    sendPacket.name = packet.name;
                    sendPacket.time = 0;

                    sendPacket.message = packet.message;
                    sendPacket.PacketToByteArray((byte)STOC_OP.STOC_CHAT);

                    tmpToken.SendData(sendPacket);
                }
            }
        }

        public void ProcessStartVote(byte[] data)
        {
            MutantPacket packet = new MutantPacket(readEventArgs.Buffer, readEventArgs.Offset);
            packet.ByteArrayToPacket();

            for (int i = 0;i<Server.players.Count;++i)
            {
                var tuple = Server.players.ElementAt(i);
                tuple.Value.position = tuple.Value.InitPos;
                for(int j = i;j<Server.players.Count;++j)
                {
                    var token = Server.players.ElementAt(j).Value.asyncUserToken;
                    PlayerStatusPacket posPacket = new PlayerStatusPacket(new byte[Defines.BUF_SIZE], 0);
                    posPacket.id = tuple.Key;
                    posPacket.name = tuple.Value.userName;
                    posPacket.time = 0;

                    posPacket.position = tuple.Value.position;
                    posPacket.rotation = tuple.Value.rotation;
                    posPacket.playerMotion = Defines.PLAYER_IDLE;

                    posPacket.PacketToByteArray((byte)STOC_OP.STOC_STATUS_CHANGE);

                    SendData(posPacket);
                }
            }

            foreach (var tuple in Server.players)
            {
                var token = tuple.Value.asyncUserToken;
                PlayerStatusPacket sendPacket = new PlayerStatusPacket(new byte[Defines.BUF_SIZE], 0);
                sendPacket.id = tuple.Value.userID;
                sendPacket.name = tuple.Value.userName;
                sendPacket.position = tuple.Value.InitPos;
                sendPacket.rotation = tuple.Value.rotation;
                sendPacket.playerMotion = Defines.PLAYER_IDLE;

                sendPacket.PacketToByteArray((byte)STOC_OP.STOC_VOTE_START);

                token.SendData(sendPacket);
            }
        }
        public void ProcessVote(byte[] data)
        {
            VotePacket packet = new VotePacket(readEventArgs.Buffer, readEventArgs.Offset);
            packet.ByteArrayToPacket();

            if(!Server.voteCounter.ContainsKey(packet.votedPersonID))
            {
                Server.voteCounter.Add(packet.votedPersonID, 1);
            }
            else
            {
                Server.voteCounter[packet.votedPersonID] += 1;
            }

            Console.WriteLine("name = {0}, id = {1}", packet.name, packet.id);

            foreach (var tuple in Server.players)
            {
                var token = tuple.Value.asyncUserToken;
                VotePacket sendPacket = new VotePacket(new byte[Defines.BUF_SIZE], 0);
                sendPacket.name = packet.name;
                sendPacket.id = packet.id;
                sendPacket.time = packet.time;

                sendPacket.votedPersonID = packet.votedPersonID;
                sendPacket.votePairs = Server.voteCounter;

                sendPacket.PacketToByteArray((byte)STOC_OP.STOC_VOTED);

                token.SendData(sendPacket);
            }
        }

        private void ProcessLogout(byte[] data)
        {
            //현재까지의 게임 정보를 DB에 업데이트 후 접속 종료

            //지금 게임에 존재하는 유저들에게 해당 유저가 게임을 종료했음을 알림
            //지금 게임을 같이 하고 있는 유저들을 어떻게 구분할 것인가?
            MutantPacket packet = new MutantPacket(this.readEventArgs.Buffer, this.readEventArgs.Offset);
            packet.ByteArrayToPacket();

            foreach(var tuple in Server.players)
            {
                if (tuple.Key != packet.id)
                {
                    var token = tuple.Value.asyncUserToken;
                    MutantPacket sendPacket = new MutantPacket(new byte[Defines.BUF_SIZE], 0);
                    sendPacket.name = packet.name;
                    sendPacket.id = packet.id;
                    sendPacket.time = packet.time;

                    Console.WriteLine("{0} player out of game", packet.id);

                    sendPacket.PacketToByteArray((byte)STOC_OP.STOC_PLAYER_LEAVE);

                    token.SendData(sendPacket);
                }
            }

            closeMethod(this.readEventArgs);
        }
    }
}