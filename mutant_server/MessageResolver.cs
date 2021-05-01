using System;

namespace mutant_server
{
    class MessageResolver
    {
        int offset;
        int leftBytes;
        int targetPos;
        int msgSize;
        byte op;
        byte[] packetAry;
        public MessageResolver()
        {
            offset = 0;
            leftBytes = 0;
            targetPos = 0;
            msgSize = 0;
            op = 0;
            packetAry = new byte[Defines.BUF_SIZE];
        }
        public MutantPacket ResolveMessage(byte[] ary, int offset, int bytesTransferred)
        {
            this.leftBytes = bytesTransferred;

            int srcOffset = offset;

            while(this.leftBytes > 0)
            {
                bool finished = false;
                if (this.offset < Header.size)
                {
                    this.targetPos = Header.size;

                    finished = ReadDataBuffer(ary, ref srcOffset, offset, bytesTransferred);

                    if(!finished)
                    {
                        return null;
                    }

                    this.msgSize = GetHeaderAttributes();

                    this.targetPos = Header.size + this.msgSize;
                }

                finished = ReadDataBuffer(ary, ref srcOffset, offset, bytesTransferred);
                if (finished)
                {
                    MutantPacket packet = new MutantPacket(this.packetAry, 0);
                    packet.ByteArrayToPacket();

                    CleanVariables();
                    return packet;
                }
            }

            switch (packet.header.op)
            {
                case Defines.CTOS_LOGIN:
                    ProcessLogin(e);
                    break;
                case Defines.CTOS_STATUS_CHANGE:
                    ProcessStatus(e);
                    break;
                case Defines.CTOS_ATTACK:
                    ProcessAttack(e);
                    break;
                case Defines.CTOS_CHAT:
                    ProcessChatting(e);
                    break;
                case Defines.CTOS_LOGOUT:
                    ProcessLogout(e);
                    break;
                case Defines.CTOS_ITEM_CLICKED:
                    ProcessItemEvent(e);
                    break;
                default:
                    throw new Exception("operation from client is not valid\n");
            }
        }
        private void CleanVariables()
        {
            Array.Clear(this.packetAry, 0, this.offset);
            offset = 0;
        }
        private ushort GetHeaderAttributes()
        {
            this.op = this.packetAry[2];
            return BitConverter.ToUInt16(this.packetAry, 0);
        }
        private bool ReadDataBuffer(byte[] ary, ref int srcOffset, int offset, int bytesTransferred)
        {
            if(this.offset >= offset + bytesTransferred)
            {
                return false;
            }

            int recvSize = this.targetPos - this.offset;

            if(this.leftBytes < recvSize)
            {
                recvSize = this.leftBytes;
            }

            Array.Copy(ary, srcOffset, this.packetAry, this.offset, recvSize);

            srcOffset += recvSize;
            this.offset += recvSize;
            leftBytes -= recvSize;

            if(this.offset < this.targetPos)
            {
                return false;
            }

            return true;
        }
    }
}
