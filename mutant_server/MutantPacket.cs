﻿using System;
using System.Numerics;

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
            int len = s.Length;
            byte[] len_buffer = BitConverter.GetBytes(len);
            len_buffer.CopyTo(ary, offset);
            offset += len_buffer.Length;

            byte[] tmp = System.Text.Encoding.UTF8.GetBytes(s);
            tmp.CopyTo(ary, offset);
            offset += tmp.Length;
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
            int tmp = BitConverter.ToInt32(ary, offset);
            offset += sizeof(int);

            return tmp;
        }
        protected string ByteToString()
        {
            int len = ByteToInt();
            string tmp = System.Text.Encoding.UTF8.GetString(ary, offset, len);
            offset += len;

            return tmp;
        }
        protected float ByteToFloat()
        {
            float tmp = BitConverter.ToSingle(ary, offset);
            offset += sizeof(float);

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

        public virtual void PacketToByteArray(byte type)
        {
            ary[offset++] = type;
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