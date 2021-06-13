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
        public void Copy(ChattingPakcet packet, byte type = (byte)STOC_OP.STOC_CHAT)
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
