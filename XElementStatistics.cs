using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchemaGenerator
{
    public class XElementStatistics
    {
        public readonly string key;
        public int depth = -1; // Don't use this!

        public bool isMixed;
        public bool isComplex;
        public bool isLi;

        public HashSet<string> possibleValues = new();
        public Dictionary<string, string> possibleAttributes = new();
        public HashSet<XElementStatistics> possibleParents = new();
        public HashSet<XElementStatistics> possibleChildren = new();
        public HashSet<string> pendingParents = new();
        public HashSet<string> pendingChildren = new();

        public XElementStatistics(string key, bool isLi)
        {
            this.key = key;
            this.isLi = isLi;
        }
    }
}