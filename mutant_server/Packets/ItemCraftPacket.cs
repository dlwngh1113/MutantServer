using System;
using System.Collections.Generic;

namespace mutant_server.Packets
{
    public class ItemCraftPacket : MutantPacket
    {
        public int itemNumber;
        public Dictionary<int, int> inventory;
        public Dictionary<int, int> globalItem;
        public ushort size
        {
            get => (ushort)(ary.Length - Header.size);
        }
        public ItemCraftPacket(byte[] ary, int offset) : base(ary, offset)
        {
        }
        public void Copy(ItemCraftPacket packet)
        {
            int len = packet.offset - packet.startPos;
            Array.Copy(packet.ary, packet.startPos, ary, offset, len);
            offset += len;
        }
        public override void ByteArrayToPacket()
        {
            base.ByteArrayToPacket();
            itemNumber = ByteToInt();
            int cnt = ByteToInt();
            inventory = new Dictionary<int, int>();
            for (int i = 0; i < cnt; ++i)
            {
                var tKey = ByteToInt();
                var tVal = ByteToInt();
                inventory.Add(tKey, tVal);
            }
            cnt = ByteToInt();
            globalItem = new Dictionary<int, int>();
            for (int i = 0; i < cnt; ++i)
            {
                var tKey = ByteToInt();
                var tVal = ByteToInt();
                globalItem.Add(tKey, tVal);
            }
        }
        public override void PacketToByteArray(byte type)
        {
            base.PacketToByteArray(type);
            ConvertToByte(itemNumber);
            ConvertToByte(inventory.Count);
            foreach (var tuple in inventory)
            {
                ConvertToByte(tuple.Key);
                ConvertToByte(tuple.Value);
            }
            ConvertToByte(globalItem.Count);
            foreach (var tuple in globalItem)
            {
                ConvertToByte(tuple.Key);
                ConvertToByte(tuple.Value);
            }

            base.AddHeader();
        }
    }
}
