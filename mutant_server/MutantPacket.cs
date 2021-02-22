using System;
using System.Numerics;
using System.Text;

namespace mutant_server
{
    public class MutantPacket
    {
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
            this.name = packet.name;
            this.id = packet.id;
            this.time = packet.time;
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
        protected float ByteToFloat()
        {
            float tmp = BitConverter.ToSingle(this.ary, this.offset);
            this.offset += sizeof(float);
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
        public virtual void PacketToByteArray(byte type)
        {
            this.ary[offset++] = type;
            ConvertToByte(this.name);
            ConvertToByte(this.id);
            ConvertToByte(this.time);
        }
        public virtual void ByteArrayToPacket()
        {
            ++offset;
            this.name = ByteToString();
            this.id = ByteToInt();
            this.time = ByteToInt();
        }
    }
    public class PlayerStatusPacket : MutantPacket
    {
        public MyVector3 position;
        public MyVector3 rotation;
        public MyVector3 posVelocity;
        public MyVector3 rotVelocity;
        public PlayerStatusPacket(byte[] ary, int offset):base(ary, offset)
        {
        }
        public void Copy(PlayerStatusPacket packet)
        {
            this.name = packet.name;
            this.id = packet.id;
            this.time = packet.time;

            this.position = packet.position;
            this.rotation = packet.rotation;
            this.posVelocity = packet.posVelocity;
            this.rotVelocity = packet.rotVelocity;
        }
        public override void PacketToByteArray(byte type)
        {
            base.PacketToByteArray(type);

            ConvertToByte(this.position);
            ConvertToByte(this.rotation);
            ConvertToByte(this.posVelocity);
            ConvertToByte(this.rotVelocity);
        }
        public override void ByteArrayToPacket()
        {
            base.ByteArrayToPacket();

            this.position = ByteToVector();
            this.rotation = ByteToVector();
            this.posVelocity = ByteToVector();
            this.rotVelocity = ByteToVector();
        }
    }
    public class ChattingPakcet : MutantPacket
    {
        public string message;
        public ChattingPakcet(byte[] ary, int offset) : base(ary, offset)
        {

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