using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Demo.BL_BackEnd
{
    class PLCCommand : IPLCCommand
    {
        private int meter;
        //1 - works , 2 - doesnt works
        private int status;
        public PLCCommand(int meter,int status){
            this.status = status;
            this.meter = meter;
        }
        public int getMeter() { return meter; }
        public int getStatus() { return status; }

        public byte[] toByteArray()
        {
            throw new NotImplementedException();
            //byte[] arr = {(byte)DevProtocol.OpCode.}
        }
    }
}
