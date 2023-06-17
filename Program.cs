using Microsoft.VisualBasic;
using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Xml;
using System.Xml.Linq;


internal class Program
{
    private static void Main(string[] args)
    {
        string liMarker = "LI_MARKER";

        List<string> fileDirectories = HelperFunctions.FindFilesByExtension("xml", new()
        {
            @"C:\Program Files (x86)\Steam\steamapps\common\RimWorld\Data\Core\Defs",
            //@"C:\Program Files (x86)\Steam\steamapps\common\RimWorld\Data\Ideology\Defs",
            //@"C:\Program Files (x86)\Steam\steamapps\common\RimWorld\Data\Royalty\Defs",
            //@"C:\Program Files (x86)\Steam\steamapps\common\RimWorld\Data\Biotech\Defs",
        });

        Dictionary<string, List<XElement>> xmlObjectsFound;
        Dictionary<string, XElementStatistics> xmlObjectsFlattened;

        xmlObjectsFound = SchemaGenHandler.GetXMLNodes(fileDirectories: fileDirectories, listElementMarker: liMarker);
        xmlObjectsFlattened = SchemaGenHandler.FlattenXmlObjects(xElements: xmlObjectsFound, listElementMarker:liMarker);

        StringBuilder builtComplexType = SchemaGenHandler.BuildComplexType(xmlObjectsFlattened["Defs"]);
        Console.WriteLine(builtComplexType);

        Console.WriteLine("1");
        Console.WriteLine("2");
        Console.WriteLine("3");
        /*
        try
        {
            SchemaGenHandler.GenerateXSD(xmlObjectsFlattened: xmlObjectsFlattened, entryPoint: "Defs", breakoffThreshhold: 4);
            //File.WriteAllText("RimworldMasterSchema.xsd", GenerateXSD(xmlObjectsFlattened: xmlObjectsFlattened, 
            //                                                          entryPoint: "Defs", 
            //                                                          breakoffThreshhold: 4).ToString());
            Console.WriteLine("Master Schema written to disk!");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }*/
    }

    
    
}
