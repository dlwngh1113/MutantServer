using mutant_server.Packets;
using System;
using System.Collections.Generic;
using System.Linq;

namespace mutant_server
{
    class Room
    {
        /// <summary>
        /// player id, player class
        /// </summary>
        private Dictionary<int, Client> _players;
        private MessageResolver _messageResolver;
        public CloseMethod closeMethod;

        private Dictionary<string, int> _globalItem = new Dictionary<string, int>();
        private Dictionary<string, int> _voteCounter = new Dictionary<string, int>();
        private byte[] jobArray = Defines.GenerateRandomJobs();
        private MyVector3[] initPosAry = { new MyVector3(95.09579f, 5.16f, 42.68918f),
            new MyVector3(95.09579f, 5.16f, 42.68918f), new MyVector3(95.09579f, 5.16f, 42.68918f),
            new MyVector3(95.09579f, 5.16f, 42.68918f), new MyVector3(95.09579f, 5.16f, 42.68918f) };
        private int globalOffset = 0;

        public int PlayerNum
        {
            get => _players.Count;
        }
        public Room()
        {
            _players = new Dictionary<int, Client>(5);
            _messageResolver = new MessageResolver();
        }

        public void AddPlayer(int id, Client c)
        {
            this._players.Add(id, c);
        }

        public void ResolveMessge(AsyncUserToken token)
        {
            byte[] data = _messageResolver.ResolveMessage(token.readEventArgs.Buffer, token.readEventArgs.Offset, token.readEventArgs.BytesTransferred);
            if (data != null)
            {
                RecvEventSelect(token);
            }
        }
        private void RecvEventSelect(AsyncUserToken token)
        {
            switch ((CTOS_OP)token.readEventArgs.Buffer[token.readEventArgs.Offset])
            {
                case CTOS_OP.CTOS_STATUS_CHANGE:
                    ProcessStatus(token);
                    break;
                case CTOS_OP.CTOS_ATTACK:
                    ProcessAttack(token);
                    break;
                case CTOS_OP.CTOS_CHAT:
                    ProcessChatting(token);
                    break;
                case CTOS_OP.CTOS_JOIN_GAME:
                    //Do Something
                    break;
                case CTOS_OP.CTOS_LEAVE_GAME:
                    ProcessLeaveGame(token);
                    break;
                case CTOS_OP.CTOS_ITEM_CLICKED:
                    ProcessItemEvent(token);
                    break;
                case CTOS_OP.CTOS_ITEM_CRAFT_REQUEST:
                    ProcessItemCraft(token);
                    break;
                case CTOS_OP.CTOS_VOTE_SELECTED:
                    ProcessVote(token);
                    break;
                case CTOS_OP.CTOS_VOTE_REQUEST:
                    ProcessStartVote(token);
                    break;
                default:
                    throw new Exception("operation from client is not valid\n");
            }
        }

        private void ProcessLeaveGame(AsyncUserToken token)
        {
            MutantPacket packet = new MutantPacket(token.readEventArgs.Buffer, token.readEventArgs.Offset);
            packet.ByteArrayToPacket();

            foreach (var tuple in _players)
            {
                if (tuple.Key != packet.id)
                {
                    var tmpToken = tuple.Value.asyncUserToken;
                    MutantPacket sendPacket = new MutantPacket(new byte[Defines.BUF_SIZE], 0);
                    sendPacket.name = packet.name;
                    sendPacket.id = packet.id;
                    sendPacket.time = packet.time;

                    sendPacket.PacketToByteArray((byte)STOC_OP.STOC_PLAYER_LEAVE);

                    tmpToken.SendData(sendPacket);
                }
            }

            closeMethod(token.readEventArgs);
        }
        private void ProcessChatting(AsyncUserToken token)
        {
            //클라이언트에서 온 메세지를 모든 클라이언트에 전송
            ChattingPakcet packet = new ChattingPakcet(token.readEventArgs.Buffer, token.readEventArgs.Offset);
            packet.ByteArrayToPacket();

            foreach (var tuple in _players)
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

        private void AddItemInGlobal(string itemName)
        {
            if (itemName == "Axe")
            {
                return;
            }
            if (_globalItem.ContainsKey(itemName))
            {
                _globalItem[itemName]++;
            }
            else
            {
                _globalItem.Add(itemName, 1);
            }
        }

        public void ProcessStartVote(AsyncUserToken token)
        {
            MutantPacket packet = new MutantPacket(token.readEventArgs.Buffer, token.readEventArgs.Offset);
            packet.ByteArrayToPacket();

            for (int i = 0; i < _players.Count; ++i)
            {
                var tuple = _players.ElementAt(i);
                tuple.Value.position = tuple.Value.InitPos;
                for (int j = i; j < _players.Count; ++j)
                {
                    var tmpToken = _players.ElementAt(j).Value.asyncUserToken;
                    PlayerStatusPacket posPacket = new PlayerStatusPacket(new byte[Defines.BUF_SIZE], 0);
                    posPacket.id = tuple.Key;
                    posPacket.name = tuple.Value.userName;
                    posPacket.time = 0;

                    posPacket.position = tuple.Value.position;
                    posPacket.rotation = tuple.Value.rotation;
                    posPacket.playerMotion = Defines.PLAYER_IDLE;

                    posPacket.PacketToByteArray((byte)STOC_OP.STOC_STATUS_CHANGE);

                    tmpToken.SendData(posPacket);
                }
            }

            foreach (var tuple in _players)
            {
                var tmpToken = tuple.Value.asyncUserToken;
                PlayerStatusPacket sendPacket = new PlayerStatusPacket(new byte[Defines.BUF_SIZE], 0);
                sendPacket.id = tuple.Value.userID;
                sendPacket.name = tuple.Value.userName;
                sendPacket.position = tuple.Value.InitPos;
                sendPacket.rotation = tuple.Value.rotation;
                sendPacket.playerMotion = Defines.PLAYER_IDLE;

                sendPacket.PacketToByteArray((byte)STOC_OP.STOC_VOTE_START);

                tmpToken.SendData(sendPacket);
            }
        }
        public void ProcessVote(AsyncUserToken token)
        {
            VotePacket packet = new VotePacket(token.readEventArgs.Buffer, token.readEventArgs.Offset);
            packet.ByteArrayToPacket();

            if (!_voteCounter.ContainsKey(packet.votedPersonID))
            {
                _voteCounter.Add(packet.votedPersonID, 1);
            }
            else
            {
                _voteCounter[packet.votedPersonID] += 1;
            }

            Console.WriteLine("name = {0}, id = {1}", packet.name, packet.id);

            foreach (var tuple in _players)
            {
                var tmpToken = tuple.Value.asyncUserToken;
                VotePacket sendPacket = new VotePacket(new byte[Defines.BUF_SIZE], 0);
                sendPacket.name = packet.name;
                sendPacket.id = packet.id;
                sendPacket.time = packet.time;

                sendPacket.votedPersonID = packet.votedPersonID;
                sendPacket.votePairs = _voteCounter;

                sendPacket.PacketToByteArray((byte)STOC_OP.STOC_VOTED);

                tmpToken.SendData(sendPacket);
            }
        }

        private void ProcessItemCraft(AsyncUserToken token)
        {
            ItemCraftPacket packet = new ItemCraftPacket(token.readEventArgs.Buffer, token.readEventArgs.Offset);
            packet.ByteArrayToPacket();

            ItemCraftPacket sendPacket = new ItemCraftPacket(token.writeEventArgs.Buffer, token.writeEventArgs.Offset);
            sendPacket.id = packet.id;
            sendPacket.name = packet.name;
            sendPacket.time = packet.time;

            if (!_players.ContainsKey(packet.id))
            {
                return;
            }

            Console.WriteLine("packet.itemName - {0}", packet.itemName);
            switch (packet.itemName)
            {
                case "Axe":
                    _players[packet.id].inventory["Stick"] -= 1;
                    _players[packet.id].inventory["Rock"] -= 1;
                    if (!_players[packet.id].inventory.ContainsKey("Axe"))
                    {
                        _players[packet.id].inventory.Add("Axe", 1);
                    }
                    break;
                case "Plane":
                    _players[packet.id].inventory["Log"] -= 2;
                    break;
                case "Sail":
                    _players[packet.id].inventory["Rope"] -= 1;
                    _players[packet.id].inventory["Log"] -= 1;
                    break;
                case "Paddle":
                    _players[packet.id].inventory["Log"] -= 1;
                    break;
            }
            AddItemInGlobal(packet.itemName);

            sendPacket.inventory = _players[packet.id].inventory;
            sendPacket.itemName = packet.itemName;
            sendPacket.globalItem = _globalItem;

            sendPacket.PacketToByteArray((byte)STOC_OP.STOC_ITEM_CRAFTED);

            token.SendData(sendPacket);

            foreach (var tuple in _players[packet.id].inventory)
            {
                Console.Write("key - {0}, value - {1}", tuple.Key, tuple.Value);
            }
            Console.WriteLine("");

            foreach (var tuple in _globalItem)
            {
                Console.Write("key - {0}, value - {1}", tuple.Key, tuple.Value);
            }
            Console.WriteLine("");

            foreach (var tuple in _players)
            {
                if (tuple.Key != packet.id)
                {
                    var tmpToken = tuple.Value.asyncUserToken;

                    ItemCraftPacket otherPacket = new ItemCraftPacket(new byte[Defines.BUF_SIZE], 0);
                    otherPacket.id = sendPacket.id;
                    otherPacket.name = sendPacket.name;
                    otherPacket.time = sendPacket.time;

                    otherPacket.itemName = sendPacket.itemName;
                    otherPacket.inventory = sendPacket.inventory;
                    otherPacket.globalItem = sendPacket.globalItem;
                    otherPacket.PacketToByteArray((byte)STOC_OP.STOC_ITEM_CRAFTED);

                    tmpToken.SendData(otherPacket);
                }
            }
        }
        private void ProcessAttack(AsyncUserToken token)
        {
            //누가 어떤 플레이어를 공격했는가?
            //공격당한 플레이어를 죽게 하고 공격한 플레이어 타이머 리셋
            MutantPacket packet = new MutantPacket(token.readEventArgs.Buffer, token.readEventArgs.Offset);
            packet.ByteArrayToPacket();

            foreach (var tuple in _players)
            {
                var tmpToken = tuple.Value.asyncUserToken;

                //기존의 플레이어에게 새로운 플레이어 정보 전달
                PlayerStatusPacket sendPacket = new PlayerStatusPacket(new byte[Defines.BUF_SIZE], 0);
                sendPacket.id = packet.id;
                sendPacket.name = packet.name;
                sendPacket.time = 0;

                sendPacket.position = _players[packet.id].position;
                sendPacket.rotation = _players[packet.id].rotation;
                sendPacket.playerMotion = Defines.PLAYER_HIT;
                sendPacket.PacketToByteArray((byte)STOC_OP.STOC_KILLED);

                tmpToken.SendData(sendPacket);
            }
        }

        private void ProcessItemEvent(AsyncUserToken token)
        {
            ItemEventPacket packet = new ItemEventPacket(token.readEventArgs.Buffer, token.readEventArgs.Offset);
            packet.ByteArrayToPacket();
            var cnt = 0;
            //인벤토리에 존재하는 아이템의 개수 구하기
            foreach (var tuple in _players[token.userID].inventory)
            {
                cnt += tuple.Value;
            }

            ItemEventPacket sendPacket = new ItemEventPacket(new byte[Defines.BUF_SIZE], 0);

            //만약 아이템의 총 개수가 3개보다 많다면 획득하지 못함
            if (cnt > 2)
            {
                sendPacket.id = packet.id;
                sendPacket.name = packet.name;
                sendPacket.itemName = packet.itemName;
                sendPacket.inventory = packet.inventory;
                sendPacket.canGainItem = false;
                sendPacket.PacketToByteArray((byte)STOC_OP.STOC_ITEM_DENIED);
            }
            //그렇지 않은 경우는 아이템 획득 가능
            else
            {
                if (_players[packet.id].inventory.ContainsKey(packet.itemName))
                {
                    _players[packet.id].inventory[packet.itemName] += 1;
                }
                else
                {
                    _players[packet.id].inventory.Add(packet.itemName, 1);
                }
                sendPacket.id = packet.id;
                sendPacket.name = packet.name;
                sendPacket.itemName = packet.itemName;
                sendPacket.inventory = _players[packet.id].inventory;
                sendPacket.canGainItem = true;
                sendPacket.PacketToByteArray((byte)STOC_OP.STOC_ITEM_GAIN);
            }

            foreach (var tuple in sendPacket.inventory)
            {
                Console.Write("key - {0}, value - {1}", tuple.Key, tuple.Value);
            }
            Console.WriteLine("");

            token.SendData(sendPacket);
        }

        private void ProcessStatus(AsyncUserToken token)
        {
            PlayerStatusPacket packet = new PlayerStatusPacket(token.readEventArgs.Buffer, token.readEventArgs.Offset);
            packet.ByteArrayToPacket();

            if (!(_players.ContainsKey(packet.id)))
            {
                return;
            }
            _players[packet.id].position = packet.position;
            _players[packet.id].rotation = packet.rotation;

            foreach (var tuple in _players)
            {
                if (tuple.Key != packet.id)
                {
                    var tmpToken = tuple.Value.asyncUserToken;
                    PlayerStatusPacket sendPacket = new PlayerStatusPacket(tmpToken.writeEventArgs.Buffer, tmpToken.writeEventArgs.Offset);
                    sendPacket.id = packet.id;
                    sendPacket.name = packet.name;
                    sendPacket.playerMotion = packet.playerMotion;
                    sendPacket.time = 0;
                    sendPacket.position = _players[packet.id].position;
                    sendPacket.rotation = _players[packet.id].rotation;

                    sendPacket.PacketToByteArray((byte)STOC_OP.STOC_STATUS_CHANGE);

                    tmpToken.SendData(sendPacket);
                }
            }
        }

        public bool IsHavePlayer(int id)
        {
            foreach (var tuple in _players)
            {
                if(tuple.Key == id)
                {
                    return true;
                }
            }
            return false;
        }

        public void Update(object elapsedTime)
        {
            var iter = _players.GetEnumerator();
            do
            {
                var token = iter.Current.Value.asyncUserToken;
            } while (iter.MoveNext());
        }
    }
}
