using Microsoft.VisualBasic;
using SchemaGenerator;
using System.Text;
using System.Xml;
using System.Xml.Linq;

internal class Program
{
    public static Dictionary<string, XElementStatistics> xmlObjectsFlattened;
    public static Dictionary<string, List<XElement>> xmlObjectsFound;

    private static void Main(string[] args)
    {
        List<string> fileDirectories = FindFilesByExtension("xml", new()
        {
            @"C:\Users\tgood.DESKTOP-DK3AJDA\source\repos\SchemaGenerator\bin\Debug\net7.0\",
            @"C:\Program Files (x86)\Steam\steamapps\common\RimWorld\Data\Core\Defs",
            //@"C:\Program Files (x86)\Steam\steamapps\common\RimWorld\Data\Ideology\Defs",
            //@"C:\Program Files (x86)\Steam\steamapps\common\RimWorld\Data\Royalty\Defs",
        });
        xmlObjectsFound = GetXMLNodes(fileDirectories);
        xmlObjectsFlattened = FlattenXmlObjects(xmlObjectsFound);

        try
        {
            File.WriteAllText("RimworldMasterSchema.xsd", GenerateXSD(xmlObjectsFlattened: xmlObjectsFlattened, 
                                                                      entryPoint: "Defs", 
                                                                      breakoffThreshhold: 4).ToString());
            Console.WriteLine("Master Schema written to disk!");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
    }

    private static Dictionary<string, List<XElement>> GetXMLNodes(List<string> fileDirectories)
    {
        Dictionary<string, List<XElement>> nodesFound = new();

        
        foreach (string fileDirectory in fileDirectories)
        {
            // Load each document
            XDocument xDocument = XDocument.Load(fileDirectory);

            // Store each found node in the dictionary/array
            foreach (XElement element in xDocument.Descendants())
            {
                if (nodesFound.TryGetValue(element.Name.LocalName, out var value))
                {
                    // Store if the key is prexisting
                    value.Add(element);
                }
                else
                {
                    // Create a new list to store it otherwise
                    nodesFound.Add(element.Name.LocalName, new() { element });
                }
            }
        }

        return nodesFound;
    }
    private static Dictionary<string, XElementStatistics> FlattenXmlObjects(Dictionary<string, List<XElement>> xElements)
    {
        // Create a point to store all the possible children
        Dictionary<string, XElementStatistics> flattenedXmlObjects = new();

        // Begin mapping the XSD
        foreach (KeyValuePair<string, List<XElement>> xmlObjectsOfKey in xElements)
        {
            // To-do: Add special catch and logic for <li> elements

            // Create a new flattened xmlObject
            XElementStatistics flattenedXmlObject = new(xmlObjectsOfKey.Key);

            // Iterate over each node
            foreach (XElement node in xmlObjectsOfKey.Value)
            {
                // Map all attributes to the flattened xmlObject
                // Iterate each attribute
                foreach (var attributeKVP in node.Attributes())
                {
                    // We only need the keys for now
                    flattenedXmlObject.possibleAttributes.Add(attributeKVP.Name.LocalName);
                }

                // Map all children to the flattened xmlObject
                // Iterate over each child
                foreach (var child in node.Elements())
                {
                    flattenedXmlObject.possibleChildren.Add(child.Name.LocalName);
                }
                    

                // Map all values to the flattened xmlObject
                // Capture this node's value
                if (node.Nodes().OfType<XText>().FirstOrDefault() != null)
                    flattenedXmlObject.possibleValues.Add(item: node.Nodes().OfType<XText>().FirstOrDefault().Value);

                // Map all parents to the flattened xmlObject;
                // Capture this node's parent
                if (node?.Parent != null)
                    flattenedXmlObject.possibleParents.Add(node.Parent.Name.LocalName);
            }

            // Then store the xmlObject for returning
            flattenedXmlObjects.Add(key: xmlObjectsOfKey.Key, value: flattenedXmlObject);
        }

        return flattenedXmlObjects;
    }
    private static StringBuilder GenerateXSD(Dictionary<string, XElementStatistics> xmlObjectsFlattened, string entryPoint, int breakoffThreshhold)
    {
        // DEBUG
        Int128 iteration = 0;
        // !DEBUG

        // Add the opening tag
        StringBuilder returnedXSD = new();
        returnedXSD.AppendLine(@"<?xml version=""1.0"" encoding=""utf-8""?>");
        returnedXSD.AppendLine(@"<xs:schema attributeFormDefault=""unqualified"" elementFormDefault=""qualified"" xmlns:xs=""http://www.w3.org/2001/XMLSchema"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">");

        Stack<Tuple<string, int>> pendingXmlObjects = new();
        Stack<Tuple<string, int>> closingTags = new();

        int currentDepth = 0;
        Dictionary<string, int> timesProcessed = new();
        pendingXmlObjects.Push(new(entryPoint, currentDepth));

        // Clear the schema and create a new writer
        File.WriteAllText(path: "RimworldMasterSchema.xsd", contents: "");
        using var writer = File.AppendText("RimworldMasterSchema.xsd");

        while (pendingXmlObjects.Count > 0)
        { // Iterate over every possible node path
            var currentXmlObjectKVP = pendingXmlObjects.Pop();
            var currentXmlObject = xmlObjectsFlattened[currentXmlObjectKVP.Item1];
            currentDepth = currentXmlObjectKVP.Item2;
            
            // Begin breaking schemas into individual files
            if (currentDepth == 1)
            {
                //returnedXSD = new()
            }

            // LOGGING
            iteration++;
            if (iteration % 1000 == 0)
            {
                Console.WriteLine("Value: " + iteration);

                if (iteration % 15000 == 0)
                {
                    try
                    {
                        // Clear this!
                        writer.Write(returnedXSD.ToString());
                        Console.WriteLine("Master Schema partially written to disk! Resetting stringbuilder...");
                        Console.WriteLine("Pending: " + pendingXmlObjects.Count);
                        returnedXSD = new();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.ToString());
                    }
                }
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
        returnedXSD.AppendLine(@" </xs:schema> ");
        return returnedXSD;
    }

    static List<string> FindFilesByExtension(string extension, List<string> directories)
    {
        List<string> foundFiles = new();

        foreach (string directory in directories)
        {
            if (Directory.Exists(directory))
            {
                string[] files = Directory.GetFiles(directory, $"*.{extension}", SearchOption.AllDirectories);
                foundFiles.AddRange(files);
            }
        }

        return foundFiles;
    }
}
