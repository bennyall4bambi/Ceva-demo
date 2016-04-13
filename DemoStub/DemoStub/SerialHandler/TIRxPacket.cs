using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DemoStub.SerialHandler
{
    public class TIRxPacket
    {
        byte byLQI;
        byte[] sbyData;
        DateTime _rxTime;

        public byte lqi
        {
            get { return byLQI; }
            set { byLQI = value; }
        }

        public byte[] payload
        {
            get { return sbyData; }
            set { sbyData = value; }
        }

        public DateTime rxTime
        {
            get { return _rxTime; }
            set { _rxTime = value; }
        }
    }
}
