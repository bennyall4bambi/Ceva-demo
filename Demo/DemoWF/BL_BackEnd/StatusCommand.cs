using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Demo.BL_BackEnd
{
    class StatusCommand :IPLCCommand
    {
        private byte connection;
        public StatusCommand(byte connection)
        {
            this.connection = connection;
        }
        public byte[] toByteArray()
        {
            byte connectionOpCode = (byte)DevProtocol.OpCode.setConnection;
            byte[] arr = {connectionOpCode , connection };
            return arr;
        }
    }
}
