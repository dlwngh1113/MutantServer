using System;

namespace mutant_server.Packets
{
    class DBPacket : MutantPacket
    {
        public bool isSuccess;
        public string message;
        public DBPacket(byte[] ary, int p) : base(ary, p)
        {
        }
        public void Copy(DBPacket packet, byte type = (byte)STOC_OP.STOC_CHAT)
        {
            Array.Copy(packet.ary, packet.offset, ary, offset, Defines.BUF_SIZE);
            ary[offset] = type;
        }
        public override void PacketToByteArray(byte type)
        {
            base.PacketToByteArray(type);
            ConvertToByte(isSuccess);
            ConvertToByte(message);

            base.AddHeader();
        }
        public override void ByteArrayToPacket()
        {
            base.ByteArrayToPacket();
            this.isSuccess = ByteToBool();
            this.message = ByteToString();
        }
    }
}
