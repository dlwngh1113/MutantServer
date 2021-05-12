using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace mutant_server
{
    public class MutantPacket
    {
        public Header header;
        public string name = null;
        public int id;
        public int time;
        public byte[] ary;
        public int offset = 0;
        public int startPos = 0;
        public ushort size
        {
            get => (ushort)(ary.Length - Header.size);
        }
        public MutantPacket(byte[] ary, int p)
        {
            this.ary = ary;
            startPos = offset = p;
            header = new Header();
        }
        public void Copy(MutantPacket packet)
        {
            Array.Copy(packet.ary, packet.offset, ary, offset, Defines.BUF_SIZE);
        }
        protected void ConvertToByte(int i)
        {
            byte[] tmp = BitConverter.GetBytes(i);
            tmp.CopyTo(this.ary, this.offset);
            this.offset += tmp.Length;
        }
        protected void ConvertToByte(string s)
        {
            byte[] tmp = Encoding.UTF8.GetBytes(s);

            short len = (short)tmp.Length;
            byte[] len_buffer = BitConverter.GetBytes(len);
            len_buffer.CopyTo(this.ary, this.offset);
            this.offset += sizeof(short);

            tmp.CopyTo(this.ary, this.offset);
            this.offset += len;
        }
        protected void ConvertToByte(byte b)
        {
            this.ary[this.offset++] = b;
        }
        protected void ConvertToByte(float f)
        {
            byte[] tmp = BitConverter.GetBytes(f);
            tmp.CopyTo(this.ary, this.offset);
            this.offset += tmp.Length;
        }

        protected void ConvertToByte(MyVector3 vec)
        {
            ConvertToByte(vec.x);
            ConvertToByte(vec.y);
            ConvertToByte(vec.z);
        }

        protected void ConvertToByte(bool b)
        {
            byte[] tmp = BitConverter.GetBytes(b);
            tmp.CopyTo(this.ary, this.offset);
            this.offset += tmp.Length;
        }
        protected int ByteToInt()
        {
            int tmp = BitConverter.ToInt32(this.ary, this.offset);
            this.offset += sizeof(int);
            return tmp;
        }
        protected string ByteToString()
        {
            short len = BitConverter.ToInt16(this.ary, this.offset);
            this.offset += sizeof(short);

            string tmp = Encoding.UTF8.GetString(this.ary, this.offset, len);
            this.offset += len;

            return tmp;
        }

        protected bool ByteToBool()
        {
            bool tmp = BitConverter.ToBoolean(this.ary, this.offset);
            this.offset += sizeof(bool);

            return tmp;
        }
        protected float ByteToFloat()
        {
            float tmp = BitConverter.ToSingle(this.ary, this.offset);
            this.offset += sizeof(float);
            return tmp;
        }
        protected byte ByteToByte()
        {
            byte tmp = this.ary[this.offset++];
            return tmp;
        }
        protected MyVector3 ByteToVector()
        {
            MyVector3 tmp = new MyVector3();
            tmp.x = ByteToFloat();
            tmp.y = ByteToFloat();
            tmp.z = ByteToFloat();

            return tmp;
        }
        protected ushort ByteToUshort()
        {
            ushort tmp = BitConverter.ToUInt16(this.ary, this.offset);
            this.offset += sizeof(ushort);
            return tmp;
        }
        public virtual void PacketToByteArray(byte type)
        {
            //ConvertToByte(this.header.bytes);
            //ConvertToByte(this.header.op);
            ary[offset++] = type;
            ConvertToByte(this.name);
            ConvertToByte(this.id);
            ConvertToByte(this.time);
        }
        public virtual void ByteArrayToPacket()
        {
            //this.header.bytes = ByteToUshort();
            //this.header.op = ByteToByte();
            this.offset++;
            this.name = ByteToString();
            this.id = ByteToInt();
            this.time = ByteToInt();
        }
    }

    public class ItemEventPacket : MutantPacket
    {
        public string itemName;
        public Dictionary<string, int> inventory;
        public bool canGainItem;
        public ushort size
        {
            get => (ushort)(ary.Length - Header.size);
        }
        public ItemEventPacket(byte[] ary, int offset) : base(ary, offset)
        {
        }
        public void Copy(ItemEventPacket packet, byte type = Defines.STOC_ITEM_GAIN)
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
            canGainItem = ByteToBool();
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
            ConvertToByte(canGainItem);
        }
    }
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
        public void Copy(ItemCraftPacket packet, byte type = Defines.STOC_ITEM_GAIN)
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
    public class PlayerStatusPacket : MutantPacket
    {
        public MyVector3 position;
        public MyVector3 rotation;
        public byte playerMotion;
        public byte playerJob;
        public ushort size
        {
            get => (ushort)(ary.Length - Header.size);
        }

        public PlayerStatusPacket(byte[] ary, int offset) : base(ary, offset)
        {
        }
        public void Copy(PlayerStatusPacket packet, byte type = Defines.STOC_STATUS_CHANGE)
        {
            Array.Copy(packet.ary, packet.offset, ary, offset, Defines.BUF_SIZE);
            ary[offset] = type;
        }
        public override void PacketToByteArray(byte type)
        {
            base.PacketToByteArray(type);

            ConvertToByte(this.position);
            ConvertToByte(this.rotation);
            ConvertToByte(this.playerMotion);
            ConvertToByte(this.playerJob);
        }
        public override void ByteArrayToPacket()
        {
            base.ByteArrayToPacket();

            this.position = ByteToVector();
            this.rotation = ByteToVector();
            this.playerMotion = ByteToByte();
            this.playerJob = ByteToByte();
        }
    }
    public class ChattingPakcet : MutantPacket
    {
        public string message;
        public ushort size
        {
            get => (ushort)(ary.Length - Header.size);
        }
        public ChattingPakcet(byte[] ary, int offset) : base(ary, offset)
        {

        }
        public void Copy(ChattingPakcet packet, byte type = Defines.STOC_CHAT)
        {
            Array.Copy(packet.ary, packet.offset, ary, offset, Defines.BUF_SIZE);
            ary[offset] = type;
        }
        public override void PacketToByteArray(byte type)
        {
            base.PacketToByteArray(type);
            ConvertToByte(message);
        }
        public override void ByteArrayToPacket()
        {
            base.ByteArrayToPacket();
            this.message = ByteToString();
        }
    }
}