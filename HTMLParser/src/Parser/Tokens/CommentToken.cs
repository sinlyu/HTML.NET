namespace HTML_NET.Parser.Tokens;

public class CommentToken : HTMLToken
{
    public CommentToken() : base(HTMLTokenType.Comment)
    {
    }
}