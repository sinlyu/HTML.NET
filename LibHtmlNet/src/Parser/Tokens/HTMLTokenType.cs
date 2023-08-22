namespace LibHtmlNet.Parser.Tokens;

public enum HTMLTokenType
{
    Invalid,
    DOCTYPE,
    StartTag,
    EndTag,
    Comment,
    Character,
    EndOfFile
}