using LibHtmlNet.Parser;
using LibHtmlNet.Parser.Tokens;

namespace LibHtmlNet.Tokens;

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

    public void NewAttribute(byte name)
    {
        NewAttribute(new string((char)name, 1));
    }

    public void AddAttributeName(string value)
    {
        _currentAttribute = new KeyValuePair<string, string>(value, string.Empty);
        Attributes.Add(_currentAttribute.Key, _currentAttribute.Value);
    }

    public void AddAttributeName(byte value)
    {
        AddAttributeName(new string((char)value, 1));
    }

    public void AddAttributeValue(string value)
    {
        Attributes[_currentAttribute.Key] += value;
        _currentAttribute = new KeyValuePair<string, string>(_currentAttribute.Key, Attributes[_currentAttribute.Key]);
    }

    public void AddAttributeValue(byte value)
    {
        AddAttributeValue(new string((char)value, 1));
    }
}