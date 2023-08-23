using System.Text;
using HTML_NET.Parser.Tokens;

namespace HTML_NET.Parser;

public partial class HTMLTokenizer
{
    private readonly ByteBuffer _buffer;
    private readonly Dictionary<Type, HTMLToken> _currentTokens;
    private  StringBuilder _temporaryBuffer;

    private HtmlTokenizerState _currentState;
    private HTMLToken? _nextToken;

    private bool _reconsume;

    // _returnState is not used yet
    private HtmlTokenizerState _returnState;

    public HTMLTokenizer(ByteBuffer buffer)
    {
        _buffer = buffer;
        _currentTokens = new Dictionary<Type, HTMLToken>();
        _currentState = HtmlTokenizerState.Data;
        _returnState = HtmlTokenizerState.Data;
        _temporaryBuffer = new StringBuilder();
    }

    /// <summary>
    ///     Get the next token or null if there are no more tokens from the buffer.
    ///     There is no method to check if the input reached its end right now, so just check for null.
    /// </summary>
    /// <returns>
    ///     HTMLToken or null if there are no more tokens from the buffer.
    /// </returns>
    public HTMLToken? NextToken()
    {
        // TODO: Implement method to check if input is at its end
        while (!_buffer.IsEndOfBuffer())
        {
            var currentInputCharacter = NextCodePoint();
            HandleStateTransition(currentInputCharacter);

            // We just continue if we dont have a token yet
            if (!HasNextToken()) continue;

            var token = _nextToken;
            _nextToken = null;
            return token;
        }

        return null;
    }

    private void HandleStateTransition(char currentInputCharacter)
    {
        switch (_currentState)
        {
            case HtmlTokenizerState.Data:
                DataState(currentInputCharacter);
                break;
            case HtmlTokenizerState.CharacterReference:
                CharacterReferenceState(currentInputCharacter);
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
                AttributeValueSingleQuotedState(currentInputCharacter);
                break;
            case HtmlTokenizerState.AttributeValueUnquoted:
                AttributeValueUnquotedState(currentInputCharacter);
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
            case HtmlTokenizerState.NamedCharacterReference:
                NamedCharacterReferenceState(currentInputCharacter);
                break;
            case HtmlTokenizerState.NumericCharacterReference:
                throw new NotImplementedException("NumericCharacterReferenceState not implemented yet");
                break;
            case HtmlTokenizerState.AmbiguousAmpersand:
                AmbiguousAmpersandState(currentInputCharacter);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(_currentState), "Unknown state");
        }
    }

    private void SwitchState(HtmlTokenizerState state, bool reconsume = false)
    {
        _currentState = state;
        _reconsume = reconsume;
    }

    private char NextCodePoint()
    {
        // Normalize newlines to \n
        // https://infra.spec.whatwg.org/#normalize-newlines

        char codePoint;
        if (_buffer.PeekByte() == '\r' && _buffer.PeekByte(1) == '\n')
        {
            Skip(2);
            codePoint = '\n';
        }
        else if (_buffer.PeekByte() == '\r')
        {
            Skip(1);
            codePoint = '\n';
        }
        else
        {
            codePoint = (char)Consume();
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

    private void Skip(int count)
    {
        _buffer.Skip(count);
        _reconsume = false;
    }

    private bool HasNextToken()
    {
        return _nextToken != null;
    }

    private void EmitToken<T>() where T : HTMLToken, new()
    {
        EmitToken<T>(string.Empty);
    }

    private void EmitToken<T>(char currentInputCharacter) where T : HTMLToken, new()
    {
        EmitToken<T>(currentInputCharacter.ToString());
    }

    private void EmitToken<T>(string data) where T : HTMLToken, new()
    {
        var token = CurrentToken<T>();
        token.Data.Append(data);

        _nextToken = token;
        _currentTokens.Remove(typeof(T));
    }

    // CurrentToken helper method
    // We check if we a have token of the specified type in the current token list
    // If we do, we return it, otherwise we create a new token of the specified type
    private T CurrentToken<T>() where T : HTMLToken, new()
    {
        // if parent class of token is TagToken, we set type to TagToken
        if (typeof(T).IsSubclassOf(typeof(TagToken))) return CurrentToken<T>(typeof(TagToken));

        // if we already have a token of the specified type, we return it
        if (_currentTokens.TryGetValue(typeof(T), out var value)) return (T)value;

        // otherwise we create a new token of the specified type
        var token = new T();
        
        // FIXME: I think Position - 1 is correct here, but I'm not sure
        token.Position = _buffer.Position;
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