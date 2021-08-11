using System;

namespace mutant_server.Packets
{
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
        public void Copy(ChattingPakcet packet)
        {
            int len = packet.offset - packet.startPos;
            Array.Copy(packet.ary, packet.startPos, ary, offset, len);
            offset += len;
        }
        public override void PacketToByteArray(byte type)
        {
            base.PacketToByteArray(type);
            ConvertToByte(message);

            base.AddHeader();
        }
        public override void ByteArrayToPacket()
        {
            base.ByteArrayToPacket();
            this.message = ByteToString();
        }
    }
}
