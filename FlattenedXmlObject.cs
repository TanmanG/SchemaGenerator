using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class XElementStatistics
{
    public readonly string key;
    public int depth = -1;

    public bool isComplex;
    public bool isLi;

    public HashSet<string> possibleValues = new();
    public HashSet<string> possibleAttributes = new();
    public HashSet<string> possibleParents = new();
    public HashSet<string> possibleChildren = new();

    public XElementStatistics(string key, bool isLi)
    {
        this.key = key;
        this.isLi = isLi;
    }
}