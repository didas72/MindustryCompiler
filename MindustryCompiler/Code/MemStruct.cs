using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MindustryCompiler.Code
{
    public readonly struct MemStruct
    {
        public readonly string name;
        public readonly int start;
        public readonly int size;

        public MemStruct(string name, int start, int size)
        {
            this.name = name; this.start = start; this.size = size;
        }

        public bool IsInRange(int index) => index >= start && index < start + size;
    }
}
