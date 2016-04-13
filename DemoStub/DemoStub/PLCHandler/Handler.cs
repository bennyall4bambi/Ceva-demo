using DemoStub.BL_BackEnd;
using DemoStub.SerialHandler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;
using System.Threading;

namespace DemoStub.PLC_Handler
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
            new Thread(delegate()
            {
                tiHost.ConfigureModem();
            }).Start();

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
                    return new RefreshCommand();
                case (byte)(DevProtocol.OpCode.setConnection):
                    return new StatusCommand(data.ElementAt(1));
                default:
                    break;
            }
            return null;
        }


        public void send(IPLCCommand cmd)
        {
            new Thread(delegate()
            {
                bool sent = tiHost.SendPacket(cmd.toByteArray());
                Console.WriteLine(sent);
            }).Start();

        }

        public void add(TIRxPacket pkt)
        {
            this.commands.Enqueue(pkt);
            new Thread(delegate()
            {
                onPacketReceived();
            }).Start();

        }
    }
}
