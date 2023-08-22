using LibHtmlNet.Parser.Tokens;
using LibHtmlNet.Tokens;

namespace LibHtmlNet.Parser;

public partial class HTMLTokenizer
{
    private readonly ByteBuffer _buffer;
    private readonly Dictionary<Type, HTMLToken> _currentTokens;

    private readonly List<HTMLToken> _tokens;

    private HtmlTokenizerState _currentState = HtmlTokenizerState.Data;

    private bool _reconsume;
    private HtmlTokenizerState _returnState = HtmlTokenizerState.Data;

    public HTMLTokenizer(ByteBuffer buffer)
    {
        _buffer = buffer;
        _tokens = new List<HTMLToken>(1000);
        _currentTokens = new Dictionary<Type, HTMLToken>();
    }

    public IEnumerable<HTMLToken> Tokenize()
    {
        _currentState = HtmlTokenizerState.Data;
        while (!_buffer.IsEndOfBuffer())
        {
            var currentInputCharacter = NextCodePoint();
            HandleStateTransition(currentInputCharacter);
        }

        return _tokens;
    }

    private void HandleStateTransition(byte currentInputCharacter)
    {
        switch (_currentState)
        {
            case HtmlTokenizerState.Data:
                DataState(currentInputCharacter);
                break;
            case HtmlTokenizerState.CharacterReference:
                break;
            case HtmlTokenizerState.TagOpen:
                TagOpenState(currentInputCharacter);
                break;
            case HtmlTokenizerState.EndTagOpen:
                EndTagOpenState(currentInputCharacter);
                break;
            case HtmlTokenizerState.TagName:
                TagNameState(currentInputCharacter);
                break;
            case HtmlTokenizerState.BeforeAttributeName:
                BeforeAttributeNameState(currentInputCharacter);
                break;
            case HtmlTokenizerState.DocType:
                DocTypeState(currentInputCharacter);
                break;
            case HtmlTokenizerState.MarkupDeclarationOpen:
                MarkupDeclarationOpenState(currentInputCharacter);
                break;
            case HtmlTokenizerState.CommentStart:
                CommentStartState(currentInputCharacter);
                break;
            case HtmlTokenizerState.BogusComment:
                BogusCommentState(currentInputCharacter);
                break;
            case HtmlTokenizerState.SelfClosingStartTag:
                SelfClosingStartTagState(currentInputCharacter);
                break;
            case HtmlTokenizerState.AfterAttributeName:
                AfterAttributeNameState(currentInputCharacter);
                break;
            case HtmlTokenizerState.AttributeName:
                AttributeNameState(currentInputCharacter);
                break;
            case HtmlTokenizerState.BeforeAttributeValue:
                BeforeAttributeValueState(currentInputCharacter);
                break;
            case HtmlTokenizerState.CDataSection:
                break;
            case HtmlTokenizerState.BeforeDocTypeName:
                BeforeDocTypeName(currentInputCharacter);
                break;
            case HtmlTokenizerState.DocTypeName:
                DocTypeNameState(currentInputCharacter);
                break;
            case HtmlTokenizerState.AttributeValueDoubleQuoted:
                AttributeValueDoubleQuotedState(currentInputCharacter);
                break;
            case HtmlTokenizerState.AttributeValueSingleQuoted:
                break;
            case HtmlTokenizerState.AttributeValueUnquoted:
                break;
            case HtmlTokenizerState.CommentStartDash:
                CommentStartDashState(currentInputCharacter);
                break;
            case HtmlTokenizerState.Comment:
                CommentState(currentInputCharacter);
                break;
            case HtmlTokenizerState.CommentLessThanSign:
                CommentLessThanSignState(currentInputCharacter);
                break;
            case HtmlTokenizerState.CommentEndDash:
                CommentEndDashState(currentInputCharacter);
                break;
            case HtmlTokenizerState.AfterAttributeValueQuoted:
                AfterAttributeValueQuotedState(currentInputCharacter);
                break;
            case HtmlTokenizerState.CommentEnd:
                CommentEndState(currentInputCharacter);
                break;
            case HtmlTokenizerState.CommentEndBang:
                CommentEndBangState(currentInputCharacter);
                break;
            case HtmlTokenizerState.CommentLessThanSignBang:
                CommentLessThanSignBangState(currentInputCharacter);
                break;
            case HtmlTokenizerState.CommentLessThanSignBangDashDash:
                CommentLessThanSignBangDashDashState(currentInputCharacter);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void SwitchState(HtmlTokenizerState state, bool reconsume = false)
    {
        _currentState = state;
        _reconsume = reconsume;
    }

    private byte NextCodePoint()
    {
        // TODO: Work with char instead of byte
        // Normalize newlines to \n
        // https://infra.spec.whatwg.org/#normalize-newlines
        
        byte codePoint;
        if (_buffer.PeekByte(0) == 0x0D && _buffer.PeekByte(1) == 0x0A)
        {
            Skip(2);
            codePoint = 0x0A;
        } else if (_buffer.PeekByte(0) == 0x0D)
        {
            Skip(1);
            codePoint = 0x0A;
        }
        else
        {
            codePoint = Consume();
        }

        return codePoint;
    }

    private byte Consume()
    {
        if (!_reconsume) return _buffer.ReadByte();

        // When the reconsume flag is set, we provide the last byte read from the buffer
        _reconsume = false;
        _buffer.UnreadByte();
        return _buffer.ReadByte();
    }

    // We need a method like Consume but with a parameter to specify how many bytes we want to consume
    // This method is used after peeking ahead and we are sure we want to consume the bytes
    // But we already know whats inside so we dont need the actual bytes
    private void Skip(int count)
    {
        for (var i = 0; i < count; i++) Consume();
    }

    private void EmitToken<T>() where T : HTMLToken, new()
    {
        EmitToken<T>(Array.Empty<byte>());
    }

    private void EmitToken<T>(byte currentInputCharacter) where T : HTMLToken, new()
    {
        EmitToken<T>(new[] { currentInputCharacter });
    }

    private void EmitToken<T>(IEnumerable<byte> data) where T : HTMLToken, new()
    {
        var token = CurrentToken<T>();

        // TODO: Implement an elegant way to populate the token data
        token.Data.Clear();
        token.Data.AddRange(data);

        _tokens.Add(token);
        _currentTokens.Remove(typeof(T));
    }

    // CurrentToken helper method
    // We check if we a have token of the specified type in the current token list
    // If we do, we return it, otherwise we create a new token of the specified type
    private T CurrentToken<T>() where T : HTMLToken, new()
    {
        // if parent class of token is TagToken, we set type to TagToken
        if (typeof(T).IsSubclassOf(typeof(TagToken))) return CurrentToken<T>(typeof(TagToken));

        if (_currentTokens.TryGetValue(typeof(T), out var value)) return (T)value;
        var token = new T();
        _currentTokens.Add(typeof(T), token);
        return token;
    }

    private T CurrentToken<T>(Type parentType) where T : HTMLToken, new()
    {
        if (_currentTokens.TryGetValue(parentType, out var value))
        {
            var token = (TagToken)value;
            if (token is T typedToken) return typedToken;
        }

        var newToken = new T();
        _currentTokens.Add(parentType, newToken);
        return newToken;
    }
}