using System.Text;
using System.Xml.Linq;

namespace SchemaGenerator
{
    public interface ISchemaGenHandler
    {
        /// <summary>
        /// Generates and writes a schema file to the given filename, at the given path, scanning the given directories and subdirectories for XMLs.
        /// </summary>
        /// <param name="filename">The name of the output file.</param>
        /// <param name="path">The location to place the output file.</param>
        /// <param name="directories">The directories to scan for XMLs within.</param>
        /// <returns>True when the schema was able to generate successfully.</returns>
        public static abstract bool GenerateSchema(string filename, string path, List<string> directories);

        /// <summary>
        /// Configures this SchemaGenHandler, out's the filename, output path, and inputs all interpreted from the provided args.
        /// </summary>
        /// <param name="args">The args/parameters passed into the command-line call.</param>
        /// <param name="filename">The filename found.</param>
        /// <param name="path">The output path found.</param>
        /// <param name="inputs">The valid input paths found.</param>
        /// <returns>True when the necessary inputs were all found to be valid.</returns>
        public static abstract bool TakeInputs(string[] args, out string filename, out string path, out List<string> inputs);

        /// <summary>
        /// Extracts and stores all XML elements by key among all provided documents, into a dictionary by element key, with Li keys being set to their parent key + LIST_MARKER.
        /// </summary>
        /// <param name="fileDirectories">Directories of all files to be read.</param>
        /// <returns>All elements found keyed by their XML key, with "li" keys being set to their parent key + LIST_MARKER.</returns>
        public static abstract Dictionary<string, List<XElement>> GetXMLNodes(List<string> fileDirectories);

        /// <summary>
        /// Extrapolates and combines all possible information for elements of this key together.
        /// </summary>
        /// <param name="key">The key to assign to the collected elements.</param>
        /// <param name="xElements">The elements to flatten together.</param>
        /// <returns>The flattened XML element, only requiring its connections to be instantiated with references from pendingChildren/Parents.</returns>
        public static abstract XElementStatistics FlattenXMLObject(string key, List<XElement> xElements);

        /// <summary>
        /// Extrapolates all options for the given XML elements, then combines them into a dictionary keyed by the element's key, with Li keys being set to their parent key + LIST_MARKER.
        /// </summary>
        /// <param name="xElements">All elements to extrapolate from.</param>
        /// <returns>All like-keyed elements flattened into singlular representations of potential combinations, with "li" keys being set to their parent key + LIST_MARKER.</returns>
        public static abstract Dictionary<string, XElementStatistics> FlattenXmlObjects(Dictionary<string, List<XElement>> xElements);

        /// <summary>
        /// Generates an li-caught key compatible with CheckKeyIsLi, using LIST_MARKER.
        /// </summary>
        /// <param name="element">The element to pull the parent of.</param>
        /// <returns>The name of the provided element's parent with the given marker concatenated.</returns>
        public static abstract string GetReplacementLiKey(XElement element);

        /// <summary>
        /// Checks if the provided key is li-caught, checking against LIST_MARKER.
        /// </summary>
        /// <param name="key">The key to check.</param>
        /// <returns>True if the key is li-caught, False if it is normal.</returns>
        public static abstract bool CheckKeyIsLi(string key);

        /// <summary>
        /// Builds an XSD tag with the given key and attributes.
        /// </summary>
        /// <param name="key">The key of the tag.</param>
        /// <param name="attributes">The collection of KVP attribute pairs</param>
        /// <param name="selfClosing">If the tag is self-closing.</param>
        /// <param name="closingSymbols">What to use for the self-closing symbol.</param>
        /// <returns>The built tag in returned.First, the closing tag in returned.Second (null if self-closing = true)</returns>
        public static abstract Tuple<StringBuilder, StringBuilder> BuildElement(string key, Dictionary<string, string>? attributes, bool selfClosing, string closingSymbols);

        /// <summary>
        /// Assembles the provided XElementStatistics into a list of StringBuilders, each an XSD-ready complexType or simpleType.
        /// </summary>
        /// <param name="elementStatistics">The statistics to build into types.</param>
        /// <returns>A list of each complexType or simpleType strings ready to be inserted into an XSD file.</returns>
        public static abstract List<StringBuilder> BuildTypes(List<XElementStatistics> elementStatistics);

        /// <summary>
        /// Builds and returns a complexType from a given elementStatistics.
        /// </summary>
        /// <param name="elementStatistics">The information to build a complex type with.</param>
        /// <returns>The assembled complexType.</returns>
        public static abstract StringBuilder BuildComplexType(XElementStatistics elementStatistics);

        /// <summary>
        /// Builds and returns a simpleType string from a given elementStatistics.
        /// </summary>
        /// <param name="elementStatistics">The information to build a simple type with.</param>
        /// <returns>The assembled simpleType.</returns>
        public static abstract StringBuilder BuildSimpleType(XElementStatistics elementStatistics);

        /// <summary>
        /// Builds and returns an XSD file from the given complexType.
        /// </summary>
        /// <param name="complexType">The complexType to insert into the XSD file.</param>
        /// <returns>A stringBuilder containing the assembled file.</returns>
        public static abstract StringBuilder BuildXSD(StringBuilder complexType);

        /// <summary>
        /// Builds and returns an XSD file from the given types.
        /// </summary>
        /// <param name="types">The simple and complex Type strings to insert into the XSD file.</param>
        /// <returns>A stringBuilder containing the assembled XSD.</returns>
        public static abstract StringBuilder BuildXSD(List<StringBuilder> types);

        /// <summary>
        /// Writes the given XSD to disk.
        /// </summary>
        /// <param name="name">The name to write the schema to, including the extension (i.e. name: "my_file_name.xsd").</param>
        /// <param name="targetPath">The directory to write the schema in.</param>
        /// <param name="xsdFile">The built XSD file to write.</param>
        /// <returns>Whether the write was successful or not.</returns>
        public static abstract bool WriteXSDToDisk(string name, string targetPath, StringBuilder xsdFile);

    }
}