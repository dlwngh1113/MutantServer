﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mutant_server
{
    class Header
    {
        ushort bytes;
        byte op;
        Header() { }
        Header(ushort bytes, byte op)
        {
            this.bytes = bytes;
            this.op = op;
        }
    }
}
