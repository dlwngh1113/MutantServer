﻿using System;
using System.Text;

namespace mutant_server
{
    public class MutantPacket
    {
        public Header header;
        public string name = null;
        public int id;
        public float time;
        public byte[] ary;
        public int offset = 0;
        public int startPos = 0;
        public MutantPacket(byte[] ary, int p)
        {
            this.ary = ary;
            startPos = offset = p;
            header = new Header();
        }
        public void Copy(MutantPacket packet)
        {
            int len = packet.offset - packet.startPos;
            Array.Copy(packet.ary, packet.startPos, ary, offset, len);
            offset += len;
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

        protected void ConvertToByte(ushort us)
        {
            byte[] tmp = BitConverter.GetBytes(us);
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
            this.header.op = type;
            ConvertToByte(this.header.op);
            ConvertToByte(this.header.bytes);
            ConvertToByte(this.name);
            ConvertToByte(this.id);
            ConvertToByte(this.time);

            AddHeader();
        }
        public virtual void ByteArrayToPacket()
        {
            this.header.op = ByteToByte();
            this.header.bytes = ByteToUshort();
            this.name = ByteToString();
            this.id = ByteToInt();
            this.time = ByteToFloat();
        }

        public void AddHeader()
        {
            byte[] ary = BitConverter.GetBytes(header.op);
            Array.Copy(ary, 0, this.ary, 0, ary.Length);

            header.bytes = (ushort)(offset - Header.size);
            ary = BitConverter.GetBytes(header.bytes);
            Array.Copy(ary, 0, this.ary, 1, ary.Length);
        }
    }
}