using LibHtmlNet.Tokens;

namespace LibHtmlNet.Parser.Tokens;

public class StartTagToken : TagToken
{
    public StartTagToken() : base(HTMLTokenType.StartTag)
    {
    }
}