namespace mutant_server.Packets
{
    class UserInfoPacket: MutantPacket
    {
        public int winCountTrator = 0;
        public int winCountResearcher = 0;
        public int winCountNocturn = 0;
        public int winCountPsychy = 0;
        public int winCountTanker = 0;

        public int playCountTrator = 0;
        public int playCountResearcher = 0;
        public int playCountNocturn = 0;
        public int playCountPsychy = 0;
        public int playCountTanker = 0;
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

            playCountTrator = ByteToInt();
            playCountResearcher = ByteToInt();
            playCountNocturn = ByteToInt();
            playCountPsychy = ByteToInt();
            playCountTanker = ByteToInt();
        }

        public override void PacketToByteArray(byte type)
        {
            base.PacketToByteArray(type);
            ConvertToByte(winCountTrator);
            ConvertToByte(winCountResearcher);
            ConvertToByte(winCountNocturn);
            ConvertToByte(winCountPsychy);
            ConvertToByte(winCountTanker);

            ConvertToByte(playCountTrator);
            ConvertToByte(playCountResearcher);
            ConvertToByte(playCountNocturn);
            ConvertToByte(playCountPsychy);
            ConvertToByte(playCountTanker);
        }
    }
}
