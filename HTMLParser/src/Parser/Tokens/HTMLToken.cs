using System.Text;

namespace HTML_NET.Parser.Tokens;

public sealed class HTMLToken
{
    private string _currentAttributeName;
    private string _currentAttributeValue;
    
    public HTMLToken(HTMLTokenType type, string data)
    {
        Type = type;
        Data = new StringBuilder(data);
        Attributes = new Dictionary<string, string>();
    }

    public HTMLToken(HTMLTokenType type) : this(type, string.Empty)
    {
    }

    public HTMLTokenType Type { get; set; }
    public long Position { get; set; }
    public StringBuilder Data { get; set; }
    
    public string Name { get; set; }
    public bool ForceQuirks { get; set; }
    public string TagName { get; set; }
    
    public bool SelfClosing { get; set; }
    public int GetLength()
    {
        return 0;
    }
    
    public void NewAttribute(char name)
    {
        NewAttribute(new string(name, 1));
    }

    public void NewAttribute(string name = "")
    
    {
        _currentAttributeName = name;
        _currentAttributeValue = "";
    }

    public void AddAttributeName(string value)
    {
        _currentAttributeValue += value;
    }

    public void AddAttributeName(char value)
    {
        AddAttributeName(new string(value, 1));
    }

    public void AddAttributeValue(string value)
    {
        _currentAttributeValue += value;
    }

    public void AddAttributeValue(char value)
    {
        AddAttributeValue(new string(value, 1));
    }
    
    public void FinishAttribute()
    {
        if(string.IsNullOrWhiteSpace(_currentAttributeName))
            return;

        if (Attributes.ContainsKey(_currentAttributeName))
            Attributes[_currentAttributeName] = _currentAttributeValue;
        
        _currentAttributeName = "";
        _currentAttributeValue = "";
    }

    public Dictionary<string, string> Attributes { get; set; }
}