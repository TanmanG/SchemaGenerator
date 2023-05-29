using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchemaGenerator
{
    internal class XElementStatistics
    {
        public readonly string key;
        public int depth = -1;
        public readonly HashSet<string> possibleValues = new();
        public readonly HashSet<string> possibleAttributes = new();
        public readonly HashSet<string> possibleParents = new();
        public readonly HashSet<string> possibleChildren = new();

        public XElementStatistics(string key)
        {
            this.key = key; 
        }
    }
}
