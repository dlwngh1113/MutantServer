using mutant_server.Packets;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Timers;

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

        private Dictionary<int, List<int>> _chestItems;
        private Dictionary<int, int> _globalItem;
        private Dictionary<string, int> _voteCounter;
        private byte[] jobArray = Defines.GenerateRandomJobs();
        private MyVector3[] initPosAry = { new MyVector3(95.05f, 15.4f, 47.55f),
            new MyVector3(94.05f, 15.4f, 45.18f), new MyVector3(91.49f, 15.4f, 45.57f),
            new MyVector3(89.98f, 15.4f, 48.5f), new MyVector3(92.11f, 15.4f, 50.36f) };
        private bool[] isLoaded;
        private int globalOffset = 0;
        private string roomTitle;
        private byte gameState = Defines.ROOM_WAIT;
        public byte GameState
        {
            get => gameState;
            set => gameState = value;
        }

        public int PlayerNum
        {
            get => _players.Count;
        }
        public string RoomTitle
        {
            get => roomTitle;
            private set => roomTitle = value;
        }

        public void SetRoomTitle(string s)
        {
            roomTitle = s;
        }

        public Room()
        {
            _players = new Dictionary<int, Client>();
            _messageResolver = new MessageResolver();
            _globalItem = new Dictionary<int, int>();
            _voteCounter = new Dictionary<string, int>();
            _chestItems = new Dictionary<int, List<int>>();
            SetItemChest();
        }

        public void AddPlayer(int id, Client c)
        {
            lock (this._players)
            {
                this._players.Add(id, c);
            }
        }

        public void ResolveMessage(AsyncUserToken token, byte[] data)
        {
            RecvEventSelect(token, data);
        }
        private void RecvEventSelect(AsyncUserToken token, byte[] data)
        {
            switch ((CTOS_OP)data[0])
            {
                case CTOS_OP.CTOS_GAME_INIT:
                    ProcessGameInit(token, data);
                    break;
                case CTOS_OP.CTOS_STATUS_CHANGE:
                    ProcessStatus(token, data);
                    break;
                case CTOS_OP.CTOS_ATTACK:
                    ProcessAttack(token, data);
                    break;
                case CTOS_OP.CTOS_CHAT:
                    ProcessChatting(token, data);
                    break;
                case CTOS_OP.CTOS_LEAVE_ROOM:
                    ProcessLeaveRoom(token, data);
                    break;
                case CTOS_OP.CTOS_LEAVE_GAME:
                    ProcessLeaveGame(token, data);
                    break;
                case CTOS_OP.CTOS_ITEM_CLICKED:
                    ProcessItemEvent(token, data);
                    break;
                case CTOS_OP.CTOS_GET_ROOM_USERS:
                    ProcessGetRoomUsers(token, data);
                    break;
                case CTOS_OP.CTOS_ITEM_CRAFT_REQUEST:
                    ProcessItemCraft(token, data);
                    break;
                case CTOS_OP.CTOS_VOTE_SELECTED:
                    ProcessVote(token, data);
                    break;
                case CTOS_OP.CTOS_VOTE_REQUEST:
                    ProcessStartVote(token, data);
                    break;
                case CTOS_OP.CTOS_READY:
                    ProcessReady(token, data);
                    break;
                case CTOS_OP.CTOS_LOADED:
                    ProcessLoadGame(token, data);
                    break;
                case CTOS_OP.CTOS_ITEM_DELETE:
                    ProcessItemDelete(token, data);
                    break;
                case CTOS_OP.CTOS_SABOTAGI:
                    ProcessSabotagi(token, data);
                    break;
                default:
                    throw new Exception("operation from client is not valid\n");
            }
        }

        private void SetItemChest()
        {
            //file io를 통한 아이템 생성
            foreach (string line in File.ReadLines(@"..\..\Files\item.txt"))
            {
                string[] strs = line.Split(' ');
                int index = Int32.Parse(strs[0]);
                _chestItems.Add(index, new List<int>());
                for (int i = 1; i < strs.Length; ++i)
                {
                    _chestItems[index].Add(Int32.Parse(strs[i]));
                }
            }
        }

        private void SetGameInit()
        {
            for (int i = 0; i < _players.Count; ++i)
            {
                Client c = _players.ElementAt(i).Value;
                c.InitPos = initPosAry[globalOffset];
                c.job = jobArray[globalOffset];
                c.inventory.Clear();
                Interlocked.Increment(ref globalOffset);
            }

            Interlocked.Exchange(ref globalOffset, 0);
            isLoaded = new bool[_players.Count];
            Console.WriteLine("isloaded[globalOffset] is {0}", isLoaded[globalOffset]);
        }

        private GameInitPacket SetGameInitPacket(MutantPacket packet)
        {
            GameInitPacket sendPacket = new GameInitPacket(new byte[Defines.BUF_SIZE], 0);
            sendPacket.id = packet.id;
            sendPacket.name = packet.name;
            sendPacket.time = packet.time;
            sendPacket.pCount = _players.Count;

            Console.WriteLine("game init packet size - {0}, id - {1}, name - {2}, offset - {3}", packet.header.bytes, packet.id, packet.name, packet.offset);

            for (int i=0;i<_players.Count;++i)
            {
                Client c = _players.ElementAt(i).Value;
                sendPacket.names.Add(c.userName);
                sendPacket.IDs.Add(c.userID);
                sendPacket.positions.Add(c.InitPos);
                sendPacket.jobs.Add(c.job);

                //Console.WriteLine("packet name {0}, name {1}, id {2}", packet.name, c.userName, c.userID);
            }

            sendPacket.chestItems = _chestItems;

            return sendPacket;
        }

        private void ProcessGameInit(AsyncUserToken token, byte[] data)
        {
            MutantPacket packet = new MutantPacket(data, 0);
            packet.ByteArrayToPacket();

            GameInitPacket sendPacket = SetGameInitPacket(packet);
            sendPacket.PacketToByteArray((byte)STOC_OP.STOC_GAME_INIT);

            token.SendData(sendPacket);
        }

        private void ProcessReady(AsyncUserToken token, byte[] data)
        {
            MutantPacket packet = new MutantPacket(data, 0);
            packet.ByteArrayToPacket();

            Console.WriteLine("ready packet size - {0}, id - {1}, name - {2}, offset - {3}", packet.header.bytes, packet.id, packet.name, packet.offset);

            _players[packet.id].isReady = !_players[packet.id].isReady;

            //Console.WriteLine("{0} player ready is {1}", packet.name, _players[packet.id].isReady);

            byte readyCount = 0;
            foreach(var t in _players)
            {
                if(t.Value.isReady)
                {
                    ++readyCount;
                }
            }

            if(readyCount >= 2)
            {
                SetGameInit();
                ProcessGameStart();
            }
            else
            {
                foreach (var tuple in _players)
                {
                    var tmpToken = tuple.Value.asyncUserToken;
                    if (tuple.Value.isReady)
                    {
                        ++readyCount;
                    }
                    MutantPacket sendPacket = new MutantPacket(new byte[Defines.BUF_SIZE], 0);
                    sendPacket.id = packet.id;
                    sendPacket.name = packet.name;
                    sendPacket.time = _players[packet.id].isReady ? 1 : 0;

                    sendPacket.PacketToByteArray((byte)STOC_OP.STOC_READY);

                    tmpToken.SendData(sendPacket);
                }
            }
        }

        private void ProcessLoadGame(AsyncUserToken token, byte[] data)
        {
            MutantPacket packet = new MutantPacket(data, 0);
            packet.ByteArrayToPacket();

            isLoaded[globalOffset] = true;
            Interlocked.Increment(ref globalOffset);

            Console.WriteLine("load game packet size - {0}, id - {1}, name - {2}, offset - {3}", packet.header.bytes, packet.id, packet.name, packet.offset);

            if (globalOffset >= _players.Count)
            {
                foreach(var p in _players)
                {
                    MutantPacket sendPacket = new MutantPacket(new byte[Defines.BUF_SIZE], 0);
                    sendPacket.id = p.Key;
                    sendPacket.name = p.Value.userName;
                    sendPacket.time = 0;

                    sendPacket.PacketToByteArray((byte)STOC_OP.ALL_PLAYER_LOADED);

                    p.Value.asyncUserToken.SendData(sendPacket);
                }

                System.Timers.Timer timer = new System.Timers.Timer();
                timer.Interval = 16.6666f;
                timer.Elapsed += new ElapsedEventHandler(Update);
                //timer.Start();

                gameState = Defines.ROOM_PLAYING;
            }
        }

        private void ProcessGameStart()
        {
            foreach (var tuple in _players)
            {
                MutantPacket packet = new MutantPacket(new byte[Defines.BUF_SIZE], 0);
                packet.id = tuple.Key;
                packet.name = tuple.Value.userName;
                packet.time = 0;

                packet.PacketToByteArray((byte)STOC_OP.STOC_GAME_START);

                tuple.Value.asyncUserToken.SendData(packet);
            }
        }
        private void ProcessGetRoomUsers(AsyncUserToken token, byte[] data)
        {
            MutantPacket packet = new MutantPacket(data, 0);
            packet.ByteArrayToPacket();

            Console.WriteLine("get room users packet size - {0}, id - {1}, name - {2}, offset - {3}", packet.header.bytes, packet.id, packet.name, packet.offset);

            foreach (var tuple in _players)
            {
                if (tuple.Key != packet.id)
                {
                    //this player to other player
                    MutantPacket sendPacket = new MutantPacket(new byte[Defines.BUF_SIZE], 0);

                    sendPacket.id = packet.id;
                    sendPacket.name = packet.name;
                    sendPacket.time = 0;
                    sendPacket.PacketToByteArray((byte)STOC_OP.STOC_PLAYER_ENTER);

                    tuple.Value.asyncUserToken.SendData(sendPacket);


                    //other player to this player
                    MutantPacket pPacket = new MutantPacket(new byte[Defines.BUF_SIZE], 0);
                    pPacket.id = tuple.Key;
                    pPacket.name = tuple.Value.userName;
                    pPacket.time = 0;
                    pPacket.PacketToByteArray((byte)STOC_OP.STOC_PLAYER_ENTER);

                    token.SendData(pPacket);
                }
            }
        }

        private void ProcessLeaveRoom(AsyncUserToken token, byte[] data)
        {
            MutantPacket packet = new MutantPacket(data, 0);
            packet.ByteArrayToPacket();

            Console.WriteLine("leave room packet size - {0}, id - {1}, name - {2}, offset - {3}", packet.header.bytes, packet.id, packet.name, packet.offset);

            foreach (var tuple in _players)
            {
                if (tuple.Key != packet.id)
                {
                    var tmpToken = tuple.Value.asyncUserToken;
                    MutantPacket sendPacket = new MutantPacket(new byte[Defines.BUF_SIZE], 0);
                    sendPacket.name = packet.name;
                    sendPacket.id = packet.id;
                    sendPacket.time = packet.time;

                    sendPacket.PacketToByteArray((byte)STOC_OP.STOC_PLAYER_LEAVE_ROOM);

                    tmpToken.SendData(sendPacket);
                }
            }

            lock (_players)
            {
                _players.Remove(packet.id);
            }

            if (PlayerNum < 1)
            {
                lock (Server._roomsInServer)
                {
                    Server._roomsInServer.Remove(this);
                }
            }
        }

        private void ProcessLeaveGame(AsyncUserToken token, byte[] data)
        {
            MutantPacket packet = new MutantPacket(data, 0);
            packet.ByteArrayToPacket();

            Console.WriteLine("leave game packet size - {0}, id - {1}, name - {2}, offset - {3}", packet.header.bytes, packet.id, packet.name, packet.offset);

            foreach (var tuple in _players)
            {
                if (tuple.Key != packet.id)
                {
                    var tmpToken = tuple.Value.asyncUserToken;
                    MutantPacket sendPacket = new MutantPacket(new byte[Defines.BUF_SIZE], 0);
                    sendPacket.name = packet.name;
                    sendPacket.id = packet.id;
                    sendPacket.time = packet.time;

                    sendPacket.PacketToByteArray((byte)STOC_OP.STOC_PLAYER_LEAVE_GAME);

                    tmpToken.SendData(sendPacket);
                }
            }

            lock(_players)
            {
                _players.Remove(packet.id);
            }

            closeMethod(token.readEventArgs);

            if (PlayerNum < 1)
            {
                lock (Server._roomsInServer)
                {
                    Server._roomsInServer.Remove(this);
                }
            }
        }
        private void ProcessChatting(AsyncUserToken token, byte[] data)
        {
            //클라이언트에서 온 메세지를 모든 클라이언트에 전송
            ChattingPakcet packet = new ChattingPakcet(data, 0);
            packet.ByteArrayToPacket();

            Console.WriteLine("chatting packet size - {0}, id - {1}, name - {2}, offset - {3}", packet.header.bytes, packet.id, packet.name, packet.offset);

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

        private void AddItemInGlobal(int itemNumber)
        {
            if (itemNumber == Defines.ITEM_AXE)
            {
                return;
            }
            if (_globalItem.ContainsKey(itemNumber))
            {
                _globalItem[itemNumber]++;
            }
            else
            {
                _globalItem.Add(itemNumber, 1);
            }
        }

        public void ProcessStartVote(AsyncUserToken token, byte[] data)
        {
            MutantPacket packet = new MutantPacket(data, 0);
            packet.ByteArrayToPacket();

            Console.WriteLine("vote start packet size - {0}, id - {1}, name - {2}, offset - {3}", packet.header.bytes, packet.id, packet.name, packet.offset);

            _voteCounter.Clear();

            //for (int i = 0; i < _players.Count; ++i)
            //{
            //    var tuple = _players.ElementAt(i);
            //    tuple.Value.position = tuple.Value.InitPos;
            //    for (int j = i; j < _players.Count; ++j)
            //    {
            //        var tmpToken = _players.ElementAt(j).Value.asyncUserToken;
            //        PlayerStatusPacket posPacket = new PlayerStatusPacket(new byte[Defines.BUF_SIZE], 0);
            //        posPacket.id = tuple.Key;
            //        posPacket.name = tuple.Value.userName;
            //        posPacket.time = 0;

            //        posPacket.position = tuple.Value.position;
            //        posPacket.rotation = tuple.Value.rotation;
            //        posPacket.playerMotion = Defines.PLAYER_IDLE;

            //        posPacket.PacketToByteArray((byte)STOC_OP.STOC_STATUS_CHANGE);

            //        tmpToken.SendData(posPacket);
            //    }
            //}

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
        public void ProcessVote(AsyncUserToken token, byte[] data)
        {
            VotePacket packet = new VotePacket(data, 0);
            packet.ByteArrayToPacket();

            Console.WriteLine("vote packet size - {0}, id - {1}, name - {2}, offset - {3}", packet.header.bytes, packet.id, packet.name, packet.offset);

            if (!_voteCounter.ContainsKey(packet.votedPersonID))
            {
                _voteCounter.Add(packet.votedPersonID, 1);
            }
            else
            {
                _voteCounter[packet.votedPersonID] += 1;
            }

            foreach (var i in _voteCounter)
            {
                Console.WriteLine("vote name = {0}, count = {1}", i.Key, i.Value);
            }

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

        private void ProcessItemEvent(AsyncUserToken token, byte[] data)
        {
            ItemEventPacket packet = new ItemEventPacket(data, 0);
            packet.ByteArrayToPacket();

            Console.WriteLine("item get packet size - {0}, id - {1}, name - {2}, offset - {3}", packet.header.bytes, packet.id, packet.name, packet.offset);

            var cnt = 0;
            //인벤토리에 존재하는 아이템의 개수 구하기
            foreach (var tuple in _players[token.userID].inventory)
            {
                cnt += tuple.Value;
            }

            //만약 아이템의 총 개수가 3개보다 많다면 획득하지 못함
            if (cnt > 2)
            {
                ItemEventPacket sendPacket = new ItemEventPacket(new byte[Defines.BUF_SIZE], 0);
                sendPacket.id = packet.id;
                sendPacket.name = packet.name;
                sendPacket.chestItem = packet.chestItem;
                sendPacket.inventory = packet.inventory;
                sendPacket.canGainItem = false;
                sendPacket.PacketToByteArray((byte)STOC_OP.STOC_ITEM_DENIED);
            }
            //그렇지 않은 경우는 아이템 획득 가능
            else
            {
                _chestItems[packet.chestItem.Item1].Remove(packet.chestItem.Item2);
                if (_players[packet.id].inventory.ContainsKey(packet.chestItem.Item2))
                {
                    _players[packet.id].inventory[packet.chestItem.Item2] += 1;
                }
                else
                {
                    _players[packet.id].inventory.Add(packet.chestItem.Item2, 1);
                }

                foreach (var tuple in _players)
                {
                    ItemEventPacket sendPacket = new ItemEventPacket(new byte[Defines.BUF_SIZE], 0);

                    sendPacket.id = packet.id;
                    sendPacket.name = packet.name;
                    sendPacket.chestItem = packet.chestItem;
                    sendPacket.inventory = _players[packet.id].inventory;
                    sendPacket.canGainItem = true;
                    sendPacket.PacketToByteArray((byte)STOC_OP.STOC_ITEM_GAIN);

                    tuple.Value.asyncUserToken.SendData(sendPacket);
                }
            }
        }

        private void ProcessItemCraft(AsyncUserToken token, byte[] data)
        {
            ItemCraftPacket packet = new ItemCraftPacket(data, 0);
            packet.ByteArrayToPacket();

            Console.WriteLine("item craft packet size - {0}, id - {1}, name - {2}, offset - {3}", packet.header.bytes, packet.id, packet.name, packet.offset);

            ItemCraftPacket sendPacket = new ItemCraftPacket(new byte[Defines.BUF_SIZE], 0);
            sendPacket.id = packet.id;
            sendPacket.name = packet.name;
            sendPacket.time = packet.time;

            if (!_players.ContainsKey(packet.id))
            {
                return;
            }

            Console.WriteLine("packet.itemName - {0}", packet.itemNumber);
            switch (packet.itemNumber)
            {
                case Defines.ITEM_AXE:
                    _players[packet.id].inventory[Defines.ITEM_STICK] -= 1;
                    _players[packet.id].inventory[Defines.ITEM_ROCK] -= 1;
                    if (!_players[packet.id].inventory.ContainsKey(Defines.ITEM_AXE))
                    {
                        _players[packet.id].inventory.Add(Defines.ITEM_AXE, 1);
                    }
                    break;
                case Defines.ITEM_PLANE:
                    _players[packet.id].inventory[Defines.ITEM_LOG] -= 2;
                    break;
                case Defines.ITEM_SAIL:
                    _players[packet.id].inventory[Defines.ITEM_ROPE] -= 1;
                    _players[packet.id].inventory[Defines.ITEM_LOG] -= 1;
                    break;
                case Defines.ITEM_PADDLE:
                    _players[packet.id].inventory[Defines.ITEM_LOG] -= 1;
                    break;
            }
            AddItemInGlobal(packet.itemNumber);

            sendPacket.inventory = _players[packet.id].inventory;
            sendPacket.itemNumber = packet.itemNumber;
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

                    otherPacket.itemNumber = sendPacket.itemNumber;
                    otherPacket.inventory = sendPacket.inventory;
                    otherPacket.globalItem = _globalItem;
                    otherPacket.PacketToByteArray((byte)STOC_OP.STOC_ITEM_CRAFTED);

                    tmpToken.SendData(otherPacket);
                }
            }
        }

        private void ProcessItemDelete(AsyncUserToken token, byte[] data)
        {
            ItemEventPacket packet = new ItemEventPacket(data, 0);
            packet.ByteArrayToPacket();

            Console.WriteLine("item delete packet size - {0}, id - {1}, name - {2}, offset - {3}", packet.header.bytes, packet.id, packet.name, packet.offset);

            foreach (var v in _players[packet.id].inventory)
            {
                Console.WriteLine("before delete {0} - {1}", v.Key, v.Value);
            }

            _players[packet.id].inventory[packet.chestItem.Item2] -= 1;

            ItemEventPacket sendPacket = new ItemEventPacket(new byte[Defines.BUF_SIZE], 0);
            sendPacket.id = packet.id;
            sendPacket.name = packet.name;
            sendPacket.time = 0;

            foreach(var v in _players[packet.id].inventory)
            {
                Console.WriteLine("after delete {0} - {1}", v.Key, v.Value);
            }

            sendPacket.inventory = _players[packet.id].inventory;
            sendPacket.chestItem = new Tuple<int, int>(0, 0);

            sendPacket.PacketToByteArray((byte)STOC_OP.STOC_ITEM_DELETE);

            token.SendData(sendPacket);
        }

        private void ProcessAttack(AsyncUserToken token, byte[] data)
        {
            //누가 어떤 플레이어를 공격했는가?
            //공격당한 플레이어를 죽게 하고 공격한 플레이어 타이머 리셋
            MutantPacket packet = new MutantPacket(data, 0);
            packet.ByteArrayToPacket();

            Console.WriteLine("kill packet size - {0}, id - {1}, name - {2}, offset - {3}", packet.header.bytes, packet.id, packet.name, packet.offset);

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

        private void ProcessSabotagi(AsyncUserToken token, byte[] data)
        {
            MutantPacket packet = new MutantPacket(data, 0);
            packet.ByteArrayToPacket();

            Console.WriteLine("sabotagi packet size - {0}, id - {1}, name - {2}, offset - {3}", packet.header.bytes, packet.id, packet.name, packet.offset);

            foreach (var p in _players)
            {
                MutantPacket sendPacket = new MutantPacket(new byte[Defines.BUF_SIZE], 0);
                sendPacket.id = packet.id;
                sendPacket.name = packet.name;
                sendPacket.time = 0;

                sendPacket.PacketToByteArray((byte)STOC_OP.STOC_SABOTAGI);

                p.Value.asyncUserToken.SendData(sendPacket);
            }
        }

        private void ProcessStatus(AsyncUserToken token, byte[] data)
        {
            PlayerStatusPacket packet = new PlayerStatusPacket(data, 0);
            packet.ByteArrayToPacket();

            Console.WriteLine("status packet size - {0}, id - {1}, name - {2}, offset - {3}", packet.header.bytes, packet.id, packet.name, packet.offset);

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
                    PlayerStatusPacket sendPacket = new PlayerStatusPacket(new byte[Defines.BUF_SIZE], 0);
                    sendPacket.id = packet.id;
                    sendPacket.name = packet.name;
                    sendPacket.time = 0;

                    sendPacket.position = _players[packet.id].position;
                    sendPacket.rotation = _players[packet.id].rotation;
                    sendPacket.playerMotion = packet.playerMotion;
                    sendPacket.playerJob = _players[packet.id].job;

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

        public void Update(object elapsedTime, ElapsedEventArgs e)
        {
            foreach (var tuple in _players)
            {
                var tmpToken = tuple.Value.asyncUserToken;
                MutantPacket packet = new MutantPacket(new byte[Defines.BUF_SIZE], 0);
                packet.id = tuple.Key;
                packet.name = tuple.Value.userName;
                packet.time = 0.016f;

                packet.PacketToByteArray((byte)STOC_OP.STOC_SYSTEM_CHANGE);

                tmpToken.SendData(packet);
            }
        }
    }
}
