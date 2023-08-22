namespace HTML_NET.Parser.Tokens;

public abstract class HTMLToken
{
    private HTMLToken(HTMLTokenType type, string data)
    {
        Type = type;
        Data = data;
    }

    protected HTMLToken(HTMLTokenType type) : this(type, string.Empty)
    {
    }

    protected HTMLToken() : this(HTMLTokenType.Invalid, string.Empty)
    {
    }

    public HTMLTokenType Type { get; protected set; }
    
    // TODO: Instead of string we could use a StringBuilder
    public string Data { get; set; }
}