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
        public void Copy(DBPacket packet)
        {
            int len = packet.offset - packet.startPos;
            Array.Copy(packet.ary, packet.startPos, ary, offset, len);
            offset += len;
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
