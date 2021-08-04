using System;

namespace mutant_server
{
    class MessageResolver
    {
        int pos;
        int leftBytes;
        int targetPos;
        int msgSize;
        byte[] packetAry;
        public MessageResolver()
        {
            pos = 0;
            leftBytes = 0;
            targetPos = 0;
            msgSize = 0;
            packetAry = new byte[Defines.BUF_SIZE];
        }
        public byte[] ResolveMessage(byte[] ary, int aryOffset, int bytesTransferred)
        {
            leftBytes = bytesTransferred;

            int srcOffset = aryOffset;

            while (leftBytes > 0)
            {
                bool finished = false;

                if(pos < Header.size)
                {
                    targetPos = Header.size;
                    finished = ReadDataBuffer(ary, ref srcOffset, aryOffset, bytesTransferred);
                    if(!finished)
                    {
                        return null;
                    }

                    msgSize = GetHeaderAttributes();
                    targetPos = msgSize + Header.size;
                }


                finished = ReadDataBuffer(ary, ref srcOffset, aryOffset, bytesTransferred);
                if (finished)
                {
                    return this.packetAry;
                }
            }
            return null;
        }

        public void CleanVariables()
        {
            packetAry = new byte[Defines.BUF_SIZE];
            pos = 0;
            msgSize = 0;
        }

        private ushort GetHeaderAttributes()
        {
            return BitConverter.ToUInt16(packetAry, 1);
        }

        private bool ReadDataBuffer(byte[] ary, ref int srcOffset, int offset, int bytesTransferred)
        {
            if (this.pos >= offset + bytesTransferred)
            {
                return false;
            }

            int recvSize = targetPos - this.pos;

            if (this.leftBytes < recvSize)
            {
                recvSize = this.leftBytes;
            }

            Array.Copy(ary, srcOffset, this.packetAry, this.pos, recvSize);

            srcOffset += recvSize;
            this.pos += recvSize;
            leftBytes -= recvSize;

            if (this.pos < this.targetPos)
            {
                return false;
            }

            return true;
        }
    }
}
