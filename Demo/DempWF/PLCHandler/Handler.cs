using DemoWF.BL_BackEnd;
using DemoWF.SerialHandler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;

namespace DemoWF.PLC_Handler
{
    public delegate void PacketRecived();
    class Handler : IHandler
    {
        public event PacketRecived onPacketReceived;
        private TIHostEI tiHost;
        private Queue<TIRxPacket> commands = new Queue<TIRxPacket>();
        public Handler(SerialPort host)
        {
            this.tiHost = new TIHostEI(host);
            tiHost.onRxPktEvent += add;
        }

        public bool hasNext()
        {
            return commands.Count() != 0;
        }

        public IPLCCommand next()
        {
            if(!hasNext()) return null;
            TIRxPacket pkt = commands.Dequeue();
            byte[] data = pkt.payload;
            switch (data.ElementAt(0))
            {
                case (byte)(DevProtocol.OpCode.getData):
                    return createMeterStatusCommand(data);
                default:
                    break;
            }
            return null;
        }

        private IPLCCommand createMeterStatusCommand(byte[] data)
        {
            byte[] meterData = new byte[4];
            meterData[0] = data[4];
            meterData[1] = data[3];
            meterData[2] = data[2];
            meterData[3] = data[1];
            int meter = BitConverter.ToInt32(meterData,0);
            int connentionStatus;
            if (data[5] == (byte)(DevProtocol.ConnectionOpCode.pause))
                connentionStatus = (int)(DevProtocol.ConnectionOpCode.pause);
            else connentionStatus = (int)(DevProtocol.ConnectionOpCode.running);
            return new PLCCommand(meter, connentionStatus);
        }

        public void send(IPLCCommand cmd)
        {
            tiHost.SendPacket(cmd.toByteArray());
        }

        public void add(TIRxPacket pkt)
        {
            this.commands.Enqueue(pkt);
            onPacketReceived();
        }
    }
}
