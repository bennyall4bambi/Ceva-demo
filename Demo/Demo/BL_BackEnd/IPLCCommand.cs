using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Demo.BL_BackEnd
{
    interface IPLCCommand
    {
        byte[] toByteArray();
    }
}
