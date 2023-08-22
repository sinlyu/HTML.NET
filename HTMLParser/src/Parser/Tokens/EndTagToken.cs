namespace HTML_NET.Parser.Tokens;

public class EndTagToken : TagToken
{
    public EndTagToken() : base(HTMLTokenType.EndTag)
    {
    }
}