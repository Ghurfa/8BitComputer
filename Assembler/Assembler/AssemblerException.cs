using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assembler
{
    internal class AssemblerException : InvalidOperationException
    {
        public AssemblerException() { }

        public AssemblerException(string message) : base(message)
        {
        }
    }
}
