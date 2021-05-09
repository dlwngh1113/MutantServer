using System;

namespace mutant_server
{
    class MessageResolver
    {
        int offset;
        int leftBytes;
        int targetPos;
        int msgSize;
        public byte op;
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
        public byte[] ResolveMessage(byte[] ary, int offset, int bytesTransferred)
        {
            CleanVariables();
            this.leftBytes = bytesTransferred;

            int srcOffset = offset;

            while(this.leftBytes > 0)
            {
                bool finished = false;
                //if (this.offset < Header.size)
                //{
                //    this.targetPos = Header.size;

                //    finished = ReadDataBuffer(ary, ref srcOffset, offset, bytesTransferred);

                //    if(!finished)
                //    {
                //        return null;
                //    }

                //    this.msgSize = GetHeaderAttributes();

                //    this.targetPos = Header.size + this.msgSize;
                //}

                finished = ReadDataBuffer(ary, ref srcOffset, offset, bytesTransferred);
                if (finished)
                {
                    return this.packetAry;
                }
            }
            return null;
        }
        private void CleanVariables()
        {
            Array.Clear(packetAry, 0, offset);
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
