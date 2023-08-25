namespace HTML_NET.Parser.Tokens;

public class TagToken : HTMLToken
{
    private string _currentAttributeName;
    private string _currentAttributeValue;

    protected TagToken(HTMLTokenType type) : base(type)
    {
        TagName = "";
        _currentAttributeName = "";
        _currentAttributeValue = "";
    }

    public TagToken() : base(HTMLTokenType.StartTag)
    {
        TagName = "";
        _currentAttributeName = "";
        _currentAttributeValue = "";
    }

    public string TagName { get; set; }
    private Dictionary<string, string> Attributes { get; } = new();
    public bool SelfClosing { get; set; }

    public void NewAttribute(string name = "")
    {
        _currentAttributeName = name;
        _currentAttributeValue = "";
    }

    public void NewAttribute(char name)
    {
        NewAttribute(new string(name, 1));
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

    public override int GetLength()
    {
        throw new NotImplementedException("TagToken.GetLength() is not implemented.");
    }
}