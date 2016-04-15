using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace MessageServer
{
    public class ClientApp
    {
        public static int Count()
        {
            return Process.GetProcessesByName("messageclient").Length;
        }
    }
}
