namespace HTML_NET.Parser.Tokens;

public class CommentToken : HTMLToken
{
    public CommentToken() : base(HTMLTokenType.Comment) { }
    public override int GetLength()
    {
        throw new NotImplementedException("CommentToken.GetLength() is not implemented.");
    }
}