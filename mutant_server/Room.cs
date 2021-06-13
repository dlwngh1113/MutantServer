using System;
using System.Collections.Generic;

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
        public Room()
        {
            _players = new Dictionary<int, Client>(5);
            _messageResolver = new MessageResolver();
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
                case CTOS_OP.CTOS_LOGOUT:
                    ProcessLogout(token);
                    break;
                case CTOS_OP.CTOS_ITEM_CLICKED:
                    ProcessItemEvent(token);
                    break;
                default:
                    throw new Exception("operation from client is not valid\n");
            }
        }

        private void ProcessLogout(AsyncUserToken token)
        {
            //현재까지의 게임 정보를 DB에 업데이트 후 접속 종료

            //지금 게임에 존재하는 유저들에게 해당 유저가 게임을 종료했음을 알림
            //지금 게임을 같이 하고 있는 유저들을 어떻게 구분할 것인가?
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
                //SendPacket.inventory Null Reference Exception
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

            if (!(Server.players.ContainsKey(packet.id)))
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

                    //Console.WriteLine("id = {0} x = {1} y = {2} z = {3}", packet.id, packet.position.x, packet.position.y, packet.position.z);

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
