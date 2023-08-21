namespace LibHtmlNet;

public enum HtmlTokenizerState
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
    AfterAttributeValueQuoted,
    CommentEnd,
    CommentEndBang,
    CommentLessThanSignBang,
    CommentLessThanSignBangDashDash
}