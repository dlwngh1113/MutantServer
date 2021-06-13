using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mutant_server.Packets
{
    public class LoginPacket : MutantPacket
    {
        public string passwd;
        public ushort size
        {
            get => (ushort)(ary.Length - Header.size);
        }
        public LoginPacket(byte[] ary, int offset) : base(ary, offset)
        {

        }
        public void Copy(LoginPacket packet, byte type = (byte)STOC_OP.STOC_CHAT)
        {
            Array.Copy(packet.ary, packet.offset, ary, offset, Defines.BUF_SIZE);
            ary[offset] = type;
        }
        public override void PacketToByteArray(byte type)
        {
            base.PacketToByteArray(type);
            ConvertToByte(passwd);
        }
        public override void ByteArrayToPacket()
        {
            base.ByteArrayToPacket();
            passwd = ByteToString();
        }
    }
}
