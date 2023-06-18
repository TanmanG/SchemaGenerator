using Microsoft.VisualBasic;
using SchemaGenerator;
using System.Diagnostics;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;


internal class Program
{
    private static void Main(string[] args)
    {
        if (SchemaGenHandler.TakeInputs(args, out string filename, out string path, out List<string> inputs))
        {
            SchemaGenHandler.GenerateSchema(filename, path, inputs);
        }
    }
}
