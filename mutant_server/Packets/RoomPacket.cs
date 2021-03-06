﻿using System;
using System.Collections.Generic;

namespace mutant_server.Packets
{
    public class RoomPacket : MutantPacket
    {
        public List<string> names;
        public List<int> numOfPlayers;
        public List<byte> gameState;
        public RoomPacket(byte[] ary, int offset) : base(ary, offset)
        {
            names = new List<string>();
            numOfPlayers = new List<int>();
            gameState = new List<byte>();
        }
        public void Copy(RoomPacket packet, byte type = (byte)STOC_OP.STOC_ROOM_CREATE_SUCCESS)
        {
            Array.Copy(packet.ary, packet.offset, ary, offset, Defines.BUF_SIZE);
            ary[offset] = type;
        }
        public override void PacketToByteArray(byte type)
        {
            base.PacketToByteArray(type);
            ConvertToByte(names.Count);
            for(int i=0;i<names.Count;++i)
            {
                ConvertToByte(names[i]);
                ConvertToByte(numOfPlayers[i]);
                ConvertToByte(gameState[i]);
            }
        }
        public override void ByteArrayToPacket()
        {
            base.ByteArrayToPacket();
            int cnt = ByteToInt();
            for(int i=0;i<cnt;++i)
            {
                names.Add(ByteToString());
                numOfPlayers.Add(ByteToInt());
                gameState.Add(ByteToByte());
            }
        }
    }
}
