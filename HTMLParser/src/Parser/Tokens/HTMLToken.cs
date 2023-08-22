namespace HTML_NET.Parser.Tokens;

public abstract class HTMLToken
{
    private HTMLToken(HTMLTokenType type, List<byte> data)
    {
        Type = type;
        Data = data;
    }

    protected HTMLToken(HTMLTokenType type) : this(type, new List<byte>())
    {
    }

    protected HTMLToken() : this(HTMLTokenType.Invalid, new List<byte>())
    {
    }

    public HTMLTokenType Type { get; protected set; }
    public List<byte> Data { get; protected set; }
}