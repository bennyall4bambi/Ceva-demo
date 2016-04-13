using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Demo.SerialHandler
{
    public delegate void RxPktHandler(TIRxPacket pkt);

    class TIHostEI
    {
        public event RxPktHandler onRxPktEvent;

        SerialPort sPort;
        enum Opcode { data = 0 , systemInfo = 1 , setInfo = 4 , loadSystemConfig = 0x0c , setMACPIB = 0x0d , shutdown = 0x05};

        enum Header { 
            iEIPacketHeaderLength = 8,
            iEIPacketPayloadOffset = 4,
            iMessageTypeIndex = 0,
            iOptionsIndex = 1,
            iLengthLIndex = 2,
            iLengthHIndex = 3,
            iPayloadIndex = 8,
            iTimeOut = 5000
        };

        enum SystemInfo { 
            firmwareVersionOffset = 0,
            firmwareVersionLength = 4, 
            deviceTypeOffset = 22, 
            deviceModeOFfset = 23,
            hardwareRevisionOffset = 23,
            hardwareRevisionLength = 2,
            portAssignmentsOffset = 32,
            tmrCOHFlagsOffset = 40,
            longAddressOffset = 44,
            longAddressLength = 8
        };

        enum SystemConfig { 
            iLoadSystemConfigTypeOffset = 0,
            loadLengthOffset = 2,
            loadPayloadOffset = 4,
            wTypePortDesignation = 1,//ushort
            wTypeSystemConfiguration = 3,//ushort
            wTypeG3Configuration = 8  //ushort
        };

        enum SetMACPIBAttribute {
            iIDOffset = 0,
            iIDLength = 4,
            iIndexOffset = 4,
            iIndexLength = 2,
            iLengthOffset = 6,
            iLengthLength = 2,
            iValueOffset = 8
        };

        enum DwMACPIB : uint { 
           coherentModeIndex = 0xFFFFFFFA,
           aPBSecurityLevel = 0x91,
        };

        enum RxPacket { 
             iQIIndex = 0,
             iOptionsIndex = 1,
             iPayloadIndex = 2,
        };
        
        enum Info { 
            iSetInfoTypeOffset = 0,
            iSetInfoTypeLength = 2,
            iSetInfoLengthOffset = 2,
            iSetInfoLengthLength = 2,
            iSetInfoValueOffset = 4,
            wInfoTypeG3PHYTxParams = 2,
            wInfoTypeG3PHYRxParams = 3,
        };

        public const int iSendPktPayloadOffset = 2;

        public readonly byte[] sbyG3TxParams = {0x00, 0x02, 0x20, 0x00,
                0x17, 0x24, 0xFF, 0xFF, 0xFF, 0xFF, 0x0F, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00};
        public readonly byte[] sbyG3RxParams = {0x00, 0x05, 0x04, 0x00,
                0x17, 0x24, 0xFF, 0xFF, 0xFF, 0xFF, 0x0F, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00};
        public readonly byte[] sbySystemConfiguration = {0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x03, 0x20};
        public readonly byte[] sbyG3Config = {0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0xEF, 0x00};
        public readonly byte[] sbyPortConfig = { 0x00 };

        enum eState { OPCODE, OPTIONS, LENGTH_L, LENGTH_H, PAYLOAD, PADDING};
        eState CurrState;
        int iExpectedRXLength;
        int iReceivedRXLength;
        int iReceivedOpcode;
        int iReceivedOptionsByte;
        int iExpectedOpcode;
        byte[] sbyRxPacket;
        private AutoResetEvent hResponseReceived;

        public bool ConfigureModem()
        {
            // point-to-point mode
            if (!LoadSystemConfig((ushort)SystemConfig.wTypeSystemConfiguration, sbySystemConfiguration)) return false;
            if (!LoadSystemConfig((ushort)SystemConfig.wTypeG3Configuration, sbyG3Config)) return false;
            if (!LoadSystemConfig((ushort)SystemConfig.wTypePortDesignation, sbyPortConfig)) return false;
            if (!Shutdown()) return false;
            Thread.Sleep(1000);
            if (!SetInfo((ushort)Info.wInfoTypeG3PHYTxParams, sbyG3TxParams)) return false;
            if (!SetInfo((int)Info.wInfoTypeG3PHYRxParams, sbyG3RxParams)) return false;
            if (!SetMACPIB((uint)DwMACPIB.coherentModeIndex, 0, (ushort)0)) return false;
            if (!SetMACPIB((uint)DwMACPIB.aPBSecurityLevel, 0, (byte)0)) return false;
            return true;
        }

        public TIHostEI(SerialPort sPort)
        {
            this.sPort = sPort;
            sPort.DataReceived += SPort_DataReceived;
            CurrState = eState.OPCODE;
            iExpectedOpcode = 0xFF;
            hResponseReceived = new AutoResetEvent(false);
        }

        public bool LoadSystemConfig(int iConfigType, byte[] sbyConfig)
        {
            byte[] sbyConfigPayload = new byte[sbyConfig.Length + (int)SystemConfig.loadPayloadOffset];
            sbyConfigPayload[(int)SystemConfig.iLoadSystemConfigTypeOffset] = (byte)iConfigType;
            sbyConfigPayload[(int)SystemConfig.loadLengthOffset] = (byte)sbyConfig.Length;
            Array.Copy(sbyConfig, 0, sbyConfigPayload, (int)SystemConfig.loadPayloadOffset, sbyConfig.Length);
            return sSendCommand((int) Opcode.loadSystemConfig, sbyConfigPayload);
        }

        public bool Shutdown()
        {
            byte[] sbyCommand = new byte[] { 0, 0 };
            return sSendCommand((int)Opcode.shutdown, sbyCommand);
        }

        public bool SetInfo(ushort wInfoType, byte[] sbyInfoValue)
        {
            byte[] sbyInfo = new byte[sbyInfoValue.Length + (int)Info.iSetInfoValueOffset];
            byte[] sbyInfoType = BitConverter.GetBytes(wInfoType);
            ushort wLength = (ushort)sbyInfoValue.Length;
            byte[] sbyInfoLength = BitConverter.GetBytes(wLength);
            Array.Copy(sbyInfoType, sbyInfo, sbyInfoType.Length);
            Array.Copy(sbyInfoLength, 0, sbyInfo, (int)Info.iSetInfoLengthOffset, sbyInfoLength.Length);
            Array.Copy(sbyInfoValue, 0, sbyInfo, (int)Info.iSetInfoValueOffset, sbyInfoValue.Length);
            return sSendCommand((int)Opcode.setInfo, sbyInfo);
        }

        public bool SetMACPIB(uint dwAttributeID, ushort wAttributeIndex, byte[] sbyAttributeValue)
        {
            byte[] sbyMACPIB = new byte[(int)SetMACPIBAttribute.iValueOffset + sbyAttributeValue.Length];
            byte[] sbyAttributeID = BitConverter.GetBytes(dwAttributeID);
            byte[] sbyAttributeIndex = BitConverter.GetBytes(wAttributeIndex);
            Array.Copy(sbyAttributeID, sbyMACPIB, sbyAttributeID.Length);
            Array.Copy(sbyAttributeIndex, 0, sbyMACPIB, (int)SetMACPIBAttribute.iIndexOffset, sbyAttributeIndex.Length);
            sbyMACPIB[(int)SetMACPIBAttribute.iLengthOffset] = (byte)(sbyAttributeValue.Length & 0xFF);
            sbyMACPIB[(int)SetMACPIBAttribute.iLengthOffset + 1] = (byte)(sbyAttributeValue.Length >> 8);
            Array.Copy(sbyAttributeValue, 0, sbyMACPIB, (int)SetMACPIBAttribute.iValueOffset, sbyAttributeValue.Length);
            return sSendCommand((int)Opcode.setMACPIB, sbyMACPIB);
        }

        public bool SetMACPIB(uint dwAttributeID, ushort wAttributeIndex, ushort wValue)
        {
            byte[] sbyValue = BitConverter.GetBytes(wValue);
            return SetMACPIB(dwAttributeID, wAttributeIndex, sbyValue);
        }

        public bool SetMACPIB(uint dwAttributeID, ushort wAttributeIndex, uint dwValue)
        {
            byte[] sbyValue = BitConverter.GetBytes(dwValue);
            return SetMACPIB(dwAttributeID, wAttributeIndex, sbyValue);
        }

        public bool SetMACPIB(uint dwAttributeID, ushort wAttributeIndex, byte byValue)
        {
            byte[] sbyValue = new byte[1];
            sbyValue[0] = byValue;
            return SetMACPIB(dwAttributeID, wAttributeIndex, sbyValue);
        }

        private bool sSendCommand(byte byOpcode, byte[] sbyPayload)
        {
            int iPayloadLength;
            iPayloadLength = (sbyPayload == null)?  0 : sbyPayload.Length;

            byte[] sbyCommand = new byte[(int)Header.iEIPacketHeaderLength + iPayloadLength];
            sbyCommand[(int)Header.iMessageTypeIndex] = byOpcode;
            sbyCommand[(int)Header.iOptionsIndex] = 0x80;
            sbyCommand[(int)Header.iLengthLIndex] = (byte)(((int)Header.iEIPacketPayloadOffset + iPayloadLength) & 0xFF);
            sbyCommand[(int)Header.iLengthHIndex] = (byte)(((int)Header.iEIPacketPayloadOffset + iPayloadLength) >> 8);
            if (iPayloadLength > 0)
            {
                Array.Copy(sbyPayload, 0, sbyCommand, (int)Header.iEIPacketHeaderLength, sbyPayload.Length);
            }
            iExpectedOpcode = byOpcode;
            try
            {
                sPort.Write(sbyCommand, 0, sbyCommand.Length);
            }
            catch (Exception ex) { }
            if((sbyCommand.Length & 1) == 1)
            {
                // send padding
                byte[] sbyPadding = new byte[1];
                sPort.Write(sbyPadding, 0, 1);
            }
            bool bResponseReceived = hResponseReceived.WaitOne((int)Header.iTimeOut);
            iExpectedOpcode = 0xFF;
            return bResponseReceived;
        }

        public TISystemInformation GetSystemInformation()
        {
            TISystemInformation stSystemInformation = null;
            if (sSendCommand((int)Opcode.systemInfo, null))
            {
                stSystemInformation = new TISystemInformation();
                stSystemInformation.DeviceMode = sbyRxPacket[(int)Header.iEIPacketPayloadOffset + (int)SystemInfo.deviceModeOFfset];
                stSystemInformation.DeviceType = sbyRxPacket[(int)Header.iEIPacketPayloadOffset + (int)SystemInfo.deviceTypeOffset];
                stSystemInformation.FirmwareVersion = new byte[(int)SystemInfo.firmwareVersionLength];
                Array.Copy(sbyRxPacket, (int)Header.iEIPacketPayloadOffset + (int)SystemInfo.firmwareVersionOffset,
                    stSystemInformation.FirmwareVersion, 0, (int)SystemInfo.firmwareVersionLength);
                stSystemInformation.HardwareRevision = new byte[(int)SystemInfo.hardwareRevisionLength];
                Array.Copy(sbyRxPacket, (int)Header.iEIPacketPayloadOffset + (int)SystemInfo.hardwareRevisionOffset,
                    stSystemInformation.HardwareRevision, 0, (int)SystemInfo.hardwareRevisionLength);
                stSystemInformation.LongAddress = new byte[(int)SystemInfo.longAddressLength];
                Array.Copy(sbyRxPacket, (int)Header.iEIPacketPayloadOffset + (int)SystemInfo.longAddressOffset,
                    stSystemInformation.LongAddress, 0, (int)SystemInfo.longAddressLength);
                stSystemInformation.PortAssignments = sbyRxPacket[(int)Header.iEIPacketPayloadOffset + (int)SystemInfo.portAssignmentsOffset];
                stSystemInformation.TMRCOHFlags = sbyRxPacket[(int)Header.iEIPacketPayloadOffset + (int)SystemInfo.tmrCOHFlagsOffset];
            }

            return stSystemInformation;
        }

        public bool SendPacket(byte[] sbyPayload)
        {
            byte[] sbyPktToSend = new byte[iSendPktPayloadOffset + sbyPayload.Length];
            Array.Copy(sbyPayload, 0, sbyPktToSend, iSendPktPayloadOffset, sbyPayload.Length);
            return sSendCommand((int)Opcode.data, sbyPktToSend);
        }

        private void s_HandleRxPacket()
        {
            TIRxPacket pkt = new TIRxPacket();
            pkt.lqi = sbyRxPacket[(int)RxPacket.iQIIndex];
            pkt.rxTime = DateTime.Now;
            pkt.payload = new byte[sbyRxPacket.Length - (int)RxPacket.iPayloadIndex - (int)Header.iEIPacketPayloadOffset];
            Array.Copy(sbyRxPacket, (int)RxPacket.iPayloadIndex + (int)Header.iEIPacketPayloadOffset, pkt.payload, 0, pkt.payload.Length);
            if(onRxPktEvent != null)
            {
                onRxPktEvent(pkt);
            }
        }

        private void sProcessRXPacket()
        {
            if(iExpectedOpcode == iReceivedOpcode)
            {
                // we have received response to the command we sent - release the waiting response block
                hResponseReceived.Set();
            }
            else
            {
                if (iReceivedOpcode == (int)Opcode.data)
                {
                    // rx packet
                    s_HandleRxPacket();
                }
            }
        }

        private void SPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            while(sPort.BytesToRead > 0)
            {
                switch(CurrState)
                {
                    case eState.OPCODE:
                        iReceivedOpcode = sPort.ReadByte();
                        CurrState = eState.OPTIONS;
                        break;
                    case eState.OPTIONS:
                        iReceivedOptionsByte = sPort.ReadByte();
                        CurrState = eState.LENGTH_L;
                        break;
                    case eState.LENGTH_L:
                        iExpectedRXLength = sPort.ReadByte();
                        CurrState = eState.LENGTH_H;
                        break;
                    case eState.LENGTH_H:
                        iExpectedRXLength += sPort.ReadByte() * 256;
                        iReceivedRXLength = 0;
                        CurrState = eState.PAYLOAD;
                        sbyRxPacket = new byte[iExpectedRXLength];
                        break;
                    case eState.PAYLOAD:
                        sbyRxPacket[iReceivedRXLength++] = (byte)sPort.ReadByte();
                        if(iReceivedRXLength == iExpectedRXLength)
                        {
                            // we have received the packet - need to process
                            if ((iReceivedRXLength & 1) == 1)
                            {
                                CurrState = eState.PADDING;
                            }
                            else
                            {
                                CurrState = eState.OPCODE;
                                sProcessRXPacket();
                            }
                        }
                        break;
                    case eState.PADDING:
                        int byDummy = sPort.ReadByte();
                        CurrState = eState.OPCODE;
                        sProcessRXPacket();
                        break;
                }
            }
        }
    }
}
