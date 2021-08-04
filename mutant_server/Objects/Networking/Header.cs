using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mutant_server
{
    public struct Header
    {
        public byte op;
        public ushort bytes;

        public Header(byte op, ushort bytes)
        {
            this.op = op;
            this.bytes = bytes;
        }
        public static int size
        {
            get => sizeof(byte) + sizeof(ushort);
        }
    }
}
