using System;
using System.Collections.Generic;

namespace mutant_server.Packets
{
    public class ItemCraftPacket : MutantPacket
    {
        public string itemName;
        public Dictionary<string, int> inventory;
        public Dictionary<string, int> globalItem;
        public ushort size
        {
            get => (ushort)(ary.Length - Header.size);
        }
        public ItemCraftPacket(byte[] ary, int offset) : base(ary, offset)
        {
        }
        public void Copy(ItemCraftPacket packet, byte type = (byte)STOC_OP.STOC_ITEM_GAIN)
        {
            Array.Copy(packet.ary, packet.offset, ary, offset, Defines.BUF_SIZE);
            ary[offset] = type;
        }
        public override void ByteArrayToPacket()
        {
            base.ByteArrayToPacket();
            itemName = ByteToString();
            int cnt = ByteToInt();
            inventory = new Dictionary<string, int>();
            for (int i = 0; i < cnt; ++i)
            {
                var tKey = ByteToString();
                var tVal = ByteToInt();
                inventory.Add(tKey, tVal);
            }
            cnt = ByteToInt();
            globalItem = new Dictionary<string, int>();
            for (int i = 0; i < cnt; ++i)
            {
                var tKey = ByteToString();
                var tVal = ByteToInt();
                globalItem.Add(tKey, tVal);
            }
        }
        public override void PacketToByteArray(byte type)
        {
            base.PacketToByteArray(type);
            ConvertToByte(itemName);
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
        }
    }
}
