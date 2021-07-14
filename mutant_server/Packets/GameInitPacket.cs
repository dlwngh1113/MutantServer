using System;

namespace mutant_server.Packets
{
    class GameInitPacket : MutantPacket
    {
        //이름, 아이디, 위치, 직업 각각 5개씩
        public string[] names;
        public int[] IDs;
        public MyVector3[] positions;
        public byte[] jobs;
        //어떤 위치의 상자가 어떤 아이템을 가지고 있는지
        public GameInitPacket(byte[] ary, int p) : base(ary, p)
        {
            names = new string[Defines.MAX_ROOM_USER];
            IDs = new int[Defines.MAX_ROOM_USER];
            positions = new MyVector3[Defines.MAX_ROOM_USER];
            jobs = new byte[Defines.MAX_ROOM_USER];
        }
        public void Copy(GameInitPacket packet)
        {
            Array.Copy(packet.ary, packet.offset, ary, offset, Defines.BUF_SIZE);
        }

        public override void ByteArrayToPacket()
        {
            base.ByteArrayToPacket();
            for(int i=0;i<Defines.MAX_ROOM_USER;++i)
            {
                names[i] = ByteToString();
                IDs[i] = ByteToInt();
                positions[i] = ByteToVector();
                jobs[i] = ByteToByte();
            }
        }

        public override void PacketToByteArray(byte type)
        {
            base.PacketToByteArray(type);
            for(int i = 0;i<Defines.MAX_ROOM_USER;++i)
            {
                ConvertToByte(names[i]);
                ConvertToByte(IDs[i]);
                ConvertToByte(positions[i]);
                ConvertToByte(jobs[i]);
            }
        }
    }
}
