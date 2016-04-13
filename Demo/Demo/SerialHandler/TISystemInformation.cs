using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Demo.SerialHandler
{
    class TISystemInformation
    {
        private byte[] sbyFirmwareVersion;
        private byte byDeviceType;
        private byte byDeviceMode;
        private byte[] sbyHardwareRevision;
        private byte byPortAssignments;
        private byte byTMRCOHFlags;
        private byte[] sbyLongAddress;

        public byte[] FirmwareVersion
        {
            get { return sbyFirmwareVersion; }
            set { sbyFirmwareVersion = value; }
        }

        public byte DeviceType
        {
            get { return byDeviceType; }
            set { byDeviceType = value; }
        }
        public byte DeviceMode
        {
            get { return byDeviceMode; }
            set { byDeviceMode = value; }
        }
        public byte[] HardwareRevision
        {
            get { return sbyHardwareRevision; }
            set { sbyHardwareRevision = value; }
        }
        public byte PortAssignments
        {
            get { return byPortAssignments; }
            set { byPortAssignments = value; }
        }
        public byte TMRCOHFlags
        {
            get { return byTMRCOHFlags; }
            set { byTMRCOHFlags = value; }
        }
        public byte[] LongAddress
        {
            get { return sbyLongAddress; }
            set { sbyLongAddress = value; }
        }
    }
}
