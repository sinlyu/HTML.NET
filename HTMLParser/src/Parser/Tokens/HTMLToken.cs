using System.Text;

namespace HTML_NET.Parser.Tokens;

public abstract class HTMLToken
{
    private HTMLToken(HTMLTokenType type, string data)
    {
        Type = type;
        Data = new StringBuilder(data);
    }

    protected HTMLToken(HTMLTokenType type) : this(type, string.Empty) { }

    public abstract int GetLength();
    
    public HTMLTokenType Type { get; protected set; }
    public long Position { get; set; }
    public StringBuilder Data { get; set; }
}