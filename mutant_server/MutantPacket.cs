﻿using System;
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
        public float xPosition = 0, yPosition = 0, zPosition = 0;
        public float xRotation = 0, yRotation = 0, zRotation = 0;
        public float xVelocity = 0, yVelocity = 0, zVelocity = 0;
        public float roll = 0, pitch = 0, yaw = 0;
        public PlayerStatusPacket(byte[] ary, int offset):base(ary, offset)
        {
        }
        public void Copy(PlayerStatusPacket packet)
        {
            this.name = packet.name;
            this.id = packet.id;
            this.time = packet.time;

            this.xPosition = packet.xPosition;
            this.yPosition = packet.yPosition;
            this.zPosition = packet.zPosition;

            this.xRotation = packet.xRotation;
            this.yRotation = packet.yRotation;
            this.zRotation = packet.zRotation;

            this.xVelocity = packet.xVelocity;
            this.yVelocity = packet.yVelocity;
            this.zVelocity = packet.zVelocity;

            this.roll = packet.roll;
            this.pitch = packet.pitch;
            this.yaw = packet.yaw;
        }
        public override void PacketToByteArray(byte type)
        {
            base.PacketToByteArray(type);
            ConvertToByte(this.xPosition);
            ConvertToByte(this.yPosition);
            ConvertToByte(this.zPosition);
            
            ConvertToByte(this.xRotation);
            ConvertToByte(this.yRotation);
            ConvertToByte(this.zRotation);

            ConvertToByte(this.xVelocity);
            ConvertToByte(this.yVelocity);
            ConvertToByte(this.zVelocity);

            ConvertToByte(this.roll);
            ConvertToByte(this.pitch);
            ConvertToByte(this.yaw);
        }
        public override void ByteArrayToPacket()
        {
            base.ByteArrayToPacket();
            this.xPosition = ByteToFloat();
            this.yPosition = ByteToFloat();
            this.zPosition = ByteToFloat();

            this.xRotation = ByteToFloat();
            this.yRotation = ByteToFloat();
            this.zRotation = ByteToFloat();

            this.xVelocity = ByteToFloat();
            this.yVelocity = ByteToFloat();
            this.zVelocity = ByteToFloat();

            this.roll = ByteToFloat();
            this.pitch = ByteToFloat();
            this.yaw = ByteToFloat();
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