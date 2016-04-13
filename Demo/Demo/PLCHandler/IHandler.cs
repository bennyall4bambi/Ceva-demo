using Demo.BL_BackEnd;
using Demo.SerialHandler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Demo.PLC_Handler
{
    interface IHandler
    {
        event PacketRecived onPacketReceived;
        Boolean hasNext();
        IPLCCommand next();
        void send(IPLCCommand cmd);
        void add(TIRxPacket pkt);
        bool waitingForResponse();
        void waitForResponse();
        void stopWaitForResponse();
    }
}
