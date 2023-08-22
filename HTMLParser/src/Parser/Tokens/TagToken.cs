namespace HTML_NET.Parser.Tokens;

public class TagToken : HTMLToken
{
    private KeyValuePair<string, string> _currentAttribute;

    public TagToken(HTMLTokenType type) : base(type)
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
        _currentAttribute = new KeyValuePair<string, string>(name, string.Empty);
        Attributes.Add(_currentAttribute.Key, _currentAttribute.Value);
    }

    public void NewAttribute(char name)
    {
        NewAttribute(new string(name, 1));
    }

    public void AddAttributeName(string value)
    {
        _currentAttribute = new KeyValuePair<string, string>(value, string.Empty);
        Attributes.Add(_currentAttribute.Key, _currentAttribute.Value);
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
}