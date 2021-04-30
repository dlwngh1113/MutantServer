using System;
using System.Numerics;
using System.Text;
using System.Runtime.InteropServices;
using System.Collections.Generic;

namespace mutant_server
{
    public class MutantPacket
    {
        Header header;
        public string name = null;
        public int id;
        public int time;
        protected byte[] ary;
        protected int offset = 0;
        public MutantPacket(byte[] ary, int p)
        {
            this.ary = ary;
            offset = p;
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
            ConvertToByte(this.header.bytes);
            ConvertToByte(this.header.op);
            ConvertToByte(this.name);
            ConvertToByte(this.id);
            ConvertToByte(this.time);
        }
        public virtual void ByteArrayToPacket()
        {
            this.header.bytes = ByteToUshort();
            this.header.op = ByteToByte();
            this.name = ByteToString();
            this.id = ByteToInt();
            this.time = ByteToInt();
        }
    }

    //[StructLayout(LayoutKind.Sequential, Pack = 1)]
    //public struct PlayerMouseEvent
    //{
    //    public string itemName;
    //    public Dictionary<string, int> inventory;
    //}
    public class ItemEventPacket : MutantPacket
    {
        public string itemName;
        public Dictionary<string, int> inventory;
        public bool canGainItem;
        public ItemEventPacket(byte[] ary, int offset) : base(ary, offset)
        {
        }
        public void Copy(ItemEventPacket packet, byte type = Defines.STOC_ITEM_GAIN)
        {
            ary[offset] = type;
            Array.Copy(packet.ary, packet.offset, ary, offset, Defines.BUF_SIZE);
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
    public class PlayerStatusPacket : MutantPacket
    {
        public MyVector3 position;
        public MyVector3 rotation;
        public byte playerMotion;

        public PlayerStatusPacket(byte[] ary, int offset) : base(ary, offset)
        {
        }
        public void Copy(PlayerStatusPacket packet, byte type = Defines.STOC_STATUS_CHANGE)
        {
            ary[offset] = type;
            Array.Copy(packet.ary, packet.offset, ary, offset, Defines.BUF_SIZE);
        }
        public override void PacketToByteArray(byte type)
        {
            base.PacketToByteArray(type);

            ConvertToByte(this.position);
            ConvertToByte(this.rotation);
            ConvertToByte(this.playerMotion);
        }
        public override void ByteArrayToPacket()
        {
            base.ByteArrayToPacket();

            this.position = ByteToVector();
            this.rotation = ByteToVector();
            this.playerMotion = ByteToByte();
        }
    }
    public class ChattingPakcet : MutantPacket
    {
        public string message;
        public ChattingPakcet(byte[] ary, int offset) : base(ary, offset)
        {

        }
        public void Copy(ChattingPakcet packet, byte type = Defines.STOC_CHAT)
        {
            ary[offset] = type;
            Array.Copy(packet.ary, packet.offset, ary, offset, Defines.BUF_SIZE);
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