using Microsoft.VisualBasic;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Xml;
using System.Xml.Linq;


internal class Program
{
    private static void Main(string[] args)
    {
        List<string> fileDirectories = HelperFunctions.FindFilesByExtension("xml", new()
        {
            @"C:\Program Files (x86)\Steam\steamapps\common\RimWorld\Data\Core\Defs",
            //@"C:\Program Files (x86)\Steam\steamapps\common\RimWorld\Data\Ideology\Defs",
            //@"C:\Program Files (x86)\Steam\steamapps\common\RimWorld\Data\Royalty\Defs",
            //@"C:\Program Files (x86)\Steam\steamapps\common\RimWorld\Data\Biotech\Defs",
        });
        Dictionary<string, List<XElement>> xmlObjectsFound = SchemaGenHandler.GetXMLNodes(fileDirectories);

        /*
        Dictionary<string, XElementStatistics> xmlObjectsFlattened = SchemaGenHandler.FlattenXmlObjects(xmlObjectsFound);

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
