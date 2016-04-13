using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DemoStub.BL_BackEnd
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
            byte[] arr = new byte[6];
            arr[0] = (byte)(DevProtocol.OpCode.getData);
            arr[1] = (byte)(meter >> 24);
            arr[2] = (byte)(meter >> 16);
            arr[3] = (byte)(meter >> 8);
            arr[4] = (byte)meter;
            arr[5] = (byte)status;
            return arr;
        }
    }
}
