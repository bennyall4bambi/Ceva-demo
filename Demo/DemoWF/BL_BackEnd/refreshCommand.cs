using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Demo.BL_BackEnd
{
    class RefreshCommand:IPLCCommand
    {
        public byte[] toByteArray()
        {
            byte[] arr = { (byte)DevProtocol.OpCode.getData };
            return arr;
        }
    }
}
