using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VRR_Inbound_File_Generator.Models
{
    internal class NDCModel
    {
        public string NDCCode { get; set; }
        public bool IsActive { get; set; }
        public bool IsDiscontinued { get; set; }
        public DateTime? DiscontinuedDate { get; set; }
    }
}
