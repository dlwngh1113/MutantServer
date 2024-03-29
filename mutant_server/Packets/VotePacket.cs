﻿using System;
using System.Collections.Generic;

namespace mutant_server.Packets
{
    public class VotePacket : MutantPacket
    {
        public string votedPersonID;
        public Dictionary<string, int> votePairs;
        public VotePacket(byte[] ary, int offset) : base(ary, offset)
        {

        }
        public void Copy(VotePacket packet)
        {
            int len = packet.offset - packet.startPos;
            Array.Copy(packet.ary, packet.startPos, ary, offset, len);
            offset += len;
        }
        public override void PacketToByteArray(byte type)
        {
            base.PacketToByteArray(type);
            ConvertToByte(votedPersonID);
            ConvertToByte(votePairs.Count);
            foreach (var tuple in votePairs)
            {
                ConvertToByte(tuple.Key);
                ConvertToByte(tuple.Value);
            }

            base.AddHeader();
        }
        public override void ByteArrayToPacket()
        {
            base.ByteArrayToPacket();
            votedPersonID = ByteToString();
            int cnt = ByteToInt();
            votePairs = new Dictionary<string, int>();
            for (int i = 0; i < cnt; ++i)
            {
                var tKey = ByteToString();
                var tVal = ByteToInt();
                votePairs.Add(tKey, tVal);
            }
        }
    }
}
