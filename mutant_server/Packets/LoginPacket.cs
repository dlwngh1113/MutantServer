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
        public void Copy(LoginPacket packet)
        {
            int len = packet.offset - packet.startPos;
            Array.Copy(packet.ary, packet.startPos, ary, offset, len);
            offset += len;
        }
        public override void PacketToByteArray(byte type)
        {
            base.PacketToByteArray(type);
            ConvertToByte(passwd);

            base.AddHeader();
        }
        public override void ByteArrayToPacket()
        {
            base.ByteArrayToPacket();
            passwd = ByteToString();
        }
    }
}
