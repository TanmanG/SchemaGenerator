using System.Reflection;
using System.Text;
using System.Xml.Linq;

namespace SchemaGenerator
{
    public class SchemaGenHandler : ISchemaGenHandler
    {
        // CORE FUNCTIONS
        public static bool VERBOSE = true;
        public static int PROGRESS_INTERVAL = 3;
        public static string LIST_MARKER = "LI_MARKER";
        private static readonly string HELP =
@"Usage: SchemaGenerator.exe [OPTIONS] [ARGUMENTS]

Description: A tool for taking a collection of RimWorld Defs and extrapolating an XSD from them.

Options:
-h --help             Prints all information for this tool and exits.
-o --output FILE      (REQUIRED) The output XSD filename.
-i --input PATHS...   (REQUIRED) The paths to search for XML files in.
-p --path PATH        The path to output the XSD into. By default, it will be the folder the executable is.
-s --silent           Runs the schema generator silently, without outputting any progress reports.
-d --debug NUM        Sets the number of status messages to be sent during each step. The default is 3.

Arguments:
FILE   A filename. Specifies the name of the file the compiled schema will be saved into.
PATHS  A list of paths to folders, separated by spaces.
PATH   A path to a folder.
NUM    A whole number.

Examples:
SchemaGenerator.exe -o ""RimworldSchema.xsd"" -i ""C:\Program Files (x86)\Steam\steamapps\common\RimWorld\Data\Core\Defs"" --debug 5
    Description: Reads all XML files within ""...\Data\Core\Defs\"" and compiles a schema into a file named RimworldSchema.xsd, placed inside the folder of SchemaGenerator.exe. Reports progress at 5 intermediary steps instead of 3.

SchemaGenerator.exe -o ""RWMasterSchema.xsd"" -i ""C:\Program Files (x86)\Steam\steamapps\common\RimWorld\Data\Core\Defs"" ""C:\Program Files (x86)\Steam\steamapps\common\RimWorld\Data\Ideology\Defs"" --silent
    Description: Searches ""...\Data\(Core, Ideology, Royalty, and Biotech)\Defs\"" for all XML files and compiles a schema to RWMasterSchema.xsd, placed in the same folder as SchemaGenerator.exe. Runs silently outputting no status messages.";

        public static bool GenerateSchema(string filename, string path, List<string> directories)
        {
            Dictionary<string, List<XElement>> xmlObjectsFound;
            Dictionary<string, XElementStatistics> xmlObjectsFlattened;
            List<StringBuilder> builtComplexTypes;
            StringBuilder builtXSDFile;

            // Find all XML files in the given locations
            List<string> fileDirectories = HelperFunctions.FindFilesByExtension("xml", directories);

            if (!fileDirectories.Any())
            {
                Console.WriteLine("No XML files found in the provided directories!");
                return false;
            }

            // Read, Process, and Build the Schema
            xmlObjectsFound = GetXMLNodes(fileDirectories: fileDirectories);
            xmlObjectsFlattened = FlattenXmlObjects(xElements: xmlObjectsFound);
            builtComplexTypes = BuildTypes(elementStatistics: xmlObjectsFlattened.Values.ToList());
            builtXSDFile = BuildXSD(builtComplexTypes);

            // Write the final schema!
            return WriteXSDToDisk(name: filename, path: path, xsdFile: builtXSDFile);
        }

        public static bool TakeInputs(string[] args, out string filename, out string path, out List<string> inputs)
        {
            filename = string.Empty;
            path = string.Empty;
            inputs = new();
            string nextArg;

            int argsLength = args.Length;
            for (int index = 0; index < args.Length; index++)
            {
                string arg = args[index];
                switch (arg.ToLower())
                {

                    case "--help": // Display help information
                    case "-h":
                        // To-do: Add help stuff
                        Console.WriteLine(HELP);
                        return false;

                    case "--output": // Take & Verify the output filename
                    case "-o":
                        // Check if a filename was provided
                        if (index + 1 >= argsLength)
                        {
                            Console.WriteLine("Filename must be provided!");
                            return false;
                        }

                        // Store the filename
                        filename = args[index + 1];

                        // Check if the filename is valid
                        if (!HelperFunctions.CheckFilenameIsValid(filename))
                        {
                            Console.WriteLine("ERROR: Filename is invalid!");
                            return false;
                        }

                        break;

                    case "-input": // List input directories
                    case "-i":
                        // Take all input directories
                        // To-do: For loop until another found
                        // Iterate until we've hit a non-path
                        List<string> badPaths = new();
                        int currentIndex = index;
                        do
                        {
                            currentIndex++;
                            // Check if we have another arg to move to
                            if (currentIndex >= argsLength)
                                break;

                            // Cache the arg
                            nextArg = args[currentIndex];

                            // Check if the arg is invalid
                            if (!Path.Exists(nextArg))
                            {
                                badPaths.Add(nextArg);
                                break;
                            }

                            // Store the arg
                            inputs.Add(nextArg);
                        } while (true);

                        if (!inputs.Any())
                        {
                            Console.WriteLine("No input folders valid!");
                            // Write each bad path, if any
                            foreach (var badPath in badPaths)
                            {
                                Console.WriteLine("ERROR: Bad path: " + badPath);
                            }
                            return false;
                        }

                        break;

                    case "--path": // Take & Verify output directory
                    case "-p":
                        // Check if a path was provided
                        if (index + 1 >= argsLength)
                        {
                            Console.WriteLine("ERROR: Path must be provided!");
                            return false;
                        }

                        // Store the path
                        path = args[index + 1];

                        // Cancel if the path doesn't exist
                        if (!Path.Exists(path))
                        {
                            Console.WriteLine("ERROR: Provided output-path (folder) doesn't exist!");
                            return false;
                        }

                        break;

                    case "--silent": // Silence the progress intervals
                    case "-s":
                        VERBOSE = false;
                        break;


                    case "--debug": // Set how many progress steps are to be logged during each phase
                    case "-d":
                        // Check if we have another arg to move to
                        if (index + 1 >= argsLength)
                            return false;

                        // Cache the arg
                        nextArg = args[index + 1];

                        // Attempt to read the interval value
                        if (int.TryParse(nextArg, out int interval))
                        {
                            PROGRESS_INTERVAL = interval;
                        }
                        else
                        {
                            return false;
                        }
                        break;
                }
            }

            // Catch any unassigned values
            bool safeToReturn = true;
            if (filename.Equals(string.Empty))
            {
                Console.WriteLine("ERROR: No filename provided!");
                safeToReturn = false;
            }
            if (!inputs.Any())
            {
                Console.WriteLine("ERROR: No input paths provivded!");
                safeToReturn = false;
            }

            if (safeToReturn && path.Equals(string.Empty))
            {
                if (VERBOSE) Console.WriteLine("STATUS: No path provided, defaulting to this executable's directory!");
                path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            }

            return safeToReturn;
        }

        public static Dictionary<string, List<XElement>> GetXMLNodes(List<string> fileDirectories)
        {
            // DEBUG
            if (VERBOSE) Console.WriteLine("STATUS: Reading Files...");
            int iteration = 1;
            int iterationMax = fileDirectories.Count;
            int modulo = iterationMax / PROGRESS_INTERVAL;

            Dictionary<string, List<XElement>> nodesFound = new();

            // Iterate over each input file
            foreach (string fileDirectory in fileDirectories)
            {
                // Load each document
                XDocument xDocument = XDocument.Load(fileDirectory);

                // Iterate over every element found in the document
                foreach (XElement element in xDocument.Descendants())
                {
                    string elementName = element.Name.LocalName;

                    // Check if the element is an Li element
                    if (elementName.ToLower().Equals("li"))
                    { // Store it with its parent key

                        // Store parent name, catching top-level Li elements
                        string liReplacementName = GetReplacementLiKey(element);

                        // Store the newly keyed element.
                        HelperFunctions.AddLD(dictionary: nodesFound, value: element, key: liReplacementName);
                    }
                    else
                    { // Store the element normally
                        HelperFunctions.AddLD(dictionary: nodesFound, value: element, key: elementName);
                    }
                }

                // DEBUG
                if (VERBOSE && (iteration % modulo == 0 || iteration == iterationMax))
                {
                    Console.WriteLine("Read Files: " + iteration + "/" + iterationMax);
                }
                iteration++;
            }

            // DEBUG
            if (VERBOSE) Console.WriteLine("Reading done!");

            return nodesFound;
        }

        public static XElementStatistics FlattenXMLObject(string key, List<XElement> xElements)
        {
            // Track whether this is an Li element or not
            bool isLi = CheckKeyIsLi(key);

            // Begin tracking the flattened element
            XElementStatistics flattenedElement = new(key: key, isLi: isLi);

            // Extrapolate!
            foreach (XElement element in xElements)
            {
                // Pull the value
                var convertedElementValues = element.Nodes().OfType<XText>().Select(text => text.ToString());
                string elementValue = string.Join(separator: " ", convertedElementValues);
                if (elementValue.Any())
                {
                    flattenedElement.possibleValues.Add(elementValue);
                }

                // Pull the parent for later, but only when not null (should rarely happen)
                string? parentName = element.Parent?.Name.LocalName;
                if (parentName != null)
                {
                    // Catch sneaky <li> nodes
                    if (parentName.ToLower().Equals("li"))
                    {
                        // Store the parent's replaced Li's name
                        string liReplacementName = GetReplacementLiKey(element.Parent);
                        flattenedElement.pendingParents.Add(liReplacementName);
                    }
                    else
                    {
                        // Store the parent's name regularly
                        flattenedElement.pendingParents.Add(parentName);
                    }
                }

                // Pull all children for later
                var elementChildren = element.Elements();
                // Check for any children
                if (elementChildren.Any())
                {
                    foreach (XElement child in elementChildren)
                    {
                        // Catch sneaky <li> nodes
                        if (child.Name.LocalName.ToLower().Equals("li"))
                        {
                            // Store the replaced Li's name
                            string liReplacementName = GetReplacementLiKey(child);
                            flattenedElement.pendingChildren.Add(liReplacementName);
                        }
                        else
                        {
                            // Store the name regularly
                            flattenedElement.pendingChildren.Add(child.Name.LocalName);
                        }
                    }
                }

                // Pull all attribute KVPs
                var elementAttributes = element.Attributes();
                // Check for any attributes
                if (elementAttributes.Any())
                {
                    foreach (XAttribute attribute in element.Attributes())
                    {
                        flattenedElement.possibleAttributes.TryAdd(attribute.Name.LocalName, attribute.Value);
                    }
                }
            }

            // Check for complexity
            if (flattenedElement.pendingChildren.Any() || flattenedElement.possibleAttributes.Any())
            {
                flattenedElement.isComplex = true;
            }
            // Check for mixed
            if (flattenedElement.possibleValues.Any()
                && (flattenedElement.pendingChildren.Any() || flattenedElement.possibleAttributes.Any()))
            {
                flattenedElement.isMixed = true;
            }

            // Return everything
            return flattenedElement;
        }

        public static Dictionary<string, XElementStatistics> FlattenXmlObjects(Dictionary<string, List<XElement>> xElements)
        {
            // DEBUG
            if (VERBOSE) Console.WriteLine("STATUS: Flattening Elements...");
            int iteration = 1;
            int iterationMax = xElements.Count;
            int modulo = iterationMax / PROGRESS_INTERVAL;

            // Create a dictionary to store all the flattened elements
            Dictionary<string, XElementStatistics> flattenedXmlElements = new();

            // Group by each key
            foreach (var elementsOfLikeKey in xElements)
            {
                // Store references to the KVP contents
                string currentKey = elementsOfLikeKey.Key;
                List<XElement> currentElements = elementsOfLikeKey.Value;

                // Flatten the current XML object
                var flattenedElement = FlattenXMLObject(currentKey, currentElements);

                // And store it for later processing & return
                flattenedXmlElements.Add(key: flattenedElement.key, flattenedElement);

                // DEBUG
                if (VERBOSE && (iteration % modulo == 0 || iteration == iterationMax))
                {
                    Console.WriteLine("Elements Flattened: " + iteration + "/" + iterationMax);
                }
                iteration++;
            }

            // DEBUG
            if (VERBOSE) Console.WriteLine("Flattening done!");
            if (VERBOSE) Console.WriteLine("STATUS: Linking Elements...");
            iteration = 1;
            iterationMax = flattenedXmlElements.Count;
            modulo = iterationMax / PROGRESS_INTERVAL;

            // Link all elements from each object
            foreach (var flattenedElement in flattenedXmlElements)
            {
                // Cache for performance
                var currentFlattenedElement = flattenedElement.Value;
                var currentKey = flattenedElement.Key;

                // Link each child
                foreach (var pendingChild in currentFlattenedElement.pendingChildren)
                {
                    // Cache for performance
                    var addedChild = flattenedXmlElements[pendingChild];

                    // Store the child
                    currentFlattenedElement.possibleChildren.Add(addedChild);

                    // Also store the parent for optimization
                    addedChild.possibleParents.Add(currentFlattenedElement);

                    // Remove this for a last once-over check
                    addedChild.pendingParents.Remove(currentKey);
                }

                // Clear the children list for memory concerns
                currentFlattenedElement.pendingChildren = default;

                // DEBUG
                if (VERBOSE && (iteration % modulo == 0 || iteration == iterationMax))
                {
                    Console.WriteLine("Elements Linked: " + iteration + "/" + iterationMax);
                }
                iteration++;
            }
            if (VERBOSE) Console.WriteLine("Linking done!");

            // Catch any remaining unlinked parents!
            foreach (var flattenedElement in flattenedXmlElements)
            {
                var currentFlattenedElement = flattenedElement.Value;
                var currentKey = flattenedElement.Key;

                if (currentFlattenedElement.pendingParents.Any())
                {
                    Console.WriteLine("Warning: Found Ghost parent: " + currentKey);
                }
            }

            return flattenedXmlElements;
        }

        public static string GetReplacementLiKey(XElement element)
        {
            return ((element.Parent?.Name.LocalName) ?? throw new NullReferenceException("Li element at top-level!")) + LIST_MARKER;
        }

        public static bool CheckKeyIsLi(string key)
        {
            // Calculate the theorhetical start point
            int keyLength = key.Length;
            int listElementMarkerLength = LIST_MARKER.Length;

            // Optimization to catch impossible length'd keys
            if (keyLength < listElementMarkerLength)
            {
                return false;
            }

            // Store the entry point (e.g. "MARK" w/ "hiMARK" -> 6 - 4 = 2, str[2..] = MARK
            int startIndexForLiCheck = keyLength - listElementMarkerLength;

            // Optimizations
            return key[startIndexForLiCheck..].Equals(LIST_MARKER);
        }

        public static Tuple<StringBuilder, StringBuilder> BuildElement(string key, Dictionary<string, string>? attributes = null, bool selfClosing = false, string closingSymbols = @"/")
        {
            StringBuilder openingElement = new();
            StringBuilder closingElement = null;

            // Opening Element
            openingElement.Append('<');
            openingElement.Append(key);

            foreach (var attribute in attributes)
            {
                openingElement.Append(' ');
                openingElement.Append(attribute.Key);
                openingElement.Append(@"=""");
                openingElement.Append(attribute.Value);
                openingElement.Append('"');
            }

            if (selfClosing)
                openingElement.Append(closingSymbols);
            openingElement.Append('>');

            if (!selfClosing)
            { // Make the closing tag
              // Closing Element
                closingElement = new();
                closingElement.Append(@"</");
                closingElement.Append(key);
                closingElement.Append('>');
            }

            return new(item1: openingElement, item2: closingElement);
        }

        public static List<StringBuilder> BuildTypes(List<XElementStatistics> elementStatistics)
        {
            // DEBUG
            if (VERBOSE) Console.WriteLine("STATUS: Building ComplexTypes...");
            int iteration = 1;
            int iterationMax = elementStatistics.Count;
            int modulo = iterationMax / PROGRESS_INTERVAL;

            List<StringBuilder> builtComplexTypes = new();

            // Build all complexTypes
            foreach (var elementStatistic in elementStatistics)
            {
                if (elementStatistic.isComplex)
                {
                    // Build a simple type
                    builtComplexTypes.Add(BuildComplexType(elementStatistic));
                }
                else
                {
                    // Build a complex type
                    builtComplexTypes.Add(BuildSimpleType(elementStatistic));
                }

                // DEBUG
                if (VERBOSE && (iteration % modulo == 0 || iteration == iterationMax))
                {
                    Console.WriteLine("ComplexTypes Built: " + iteration + "/" + iterationMax);
                }
                iteration++;
            }

            if (VERBOSE) Console.WriteLine("Building done!");

            // Return the build complexTypes
            return builtComplexTypes;
        }

        public static StringBuilder BuildComplexType(XElementStatistics elementStatistics)
        {
            StringBuilder returnedElement = new();
            Stack<string> closingElements = new();

            // Build the opening tag w/ class-name
            Dictionary<string, string> attributes = new();
            attributes.Add(key: "name", value: elementStatistics.key);

            // Track mixed-ness
            if (elementStatistics.isMixed)
            {
                attributes.Add(key: "mixed", value: "true");
            }

            // Store the opening/closing tags
            Tuple<StringBuilder, StringBuilder> currentElement;
            currentElement = BuildElement(key: "xs:complexType", attributes: attributes);
            returnedElement.Append(currentElement.Item1.Append('\n'));
            closingElements.Push(currentElement.Item2.ToString());

            // Check for children
            if (elementStatistics.possibleChildren.Any())
            {
                // Store the choice element
                attributes.Clear();
                attributes.Add(key: "minOccurs", value: "0");
                attributes.Add(key: "maxOccurs", value: "unbounded");
                currentElement = BuildElement(key: "xs:choice", attributes: attributes);
                returnedElement.Append(currentElement.Item1.Append('\n'));
                closingElements.Push(currentElement.Item2.ToString());

                // Store each child
                foreach (var child in elementStatistics.possibleChildren)
                {
                    attributes.Clear();

                    // Store the name key, catching <li>s
                    string nameKey = child.isLi ? "li"
                                                : child.key;
                    attributes.Add(key: "name", value: nameKey);
                    attributes.Add(key: "type", value: child.key);

                    // Build the child element
                    currentElement = BuildElement(key: "xs:element", attributes: attributes, selfClosing: true);

                    // Store the child element
                    returnedElement.Append(currentElement.Item1.Append('\n'));
                }

                // Close the choice element
                returnedElement.AppendLine(closingElements.Pop());
            }

            // Check for attributes
            if (elementStatistics.possibleAttributes.Any())
            {
                // Store each attribute
                foreach (var attribute in elementStatistics.possibleAttributes)
                {
                    attributes.Clear();
                    attributes.Add(key: "name", value: attribute.Key);
                    // To-do: Maybe use type inferrence instead of defaulting to string???
                    attributes.Add(key: "type", value: "xs:string");

                    // Build the attribute element
                    currentElement = BuildElement(key: "xs:attribute", attributes: attributes, selfClosing: true);

                    // Store the attribute element
                    returnedElement.Append(currentElement.Item1.Append('\n'));
                }
            }

            // Add all closing elements
            while (closingElements.Count > 0)
            {
                returnedElement.AppendLine(closingElements.Pop());
            }

            return returnedElement;
        }

        public static StringBuilder BuildSimpleType(XElementStatistics elementStatistics)
        {
            StringBuilder returnedElement = new();
            Stack<string> closingElements = new();

            // Build the opening tag w/ class-name
            Dictionary<string, string> attributes = new();
            attributes.Add(key: "name", value: elementStatistics.key);

            // Store the opening/closing tags
            Tuple<StringBuilder, StringBuilder> currentElement;
            currentElement = BuildElement(key: "xs:simpleType", attributes: attributes);
            returnedElement.Append(currentElement.Item1.Append('\n'));
            closingElements.Push(currentElement.Item2.ToString());

            // To-do: Maybe do type extrapolation?
            // Store the basic type
            attributes.Clear();
            attributes.Add(key: "base", value: @"xs:string");
            currentElement = BuildElement(key: "xs:restriction", attributes: attributes);
            returnedElement.Append(currentElement.Item1.Append('\n'));
            closingElements.Push(currentElement.Item2.ToString());

            // Add all closing elements
            while (closingElements.Count > 0)
            {
                returnedElement.AppendLine(closingElements.Pop());
            }

            return returnedElement;
        }

        public static StringBuilder BuildXSD(StringBuilder complexType)
        {
            return BuildXSD(new List<StringBuilder>() { complexType });
        }

        public static StringBuilder BuildXSD(List<StringBuilder> complexTypes)
        {
            StringBuilder returnedDocument = new();
            Stack<string> closingElements = new();

            Dictionary<string, string> attributes = new();
            Tuple<StringBuilder, StringBuilder> currentElement;

            // Add the header
            attributes.Add(key: "version", value: @"1.0");
            currentElement = BuildElement(key: @"?xml", attributes: attributes, selfClosing: true, closingSymbols: @"?");
            returnedDocument.Append(currentElement.Item1.Append('\n'));

            // Add the schema element
            attributes.Clear();
            attributes.Add(key: "id", value: "RMMasterSchema");
            attributes.Add(key: "elementFormDefault", value: "qualified");
            attributes.Add(key: "xmlns:xs", value: @"http://www.w3.org/2001/XMLSchema");
            currentElement = BuildElement(key: @"xs:schema", attributes: attributes);
            returnedDocument.Append(currentElement.Item1.Append('\n'));
            closingElements.Push(currentElement.Item2.ToString());

            // Write the parent Defs
            attributes.Clear();
            attributes.Add(key: "name", value: "Defs");
            attributes.Add(key: "type", value: "Defs");
            currentElement = BuildElement(key: @"xs:element", attributes: attributes, selfClosing: true);
            returnedDocument.Append(currentElement.Item1.Append('\n'));

            // Write each complexType in
            foreach (StringBuilder complexType in complexTypes)
            {
                returnedDocument.Append(complexType.Append('\n'));
            }

            // Add all closing elements
            while (closingElements.Count > 0)
            {
                returnedDocument.AppendLine(closingElements.Pop());
            }

            return returnedDocument;
        }

        public static bool WriteXSDToDisk(string name, string path, StringBuilder xsdFile)
        {
            try
            {
                if (VERBOSE) Console.WriteLine("Writing schema to disk!");
                File.WriteAllText(path + '/' + name, xsdFile.ToString());
                if (VERBOSE) Console.WriteLine("Writing done! Wrote information to: " + path + '/' + name);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return false;
            }
        }
    }
}