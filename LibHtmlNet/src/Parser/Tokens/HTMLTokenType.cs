namespace LibHtmlNet.Parser;

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