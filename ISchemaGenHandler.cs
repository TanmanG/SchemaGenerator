using System.Text;
using System.Xml.Linq;

public interface ISchemaGenHandler
{
    /// <summary>
    /// Extracts and stores all XML elements by key among all provided documents, into a dictionary by element key, with Li keys being set to their parent key + "Li".
    /// </summary>
    /// <param name="fileDirectories">Directories of all files to be read.</param>
    /// <returns>All elements found keyed by their XML key, with "<Li>" keys being set to their parent key + "Li </returns>
    public static abstract Dictionary<string, List<XElement>> GetXMLNodes(List<string> fileDirectories);
    /// <summary>
    /// Extrapolates all options for the given XML elements, then combines them into a dictionary keyed by the element's XML key, with Li keys being set to their parent key + "Li".
    /// </summary>
    /// <param name="xElements">All elements to extrapolate from.</param>
    /// <returns>All like-keyed elements flattened into singlular representations of potential combinations, with "<Li>" keys being set to their parent key + "Li".</returns>
    public static abstract Dictionary<string, XElementStatistics> FlattenXmlObjects(Dictionary<string, List<XElement>> xElements);
    /// <summary>
    /// Converts the provided flattened XML elements into an XSD family, with a central 'master' XSD of name outputName.
    /// </summary>
    /// <param name="xmlObjectsFlattened">The collection of all flattened XML elements.</param>
    /// <param name="entryPoint">REFACTOR THIS!</param>
    /// <param name="breakoffThreshhold">REFACTOR THIS!</param>
    public static abstract void GenerateXSD(Dictionary<string, XElementStatistics> xmlObjectsFlattened, string entryPoint, int breakoffThreshhold, string outputName);
    
}