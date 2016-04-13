using DemoWF.BL_BackEnd;
using DemoWF.SerialHandler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DemoWF.PLC_Handler
{
    interface IHandler
    {
        event PacketRecived onPacketReceived;
        Boolean hasNext();
        IPLCCommand next();
        void send(IPLCCommand cmd);
        void add(TIRxPacket pkt);
        

    }
}
