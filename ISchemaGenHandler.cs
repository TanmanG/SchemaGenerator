using SchemaGenerator;
using System.Text;
using System.Xml.Linq;

public interface ISchemaGenHandler
{
    /// <summary>
    /// Extracts and stores all XML elements by key among all provided documents, into a dictionary by element key, with Li keys being set to their parent key + "Li".
    /// </summary>
    /// <param name="fileDirectories">Directories of all files to be read.</param>
    /// <returns>All elements found keyed by their XML key, with "li" keys being set to their parent key + "Li" </returns>
    public static abstract Dictionary<string, List<XElement>> GetXMLNodes(List<string> fileDirectories);
    
    /// <summary>
    /// Extrapolates and concatenates all information except child/parent references (which are instead tracked in the tuple.Second).
    /// </summary>
    /// <param name="key">Key to assign to the collected elements.</param>
    /// <param name="xElements">Elements to flatten together.</param>
    /// <returns></returns>
    public static abstract XElementStatistics FlattenXMLObject(string key, List<XElement> xElements);
    
    /// <summary>
    /// Extrapolates all options for the given XML elements, then combines them into a dictionary keyed by the element's XML key, with Li keys being set to their parent key + "Li".
    /// </summary>
    /// <param name="xElements">All elements to extrapolate from.</param>
    /// <returns>All like-keyed elements flattened into singlular representations of potential combinations, with "li" keys being set to their parent key + "Li".</returns>
    public static abstract Dictionary<string, XElementStatistics> FlattenXmlObjects(Dictionary<string, List<XElement>> xElements);

    /// <summary>
    /// Generates an li-caught key compatible with CheckKeyIsLi.
    /// </summary>
    /// <param name="element">The element to pull the parent of.</param>
    /// <param name="listElementMarker">The marker to add to the key.</param>
    /// <returns>The name of the provided element's parent with the given marker concatenated.</returns>
    public static abstract string GetReplacementLiKey(XElement element);

    /// <summary>
    /// Checks if the provided key is li-caught.
    /// </summary>
    /// <param name="key">The key to check.</param>
    /// <param name="listElementMarker">The marker to check the presence of.</param>
    /// <returns>True if the key is li-caught, False if it is normal.</returns>
    public static abstract bool CheckKeyIsLi(string key);

    /// <summary>
    /// Builds an XSD tag with the given key and attributes.
    /// </summary>
    /// <param name="key">The key of the tag.</param>
    /// <param name="attributes">The collection of KVP attribute pairs</param>
    /// <param name="closed">Whether the tag is self-closing.</param>
    /// <returns>The built tag in returned.First, the closing tag in returned.Second (null if self-closing = true)</returns>
    public static abstract Tuple<StringBuilder, StringBuilder> BuildElement(string key, Dictionary<string, string>? attributes, bool selfClosed, string closingSymbols);

    /// <summary>
    /// Builds and returns a complexType block from a given elementStatistics.
    /// </summary>
    /// <param name="elementStatistics">The information to build a complex type with.</param>
    /// <returns>The assembled complexType as a string.</returns>
    public static abstract StringBuilder BuildComplexType(XElementStatistics elementStatistics);

    /// <summary>
    /// Builds and returns an XSD file from the given complexType.
    /// </summary>
    /// <param name="complexType">The complexType to insert into the XSD file.</param>
    /// <returns>A stringBuilder containing the assembled file.</returns>
    public static abstract StringBuilder BuildXSD(StringBuilder complexType);
    
    /// <summary>
    /// Builds and returns an XSD file from the given complexTypes.
    /// </summary>
    /// <param name="complexTypes">The complexTypes to insert into the XSD file.</param>
    /// <returns>A stringBuilder containing the assembled file.</returns>
    public static abstract StringBuilder BuildXSD(List<StringBuilder> complexTypes);

    /// <summary>
    /// Writes the given XSD to disk.
    /// </summary>
    /// <param name="name">The name to write the schema to, excluding the extension (i.e. "RWSchema").</param>
    /// <param name="targetPath">The directory to write the schema in.</param>
    /// <param name="xsdFile">The built XSD file to write.</param>
    /// <returns>Whether the write was successful or not.</returns>
    public static abstract bool WriteXSDToDisk(string name, string targetPath, StringBuilder xsdFile);

}