using System;
using System.Collections.Generic;
using System.IO;

namespace mutant_server.Packets
{
    class GameInitPacket : MutantPacket
    {
        public int pCount = 0;
        //이름, 아이디, 위치, 직업 각각 n개씩
        public List<string> names;
        public List<int> IDs;
        public List<MyVector3> positions;
        public List<byte> jobs;
        //어떤 위치의 상자가 어떤 아이템을 가지고 있는지
        public Dictionary<int, List<int>> chestItems;
        public GameInitPacket(byte[] ary, int p) : base(ary, p)
        {
            names = new List<string>();
            IDs = new List<int>();
            positions = new List<MyVector3>();
            jobs = new List<byte>();

            chestItems = new Dictionary<int, List<int>>();
        }

        public void Copy(GameInitPacket packet)
        {
            Array.Copy(packet.ary, packet.offset, ary, offset, Defines.BUF_SIZE);
        }

        public override void ByteArrayToPacket()
        {
            base.ByteArrayToPacket();
            pCount = ByteToInt();
            for (int i = 0; i < pCount; ++i)
            {
                names.Add(ByteToString());
                IDs.Add(ByteToInt());
                positions.Add(ByteToVector());
                jobs.Add(ByteToByte());
            }

            for (int i = 0; i < 20; ++i)
            {
                int idx = ByteToInt();
                chestItems.Add(idx, new List<int>());
                int cnt = ByteToInt();
                for (int j = 0; j < cnt; ++j)
                {
                    chestItems[idx].Add(ByteToInt());
                }
            }
        }

        public override void PacketToByteArray(byte type)
        {
            base.PacketToByteArray(type);
            ConvertToByte(pCount);
            for (int i = 0; i < pCount; ++i)
            {
                ConvertToByte(names[i]);
                ConvertToByte(IDs[i]);
                ConvertToByte(positions[i]);
                ConvertToByte(jobs[i]);
            }

            foreach (var list in chestItems)
            {
                ConvertToByte(list.Key);
                ConvertToByte(list.Value.Count);
                for (int i = 0; i < list.Value.Count; ++i)
                {
                    ConvertToByte(list.Value[i]);
                }
            }

            base.AddHeader();
        }
    }
}
