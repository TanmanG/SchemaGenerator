public class XmlObject
{
    public readonly int depth;
    public readonly string key;
    public string value;

    public readonly Dictionary<string, string> attributes;
    public readonly List<XmlObject> children;
    public readonly XmlObject? parent;

    public XmlObject(string key, XmlObject? parent, Dictionary<string, string> attributes, int depth)
    {
        this.key = key;
        this.parent = parent;
        this.depth = depth;

        this.attributes = attributes;
        parent?.children.Add(this);
        children = new();
    }
    public XmlObject(string key, string value, XmlObject? parent, Dictionary<string, string> parameters, int depth)
    {
        this.key = key;
        this.value = value;
        this.parent = parent;

        this.attributes = parameters;
        parent?.children.Add(this);
        this.children = new();
        this.depth = depth;
    }
}