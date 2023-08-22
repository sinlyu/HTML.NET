using LibHtmlNet.Tokens;

namespace LibHtmlNet.Parser.Tokens;

public class EndTagToken : TagToken
{
    public EndTagToken() : base(HTMLTokenType.EndTag)
    {
    }
}