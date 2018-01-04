using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SUNO.model
{
    public class WIACamera
    {
        public String DeviceID;
        public String Name;
        
        override
        public String ToString() {
            return Name;
        }

    }
}
