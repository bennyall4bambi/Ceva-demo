using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Demo.BL_BackEnd
{
    public static class DevProtocol
    {
        public enum OpCode
        {
            getData = 0x01,
            setConnection = 0x02
        };
        public enum ConnectionOpCode
        {
            running = 0x01,
            pause = 0x02
        };
    }
}
