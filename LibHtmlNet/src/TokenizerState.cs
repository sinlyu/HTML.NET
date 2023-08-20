namespace LibHtmlNet;

public enum TokenizerState
{
    CharacterReference,
    TagOpen,
    EndTagOpen,
    TagName,
    BeforeAttributeName,
    Data,
    DocType,
    MarkupDeclarationOpen,
    CommentStart,
    BogusComment,
    SelfClosingStartTag,
    AfterAttributeName,
    AttributeName,
    BeforeAttributeValue,
    CDataSection,
    BeforeDocTypeName,
    DocTypeName,
    AttributeValueDoubleQuoted,
    AttributeValueSingleQuoted,
    AttributeValueUnquoted,
    CommentStartDash,
    Comment,
    CommentLessThanSign,
    CommentEndDash,
    AfterAttributeValueQuoted
}