using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;

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
            //RecvEventSelect(ary);
            byte[] data = _messageResolver.ResolveMessage(ary, offset, bytesTransferred);
            if (data != null)
            {
                RecvEventSelect(data);
            }
        }
        private void RecvEventSelect(byte[] data)
        {
            switch (this.readEventArgs.Buffer[this.readEventArgs.Offset])
            {
                case Defines.CTOS_LOGIN:
                    ProcessLogin(data);
                    break;
                case Defines.CTOS_STATUS_CHANGE:
                    ProcessStatus(data);
                    break;
                case Defines.CTOS_ATTACK:
                    ProcessAttack(data);
                    break;
                case Defines.CTOS_CHAT:
                    ProcessChatting(data);
                    break;
                case Defines.CTOS_LOGOUT:
                    ProcessLogout(data);
                    break;
                case Defines.CTOS_ITEM_CLICKED:
                    ProcessItemEvent(data);
                    break;
                case Defines.CTOS_ITEM_CRAFT_REQUEST:
                    ProcessItemCraft(data);
                    break;
                default:
                    throw new Exception("operation from client is not valid\n");
            }
        }

        private void SendData(MutantPacket packet)
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

                bool willRaise = this.socket.SendAsync(this.writeEventArgs);
                if(!willRaise)
                {
                    sendCallback(this.writeEventArgs);
                }
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
        private Client InitClient()
        {
            Interlocked.Increment(ref Defines.id);
            this.userID = Defines.id;

            Client client = new Client(Defines.id);
            client.position = new MyVector3(95.09579f, 4.16f, 42.68918f);
            client.rotation = new MyVector3();
            client.asyncUserToken = this;
            client.job = Server.jobArray[Server.jobOffset];
            Server.jobOffset = (byte)((Server.jobOffset + 1) % Server.jobArray.Length);

            lock (Server.players)
            {
                Server.players[client.userID] = client;
            }

            return client;
        }
        private bool IsValidUser(MutantPacket packet)
        {
            //if (packet.id < Defines.id)
            //{
            //    return false;
            //}
            if (packet.name != "admin")
            {
                return false;
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

            if(!IsValidUser(packet))
            {
                return;
            }

            Client c = InitClient();
            Console.WriteLine("{0} client has {1} id, login request!", c.userName, c.userID);

            PlayerStatusPacket sendPacket = new PlayerStatusPacket(this.writeEventArgs.Buffer, this.writeEventArgs.Offset);
            sendPacket.id = c.userID;
            sendPacket.name = packet.name;
            sendPacket.time = packet.time;
            sendPacket.position = c.position;
            sendPacket.rotation = c.rotation;
            sendPacket.playerJob = c.job;

            sendPacket.PacketToByteArray(Defines.STOC_LOGIN_OK);

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

                    curPacket.PacketToByteArray(Defines.STOC_PLAYER_ENTER);

                    tmpToken.SendData(curPacket);

                    //새로운 플레이어에게 기존의 플레이어 정보 전달
                    PlayerStatusPacket otherPacket = new PlayerStatusPacket(new byte[Defines.BUF_SIZE], 0);
                    otherPacket.id = tuple.Key;
                    otherPacket.name = tuple.Value.userName;
                    otherPacket.position = tuple.Value.position;
                    otherPacket.rotation = tuple.Value.rotation;

                    otherPacket.PacketToByteArray(Defines.STOC_PLAYER_ENTER);

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
                    sendPacket.playerMotion = packet.playerMotion;
                    sendPacket.time = Defines.GetCurrentMilliseconds();
                    sendPacket.position = Server.players[packet.id].position;
                    sendPacket.rotation = Server.players[packet.id].rotation;

                    //Console.WriteLine("id = {0} x = {1} y = {2} z = {3}", packet.id, packet.position.x, packet.position.y, packet.position.z);

                    sendPacket.PacketToByteArray(Defines.STOC_STATUS_CHANGE);

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
                sendPacket.PacketToByteArray(Defines.STOC_ITEM_DENIED);
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
                sendPacket.PacketToByteArray(Defines.STOC_ITEM_GAIN);
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

            sendPacket.PacketToByteArray(Defines.STOC_ITEM_CRAFTED);

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
                    otherPacket.PacketToByteArray(Defines.STOC_ITEM_CRAFTED);

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

            foreach (var tuple in Server.players)
            {
                var tmpToken = tuple.Value.asyncUserToken;

                //기존의 플레이어에게 새로운 플레이어 정보 전달
                PlayerStatusPacket sendPacket = new PlayerStatusPacket(new byte[Defines.BUF_SIZE], 0);
                sendPacket.id = packet.id;
                sendPacket.name = packet.name;
                sendPacket.time = Defines.GetCurrentMilliseconds();

                sendPacket.position = Server.players[packet.id].position;
                sendPacket.rotation = Server.players[packet.id].rotation;
                sendPacket.playerMotion = Defines.PLAYER_HIT;
                sendPacket.PacketToByteArray(Defines.STOC_KILLED);

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
                var tmpToken = tuple.Value.asyncUserToken;

                //기존의 플레이어에게 새로운 플레이어 정보 전달
                ChattingPakcet sendPacket = new ChattingPakcet(new byte[Defines.BUF_SIZE], 0);
                sendPacket.id = packet.id;
                sendPacket.name = packet.name;
                sendPacket.time = Defines.GetCurrentMilliseconds();

                sendPacket.message = packet.message;
                sendPacket.PacketToByteArray(Defines.STOC_CHAT);

                tmpToken.SendData(sendPacket);
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

                    sendPacket.PacketToByteArray(Defines.STOC_PLAYER_LEAVE);

                    token.SendData(sendPacket);
                }
            }

            closeMethod(this.readEventArgs);
        }
    }
}
