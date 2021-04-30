using System;

namespace mutant_server
{
    class MessageResolver
    {
        int offset;
        int leftBytes;
        int toRecvBytes;
        byte[] packetAry;
        public MessageResolver()
        {
            offset = 0;
            leftBytes = 0;
            toRecvBytes = 0;
            packetAry = new byte[Defines.BUF_SIZE];
        }
        public void ResolveMessage(byte[] ary, int offset, int bytesTransferred)
        {
            toRecvBytes = bytesTransferred;
            leftBytes = bytesTransferred; 

            if(this.offset < Header.size)
            {
                ReadDataBuffer(ary, offset, Header.size);
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
        private void ReadDataBuffer(byte[] ary, int offset, int targetBytes)
        {
            while (true)
            {
                if (this.offset < targetBytes)
                {
                    Array.Copy(ary, offset, this.packetAry, this.offset, targetBytes);
                    this.leftBytes -= targetBytes;
                }
            }
        }
    }
}
