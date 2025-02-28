using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VRR_Inbound_File_Generator
{
    public class ValidationResult
    {
        private readonly List<string> _errors = new List<string>();

        public bool IsValid => _errors.Count == 0;
        public void AddError(string message)
        {
            _errors.Add(message);
        }

        public string GetErrorMessage()
        {
            return string.Join(Environment.NewLine, _errors);
        }
    }
}
