using System;

namespace mutant_server
{
    class MessageResolver
    {
        int offset;
        int leftBytes;
        int targetPos;
        byte[] packetAry;
        public MessageResolver()
        {
            offset = 0;
            leftBytes = 0;
            targetPos = 0;
        }
        public byte[] ResolveMessage(byte[] ary, int aryOffset, int bytesTransferred)
        {
            CleanVariables();
            this.leftBytes = bytesTransferred;

            int srcOffset = aryOffset;
            targetPos = bytesTransferred;

            while (this.leftBytes > 0)
            {
                bool finished = false;

                finished = ReadDataBuffer(ary, ref srcOffset, aryOffset, bytesTransferred);
                if (finished)
                {
                    return this.packetAry;
                }
            }
            return null;
        }
        private void CleanVariables()
        {
            packetAry = new byte[Defines.BUF_SIZE];
            offset = 0;
        }
        private ushort GetHeaderAttributes()
        {
            return BitConverter.ToUInt16(this.packetAry, 0);
        }
        private bool ReadDataBuffer(byte[] ary, ref int srcOffset, int offset, int bytesTransferred)
        {
            if (this.offset >= offset + bytesTransferred)
            {
                return false;
            }

            int recvSize = targetPos - this.offset;

            if (this.leftBytes < recvSize)
            {
                recvSize = this.leftBytes;
            }

            Array.Copy(ary, srcOffset, this.packetAry, this.offset, recvSize);

            srcOffset += recvSize;
            this.offset += recvSize;
            leftBytes -= recvSize;

            if (this.offset < this.targetPos)
            {
                return false;
            }

            return true;
        }
    }
}
