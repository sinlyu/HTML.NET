namespace HTML_NET.Parser.Tokens;

public class StartTagToken : TagToken
{
    public StartTagToken() : base(HTMLTokenType.StartTag)
    {
    }
}