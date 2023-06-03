using System.Text;
using System.Xml.Linq;

public class SchemaGenHandler : ISchemaGenHandler
{
    // CORE FUNCTIONS
    public static Dictionary<string, List<XElement>> GetXMLNodes(List<string> fileDirectories)
    {
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
                string elementValue = element.Value;
                string listElementMarker = "DelineatedListElement";

                // Check if the element is an Li element
                if (elementName.ToLower().Equals("li"))
                { // Store it with its parent key
                    // Store parent name, catching top-level Li elements
                    string parentName = (element.Parent?.Name.LocalName) ?? throw new NullReferenceException("Li element at top-level!");
                    
                    // Store the newly keyed element.
                    HelperFunctions.AddLD(dictionary: nodesFound, value: element, key: parentName + listElementMarker);
                }
                else
                { // Store the element normally
                    HelperFunctions.AddLD(dictionary: nodesFound, value: element, key: elementName);
                }
            }
        }

        return nodesFound;
    }
    public static Dictionary<string, XElementStatistics> FlattenXmlObjects(Dictionary<string, List<XElement>> xElements)
    {
        // Create a dictionary to store all the flattened elements
        Dictionary<string, XElementStatistics> flattenedXmlElements = new();

        // Iterate over each XML element list by their key
        foreach (KeyValuePair<string, List<XElement>> elementsOfLikeKey in xElements)
        {
            // Store the currently iterate key
            string currentKey = elementsOfLikeKey.Key;
            // Check if we're on an Li element
            string listElementMarker = "DelineatedListElement";
            bool currentKeyIsLi = currentKey.EndsWith(listElementMarker);
            // Create a new flattened xmlObject
            XElementStatistics flattenedXmlElement = new(elementsOfLikeKey.Key, isLi: currentKeyIsLi);

            // Iterate over each element of the current key
            foreach (XElement node in elementsOfLikeKey.Value)
            {
                // Map all attributes to the flattened xmlObject
                // Iterate each attribute
                foreach (var attributeKVP in node.Attributes())
                {
                    // We only need the keys for now
                    flattenedXmlElement.possibleAttributes.Add(attributeKVP.Name.LocalName);
                }

                // Map all children to the flattened xmlObject
                // Iterate over each child
                foreach (var child in node.Elements())
                {
                    flattenedXmlElement.possibleChildren.Add(child.Name.LocalName);
                }

                // Map all values to the flattened xmlObject
                // Capture this node's value
                if (node.Nodes().OfType<XText>().FirstOrDefault() != null)
                    flattenedXmlElement.possibleValues.Add(item: node.Nodes().OfType<XText>().FirstOrDefault().Value);

                // Map all parents to the flattened xmlObject;
                // Capture this node's parent
                if (node?.Parent != null)
                    flattenedXmlElement.possibleParents.Add(node.Parent.Name.LocalName);
            }

            // Split the key into a simple and complex type if a conflict between simple and complex types is found
            if (flattenedXmlElement.possibleValues.Count > 0 
                && (flattenedXmlElement.possibleChildren.Count > 0 
                    || flattenedXmlElement.possibleAttributes.Count > 0))
            {
                string simpleKey = currentKey + "SimpleType";
                XElementStatistics flattenedXmlElementSimple = new(key: simpleKey, isLi: currentKeyIsLi)
                {
                    possibleValues = flattenedXmlElement.possibleValues,
                    possibleParents = flattenedXmlElement.possibleParents
                    
                };

                string complexKey = currentKey + "ComplexType";
                XElementStatistics flattenedXmlElementComplex = new(key: complexKey, isLi: currentKeyIsLi)
                {
                    possibleAttributes = flattenedXmlElement.possibleAttributes,
                    possibleChildren = flattenedXmlElement.possibleChildren,
                    possibleParents = flattenedXmlElement.possibleParents
                };

                flattenedXmlElements.Add(key: elementsOfLikeKey.Key, value: flattenedXmlElementSimple);
                flattenedXmlElements.Add(key: elementsOfLikeKey.Key, value: flattenedXmlElementComplex);
            }
            else
            { // Store the xmlElement normally otherwise
                flattenedXmlElements.Add(key: elementsOfLikeKey.Key, value: flattenedXmlElement);
            }

        }

        return flattenedXmlElements;
    }
    public static void GenerateXSD(Dictionary<string, XElementStatistics> xmlObjectsFlattened, string entryPoint, int breakoffThreshhold, string outputName)
    {
        // DEBUG
        Int128 iteration = 0;
        // !DEBUG

        // Add the opening tag
        StringBuilder returnedXSD = new();
        returnedXSD.AppendLine(@"<?xml version=""1.0"" encoding=""utf-8""?>");
        returnedXSD.AppendLine(@"<xs:schema targetNamespace=""http://rimworldmasterschema.com/rms"""
                                            + @" attributeFormDefault=""unqualified"""
                                            + @" elementFormDefault=""qualified"" xmlns:xs=""http://www.w3.org/2001/XMLSchema"""
                                            + @" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"""
                                            + @" xmlns:rwcs=""http://rimworldmasterschema.com/rwcs"">");

        // Instantiate tracking-stacks 
        Stack<Tuple<string, int>> pendingXmlObjects = new();
        Stack<Tuple<string, int>> closingTags = new();

        // Set initial variables
        int currentDepth = 0;
        Dictionary<string, int> timesProcessed = new();
        pendingXmlObjects.Push(new(entryPoint, currentDepth));

        // Clear the schema and create a new writer
        File.WriteAllText(path: "RimworldMasterSchema.xsd", contents: "");
        var writer = File.AppendText("RimworldMasterSchema.xsd");

        // Create a folder to hold child schemas
        string schemasDirectory = Directory.GetCurrentDirectory() + @"\schemas\";
        Directory.CreateDirectory(schemasDirectory);


        while (pendingXmlObjects.Count > 0)
        { // Iterate over every possible node path
            var currentXmlObjectKVP = pendingXmlObjects.Pop();
            var currentXmlObject = xmlObjectsFlattened[currentXmlObjectKVP.Item1];
            currentDepth = currentXmlObjectKVP.Item2;

            // Break each topmost schema into an individual file
            if (currentDepth == 1)
            {
                // Push closing tags down to current depth
                while (closingTags.Count > 0)
                {
                    returnedXSD.AppendLine(closingTags.Pop().Item1);
                }

                // Add the closing tag
                returnedXSD.AppendLine(@"</xs:schema>");
                // Write the old document
                writer.Write(returnedXSD.ToString());

                // Close the old writer stream
                writer.Close();

                // 
                File.WriteAllText(path: schemasDirectory + currentXmlObject.key + ".xsd", contents: "");
                returnedXSD = new();
                writer = File.AppendText(schemasDirectory + currentXmlObject.key + ".xsd");

                // Write the reference to this XSD in the original
                var originalWriter = File.AppendText("RimworldMasterSchema.xsd");
                originalWriter.WriteLine(@"<xsd:element ref=""rwcs:" + currentXmlObject.key + @"""/>");
                originalWriter.Close();

                // Write boilerplate
                returnedXSD.AppendLine(@"<?xml version=""1.0"" encoding=""utf-8""?>");
                returnedXSD.AppendLine(@"<xs:schema targetNamespace=""http://rimworldmasterschema.com/rwcs"" xmlns:rwcs=""http://rimworldmasterschema.com/rwcs"" xmlns:xs=""http://www.w3.org/2001/XMLSchema"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">");
            }

            // LOGGING
            iteration++;
            if (iteration % 1000 == 0)
            {
                Console.WriteLine("Value: " + iteration);
            }
            // !LOGGING

            // Push closing tags down to current depth
            while (closingTags.Count > 0 && closingTags.Peek().Item2 >= currentDepth)
            {
                returnedXSD.AppendLine(closingTags.Pop().Item1);
            }

            // Emergency Breakoff Point
            //if (currentDepth > breakoffThreshhold)
            //continue;

            // Catch Li's, instead insert any
            if (currentXmlObject.key.Equals("li"))
            {
                returnedXSD.AppendLine(@"<xsd:any minOccurs=""0"" maxOccurs=""unbounded"" processContents=""skip""/>");
            }
            // Write the info for this xmlObject
            else if (currentXmlObject.possibleChildren.Count == 0 && currentXmlObject.possibleAttributes.Count == 0)
            {
                // To-do: Add type locking
                returnedXSD.Append(@"<xs:element name=""");
                returnedXSD.Append(currentXmlObject.key);
                returnedXSD.Append(@""" type=""xs:");
                returnedXSD.Append(currentXmlObject.possibleValues.All(
                                                    xmlObject => int.TryParse(xmlObject, out var _) && xmlObject.Length > 0)
                                                    ? "int"
                                                    : "string");
                returnedXSD.AppendLine(@"""/>");
            }
            else
            {
                bool hasChildren = currentXmlObject.possibleChildren.Count > 0;
                bool hasAttributes = currentXmlObject.possibleAttributes.Count > 0;

                // Write required tags for complex type
                returnedXSD.Append(@"<xs:element name=""");
                returnedXSD.Append(currentXmlObject.key);
                returnedXSD.AppendLine(@""">");
                returnedXSD.AppendLine(@"<xs:complexType>");

                // String added to the closing tags, used to pile-on the </choice> and </attribute> tags
                StringBuilder addedClosingTags = new();

                returnedXSD.AppendLine(@"<xs:choice minOccurs=""0"" maxOccurs=""unbounded"">");

                // Push children to continue looping
                foreach (var child in currentXmlObject.possibleChildren)
                {
                    if (currentDepth + 1 < breakoffThreshhold)
                    {
                        pendingXmlObjects.Push(new(child, currentDepth + 1));
                    }
                }
                addedClosingTags.AppendLine(@"</xs:choice>");

                // Write any possible attributes
                if (hasAttributes)
                {
                    foreach (var attribute in currentXmlObject.possibleAttributes)
                    {
                        addedClosingTags.Append(@"<xs:attribute name=""");
                        addedClosingTags.Append(attribute);
                        addedClosingTags.Append(@""" type=""xs:");
                        addedClosingTags.Append(currentXmlObject.possibleAttributes.All(
                                                    attribute => int.TryParse(attribute, out var _) && attribute.Length > 0)
                                                    ? "int"
                                                    : "string");
                        addedClosingTags.AppendLine(@""" />");
                    }
                }


                // Store the closing tags for this
                closingTags.Push(new(addedClosingTags + @"</xs:complexType>" + "\n" + @"</xs:element>" + "\n", currentDepth));
            }
        }

        // Push closing tags down to current depth
        while (closingTags.Count > 0)
        {
            returnedXSD.AppendLine(closingTags.Pop().Item1);
        }

        // Add the closing tag
        returnedXSD.AppendLine(@"</xs:schema>");

        writer.Write(returnedXSD);
        writer.Close();

        // Move generated elements in the master file, and generate the imports
        HelperFunctions.MoveDocumentLines("RimworldMasterSchema.xsd", start: 11, destination: 6);

        // Generate imports and move them into the master file
        List<string> schemaImports = new();
        foreach (string schema in Directory.GetFiles(schemasDirectory))
        {
            schemaImports.Add(@"<xs:import schemaLocation=""" + schema + @""" namespace=""http://rimworldmasterschema.com/rwcs""/>");
        }
        HelperFunctions.InsertDocumentLines(file: "RimworldMasterSchema.xsd", insertedLines: schemaImports, insertPosition: 3);
    }
}