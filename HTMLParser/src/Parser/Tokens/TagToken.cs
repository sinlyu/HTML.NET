namespace HTML_NET.Parser.Tokens;

public class TagToken : HTMLToken
{
    private KeyValuePair<string, string> _currentAttribute;

    protected TagToken(HTMLTokenType type) : base(type)
    {
        TagName = "";
    }

    public TagToken() : base(HTMLTokenType.StartTag)
    {
        TagName = "";
    }

    public string TagName { get; set; }
    private Dictionary<string, string> Attributes { get; } = new();
    public bool SelfClosing { get; set; }

    public void NewAttribute(string name = "")
    {
        // FIXME: The name gets built up as the parser reads the attribute name
        // Which means that the name is not complete until the attribute value is read
        // This can cause duplicate keys in the Attributes dictionary

        _currentAttribute = new KeyValuePair<string, string>(name, string.Empty);
        Attributes.TryAdd(_currentAttribute.Key, _currentAttribute.Value);
    }

    public void NewAttribute(char name)
    {
        NewAttribute(new string(name, 1));
    }

    public void AddAttributeName(string value)
    {
        // FIXME: This is a hack to to replace the Key of the current attribute
        var newAttribute = new KeyValuePair<string, string>(_currentAttribute.Key + value, _currentAttribute.Value);
        Attributes.Remove(_currentAttribute.Key);
        Attributes.TryAdd(newAttribute.Key, newAttribute.Value);
        _currentAttribute = newAttribute;
    }

    public void AddAttributeName(char value)
    {
        AddAttributeName(new string(value, 1));
    }

    public void AddAttributeValue(string value)
    {
        Attributes[_currentAttribute.Key] += value;
        _currentAttribute = new KeyValuePair<string, string>(_currentAttribute.Key, Attributes[_currentAttribute.Key]);
    }

    public void AddAttributeValue(char value)
    {
        AddAttributeValue(new string(value, 1));
    }

    public override int GetLength()
    {
        throw new NotImplementedException("TagToken.GetLength() is not implemented.");
    }
}