namespace mutant_server.Packets
{
    class UserInfoPacket: MutantPacket
    {
        public int winCountTrator = 0;
        public int winCountResearcher = 0;
        public int winCountNocturn = 0;
        public int winCountPsychy = 0;
        public int winCountTanker = 0;
        public UserInfoPacket(byte[] ary, int p):base(ary, p)
        {

        }

        public override void ByteArrayToPacket()
        {
            base.ByteArrayToPacket();
            winCountTrator = ByteToInt();
            winCountResearcher = ByteToInt();
            winCountNocturn = ByteToInt();
            winCountPsychy = ByteToInt();
            winCountTanker = ByteToInt();
        }

        public override void PacketToByteArray(byte type)
        {
            base.PacketToByteArray(type);
            ConvertToByte(winCountTrator);
            ConvertToByte(winCountResearcher);
            ConvertToByte(winCountNocturn);
            ConvertToByte(winCountPsychy);
            ConvertToByte(winCountTanker);
        }
    }
}
