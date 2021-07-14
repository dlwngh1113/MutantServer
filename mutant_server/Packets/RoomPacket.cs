using System;
using System.Collections.Generic;

namespace mutant_server.Packets
{
    public class RoomPacket : MutantPacket
    {
        /// <summary>
        /// room name, playerCount
        /// </summary>
        public Dictionary<string, int> roomList;
        public RoomPacket(byte[] ary, int offset) : base(ary, offset)
        {
            roomList = new Dictionary<string, int>();
        }
        public void Copy(RoomPacket packet, byte type = (byte)STOC_OP.STOC_ROOM_CREATE_SUCCESS)
        {
            Array.Copy(packet.ary, packet.offset, ary, offset, Defines.BUF_SIZE);
            ary[offset] = type;
        }
        public override void PacketToByteArray(byte type)
        {
            base.PacketToByteArray(type);
            ConvertToByte(roomList.Count);
            foreach(var tuple in roomList)
            {
                ConvertToByte(tuple.Key);
                ConvertToByte(tuple.Value);
            }
        }
        public override void ByteArrayToPacket()
        {
            base.ByteArrayToPacket();
            int cnt = ByteToInt();
            for(int i=0;i<cnt;++i)
            {
                roomList.Add(ByteToString(), ByteToInt());
            }
        }
    }
}
