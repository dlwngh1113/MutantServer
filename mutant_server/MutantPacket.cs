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
        protected void ConvertToByte(int i)
        {
            byte[] tmp = BitConverter.GetBytes(i);
            tmp.CopyTo(ary, offset);
            offset += tmp.Length;
        }
        protected void ConvertToByte(string s)
        {
            byte[] tmp = Encoding.UTF8.GetBytes(s);

            short len = (short)tmp.Length;
            byte[] len_buffer = BitConverter.GetBytes(len);
            len_buffer.CopyTo(this.ary, this.offset);
            this.offset += sizeof(short);

            tmp.CopyTo(this.ary, this.offset);
            this.offset += tmp.Length;
        }
        protected void ConvertToByte(byte b)
        {
            byte[] tmp = BitConverter.GetBytes(b);
            tmp.CopyTo(this.ary, this.offset);
            this.offset += sizeof(byte);
        }
        protected void ConvertToByte(float f)
        {
            byte[] tmp = BitConverter.GetBytes(f);
            tmp.CopyTo(ary, offset);
            offset += tmp.Length;
        }
        protected void ConvertToByte(Vector3 vec)
        {
            ConvertToByte(vec.X);
            ConvertToByte(vec.Y);
            ConvertToByte(vec.Z);
        }
        protected int ByteToInt()
        {
            int tmp = BitConverter.ToInt32(this.ary, this.offset);
            this.offset += sizeof(int);
            return tmp;
        }
        protected string ByteToString()
        {
            int len = BitConverter.ToInt16(this.ary, this.offset);
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
        protected Vector3 ByteToVector3()
        {
            Vector3 tmp;
            tmp.X = ByteToFloat();
            tmp.Y = ByteToFloat();
            tmp.Z = ByteToFloat();

            return tmp;
        }
        protected byte ByteToByte()
        {
            byte tmp = (byte)BitConverter.ToInt32(this.ary, this.offset);
            this.offset += sizeof(byte);
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
        public Vector3 position;
        public Vector3 rotation;
        public Vector3 posVel;
        public Vector3 rotateVel;
        public PlayerStatusPacket(byte[] ary, int offset):base(ary, offset)
        {
            this.position = new Vector3();
            this.posVel = new Vector3();
            this.rotation = new Vector3();
            this.rotateVel = new Vector3();
        }
        public override void PacketToByteArray(byte type)
        {
            base.PacketToByteArray(type);
            ConvertToByte(position);
            ConvertToByte(rotation);
            ConvertToByte(posVel);
            ConvertToByte(rotateVel);
        }
        public override void ByteArrayToPacket()
        {
            base.ByteArrayToPacket();
            this.position = ByteToVector3();
            this.rotation = ByteToVector3();
            this.posVel = ByteToVector3();
            this.rotateVel = ByteToVector3();
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