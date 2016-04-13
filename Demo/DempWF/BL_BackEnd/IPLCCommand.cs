using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DemoWF.BL_BackEnd
{
    interface IPLCCommand
    {
        byte[] toByteArray();
    }
}
