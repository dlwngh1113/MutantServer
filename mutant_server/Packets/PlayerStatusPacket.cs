using System;

namespace mutant_server.Packets
{
    public class PlayerStatusPacket : MutantPacket
    {
        public MyVector3 position;
        public MyVector3 rotation;
        public byte playerMotion;
        public byte playerJob;
        public ushort size
        {
            get => (ushort)(ary.Length - Header.size);
        }

        public PlayerStatusPacket(byte[] ary, int offset) : base(ary, offset)
        {
        }
        public void Copy(PlayerStatusPacket packet, byte type = (byte)STOC_OP.STOC_STATUS_CHANGE)
        {
            Array.Copy(packet.ary, packet.offset, ary, offset, Defines.BUF_SIZE);
            ary[offset] = type;
        }
        public override void PacketToByteArray(byte type)
        {
            base.PacketToByteArray(type);

            ConvertToByte(this.position);
            ConvertToByte(this.rotation);
            ConvertToByte(this.playerMotion);
            ConvertToByte(this.playerJob);

            base.AddHeader();
        }
        public override void ByteArrayToPacket()
        {
            base.ByteArrayToPacket();

            this.position = ByteToVector();
            this.rotation = ByteToVector();
            this.playerMotion = ByteToByte();
            this.playerJob = ByteToByte();
        }
    }
}
