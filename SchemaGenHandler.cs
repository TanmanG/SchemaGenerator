using System.Runtime.InteropServices;
using System.Text;
using System.Xml.Linq;

public class SchemaGenHandler : ISchemaGenHandler
{
    // CORE FUNCTIONS
    
    public static Dictionary<string, List<XElement>> GetXMLNodes(List<string> fileDirectories, string listElementMarker)
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

                // Check if the element is an Li element
                if (elementName.ToLower().Equals("li"))
                { // Store it with its parent key

                    // Store parent name, catching top-level Li elements
                    string liReplacementName = GetReplacementLiKey(element, listElementMarker);

                    // Store the newly keyed element.
                    HelperFunctions.AddLD(dictionary: nodesFound, value: element, key: liReplacementName);
                }
                else
                { // Store the element normally
                    HelperFunctions.AddLD(dictionary: nodesFound, value: element, key: elementName);
                }
            }
        }

        return nodesFound;
    }

    public static XElementStatistics FlattenXMLObject(string key, List<XElement> xElements, string listElementMarker)
    {
        // Track whether this is an Li element or not
        bool isLi = CheckKeyIsLi(key, listElementMarker);

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
                    string liReplacementName = GetReplacementLiKey(element.Parent, listElementMarker);
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
                        string liReplacementName = GetReplacementLiKey(child, listElementMarker);
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
        if (flattenedElement.possibleParents.Any() || flattenedElement.possibleAttributes.Any())
        {
            flattenedElement.isComplex = true;
        }
        // Check for mixed
        if (flattenedElement.possibleValues.Any()
            && (flattenedElement.possibleParents.Any() || flattenedElement.possibleAttributes.Any()))
        {
            flattenedElement.isComplex = true;
        }

        // Return everything
        return flattenedElement;
    }
    
    public static Dictionary<string, XElementStatistics> FlattenXmlObjects(Dictionary<string, List<XElement>> xElements, string listElementMarker)
    {
        // Create a dictionary to store all the flattened elements
        Dictionary<string, XElementStatistics> flattenedXmlElements = new();

        // Group by each key
        foreach (var elementsOfLikeKey in xElements)
        {
            // Store references to the KVP contents
            string currentKey = elementsOfLikeKey.Key;
            List<XElement> currentElements = elementsOfLikeKey.Value;

            // Flatten the current XML object
            var flattenedElement = FlattenXMLObject(currentKey, currentElements, listElementMarker);

            // And store it for later processing & return
            flattenedXmlElements.Add(key: flattenedElement.key, flattenedElement);
        }

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
        }

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

    public static string GetReplacementLiKey(XElement element, string listElementMarker)
    {
        return ((element.Parent?.Name.LocalName) ?? throw new NullReferenceException("Li element at top-level!")) + listElementMarker;
    }

    public static bool CheckKeyIsLi(string key, string listElementMarker)
    {
        // Calculate the theorhetical start point
        int keyLength = key.Length;
        int listElementMarkerLength = listElementMarker.Length;

        // Optimization to catch impossible length'd keys
        if (keyLength < listElementMarkerLength)
        {
            return false;
        }

        // Store the entry point (e.g. "MARK" w/ "hiMARK" -> 6 - 4 = 2, str[2..] = MARK
        int startIndexForLiCheck = keyLength - listElementMarkerLength;

        // Optimizations
        return key[startIndexForLiCheck..].Equals(listElementMarker);
    }

    public static Tuple<StringBuilder, StringBuilder> BuildElement(string key, Dictionary<string, string>? attributes = null, bool selfClosed = false)
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
            openingElement.Append(@"""");
        }

        if (selfClosed)
            openingElement.Append('/');
        openingElement.Append('>');

        if (!selfClosed)
        { // Make the closing tag
            // Closing Element
            closingElement = new();
            closingElement.Append(@"</");
            closingElement.Append(key);
            closingElement.Append('>');
        }

        return new(item1: openingElement, item2: closingElement);
    }

    public static StringBuilder BuildComplexType(XElementStatistics elementStatistics)
    {
        StringBuilder returnedElement = new();
        Stack<string> closingElements = new();

        // Build the opening tag w/ class-name
        Dictionary<string, string> attributes = new()
        {
            { "name", elementStatistics.key },
        };

        // Track mixed-ness
        if (elementStatistics.isMixed)
        {
            attributes.Add(key: "mixed", value: "true");
        }

        // Store the opening/closing tags
        var currentTag = BuildElement(key: "complexType", attributes: attributes);
        returnedElement.Append(currentTag.Item1);
        closingElements.Push(currentTag.Item2.ToString());

        // Check for children
        if (elementStatistics.possibleChildren.Any())
        {
            // Store the choice element
            attributes.Clear();
            attributes.Add(key: "minOccurs", value: "0");
            attributes.Add(key: "maxOccurs", value: "unbounded");
            currentTag = BuildElement(key: "xs:choice", attributes: attributes);
            returnedElement.Append(currentTag.Item1.Append('\n'));
            closingElements.Push(currentTag.Item2.ToString());

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
                currentTag = BuildElement(key: "xs:element", attributes : attributes, selfClosed: true);

                // Store the child element
                returnedElement.Append(currentTag.Item1.Append('\n'));
            }
            
            // Close the choice element
            returnedElement.AppendLine(closingElements.Pop());
        }

        // Check for attributes
        if (elementStatistics.possibleAttributes.Any())
        {
            attributes.Clear();

            // Store each attribute
            foreach (var attribute in elementStatistics.possibleAttributes)
            {
                attributes.Add(key: "name", value: attribute.Key);
                // To-do: Maybe use type inferrence instead of defaulting to string???
                attributes.Add(key: "type", value: "xs:string");

                // Build the attribute element
                currentTag = BuildElement(key: "xs:attribute", attributes: attributes, selfClosed: true);

                // Store the attribute element
                returnedElement.Append(currentTag.Item1.Append('\n'));
            }
        }

        // Add all closing elements
        while (closingElements.Count > 0)
        {
            returnedElement.AppendLine(closingElements.Pop());
        }

        return returnedElement;
    }

    public static StringBuilder BuildXSDFile(XElementStatistics elementStatistics, StringBuilder complexType)
    {
        throw new NotImplementedException();
    }
}