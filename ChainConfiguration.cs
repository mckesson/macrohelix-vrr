using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VRR_Inbound_File_Generator
{
    public class ChainConfiguration
    {
        public int MaxPIDLength { get; set; }
        public string FileNameFormat { get; set; }
        public string DataFilePrefix { get; set; }
        public string TriggerFilePrefix { get; set; }

    }

}
