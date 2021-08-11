using System;
using System.Collections.Generic;

namespace mutant_server.Packets
{
    public class RoomPacket : MutantPacket
    {
        public List<string> names;
        public List<int> numOfPlayers;
        public List<byte> gameState;
        public RoomPacket(byte[] ary, int offset) : base(ary, offset)
        {
            names = new List<string>();
            numOfPlayers = new List<int>();
            gameState = new List<byte>();
        }
        public void Copy(RoomPacket packet)
        {
            int len = packet.offset - packet.startPos;
            Array.Copy(packet.ary, packet.startPos, ary, offset, len);
            offset += len;
        }
        public override void PacketToByteArray(byte type)
        {
            base.PacketToByteArray(type);
            ConvertToByte(names.Count);
            for(int i=0;i<names.Count;++i)
            {
                ConvertToByte(names[i]);
                ConvertToByte(numOfPlayers[i]);
                ConvertToByte(gameState[i]);
            }

            base.AddHeader();
        }
        public override void ByteArrayToPacket()
        {
            base.ByteArrayToPacket();
            int cnt = ByteToInt();
            for(int i=0;i<cnt;++i)
            {
                names.Add(ByteToString());
                numOfPlayers.Add(ByteToInt());
                gameState.Add(ByteToByte());
            }
        }
    }
}
