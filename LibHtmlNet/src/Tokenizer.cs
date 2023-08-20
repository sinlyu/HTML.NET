using System.Text;
using LibHtmlNet.Tokens;

namespace LibHtmlNet;

public class Tokenizer
{
    private readonly ByteBuffer _buffer;
    private TokenizerState _currentState = TokenizerState.Data;
    private TokenizerState _returnState = TokenizerState.Data;
    private List<Token> _tokens;
    private readonly Dictionary<Type, Token> _currentTokens;
    
    private bool _reconsume = false;
    
    public Tokenizer(ByteBuffer buffer)
    {
        _buffer = buffer;
        _tokens = new List<Token>(1000);
        _currentTokens = new Dictionary<Type, Token>();
    }

    public byte Consume()
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
    private void Consume(int count)
    {
        for (var i = 0; i < count; i++)
        {
            Consume();
        }
    }
    
    private void EmitToken<T>() where T : Token, new()
    {
        EmitToken<T>(Array.Empty<byte>());
    }
    
    private void EmitToken<T>(byte currentInputCharacter) where T : Token, new()
    {
        EmitToken<T>(new byte[] { currentInputCharacter });
    }
    
    private void EmitToken<T>(byte[] data) where T : Token, new()
    {
        var token = CurrentToken<T>();
        token.Data = new List<byte>(data); 
        _tokens.Add(token);
        _currentTokens.Remove(typeof(T));
    }

    // CurrentToken helper method
    // We check if we a have token of the specified type in the current token list
    // If we do, we return it, otherwise we create a new token of the specified type
    private T CurrentToken<T>() where T : Token, new()
    {
        // if parent class of token is TagToken, we set type to TagToken
        if (typeof(T).IsSubclassOf(typeof(TagToken)))
        {
            return CurrentToken<T>(typeof(TagToken));
        }
        
        if(_currentTokens.TryGetValue(typeof(T), out Token value)) return (T)value;
        var token = new T();
        _currentTokens.Add(typeof(T), token);
        return token;
    }
    
    private T CurrentToken<T>(Type parentType) where T : Token, new()
    {
        if(_currentTokens.TryGetValue(parentType, out Token value))
        {
            var token = (TagToken)value;
            if (token is T typedToken) return typedToken;
        }
        
        var newToken = new T();
        _currentTokens.Add(parentType, newToken);
        return newToken;
    }
    
    private void DataState(byte currentInputCharacter)
    {
        switch (currentInputCharacter)
        {
            // Switch to TagOpen state if we encounter a 0x3C '<' character
            // <
            case 0x3C:
                SwitchState(state: TokenizerState.TagOpen);
                break;
            
            // Switch to CharacterReference state if we encounter a 0x26 '&' character
            // Set the return state to Data
            // &
            case 0x26:
                SwitchState(state: TokenizerState.CharacterReference);
                _returnState = TokenizerState.Data;
                break;
            
            // Emit the current input character as a character token
            case 0x00:
                // TODO: Implement ParseError
                // unexpected-null-character
                var token = CurrentToken<CharacterToken>();
                token.Data = new List<byte>(new byte[] { currentInputCharacter });
                EmitToken<CharacterToken>();
                break;
            
            // Anything else
            default:
                // Emit the current input character as a character token
                EmitToken<CharacterToken>(currentInputCharacter);
                break;
        }
        
        // TODO: Implement EOF handling
    }

    private void TagOpenState(byte currentInputCharacter)
    {
        switch (currentInputCharacter)
        {
            // Switch to MarkupDeclaration state if we encounter a 0x21 '!' character
            case 0x21: // !
                SwitchState(state: TokenizerState.MarkupDeclarationOpen);
                break;
            
            // Switch to EndTagOpen state if we encounter a 0x2F '/' character
            case 0x2F: // /
                SwitchState(state: TokenizerState.EndTagOpen);
                break;
            
            // Check if current input character is an ASCII alpha character
            // Create a new start tag token, set its tag name to the empty string. Reconsume in the tag name state.
            case >= 0x41 and <= 0x5A: // A-Z
            case >= 0x61 and <= 0x7A: // a-z
                CurrentToken<StartTagToken>();
                SwitchState(state: TokenizerState.TagName, reconsume: true);
                break;
            
            // This is an unexpected-question-mark-instead-of-tag-name parse error.
            // Create a comment token whose data is the empty string. Reconsume in the bogus comment state.
            case 0x3F: // ?
                // TODO: Implement ParseError
                EmitToken<CommentToken>();
                SwitchState(state: TokenizerState.BogusComment, reconsume: true);
                break;
            
            // This is an invalid-first-character-of-tag-name parse error.
            // Emit a U+003C LESS-THAN SIGN character token. Reconsume in the data state.
            default:
                // TODO: Implement ParseError
                EmitToken<CharacterToken>(0x3C );
                SwitchState(state: TokenizerState.Data, reconsume: true);
                break;
        }
        
        // TODO: Implement EOF handling
    }

    private void EndTagOpenState(byte currentInputCharacter)
    {
        switch (currentInputCharacter)
        {
            // Create a new end tag token, set its tag name to the empty string.
            // Reconsume in the tag name state.
            case >= 0x41 and <= 0x5A: // A-Z
            case >= 0x61 and <= 0x7A: // a-z
                CurrentToken<EndTagToken>();
                SwitchState(state: TokenizerState.TagName, reconsume: true);
                break;
            
            // This is a missing-end-tag-name parse error.
            // Switch to the data state.
            case 0x3E: // >
                // TODO: Implement ParseError
                SwitchState(state: TokenizerState.Data);
                break;
            
            // This is an invalid-first-character-of-tag-name parse error.
            // Create a comment token whose data is the empty string.
            // Reconsume in the bogus comment state.
            default:
                // TODO: Implement ParseError
                EmitToken<CommentToken>();
                SwitchState(state: TokenizerState.BogusComment);
                break;
        }
        
        // TODO: Implement EOF handling
    }

    private void TagNameState(byte currentInputCharacter)
    {
        switch (currentInputCharacter)
        {
            // Switch to the before attribute name state.
            case 0x09: // \t
            case 0x0A: // \n
            case 0x0C: // \f
            case 0x20: // space
                SwitchState(state: TokenizerState.BeforeAttributeName);
                break;
            
            // Switch to the self-closing start tag state.
            case 0x2F: // /
                SwitchState(state: TokenizerState.SelfClosingStartTag);
                break;
            
            // Switch to the data state. Emit the current tag token.
            case 0x3E: // >
                EmitToken<TagToken>();
                SwitchState(state: TokenizerState.Data);
                break;
            
            // ASCII upper alpha character
            // Append the lowercase version of the current input character (add 0x0020 to the character's code point) to the current tag token's tag name.
            case >= 0x41 and <= 0x5A: // A-Z
                CurrentToken<TagToken>().TagName +=
                    Encoding.UTF8.GetString( new [] { (byte)(currentInputCharacter + 0x20) });
                break;
            
            // Anything else
            // Append the current input character to the current tag token's tag name.
            default:
                CurrentToken<TagToken>().TagName +=
                    Encoding.UTF8.GetString(new [] { currentInputCharacter });
                break;
        }
        
        // TODO: Implement EOF handling
    }

    private void BeforeAttributeNameState(byte currentInputCharacter)
    {
        switch (currentInputCharacter)
        {
            // Ignore the character.
            case 0x09: // \t
            case 0x0A: // \n
            case 0x0C: // \f
            case 0x20: // space
                break;
            
            // Reconsume in the after attribute name state.
            case 0x2F: // /
            case 0x3E: // >
                SwitchState(state: TokenizerState.AfterAttributeName, reconsume: true);
                break;
            
            // This is an unexpected-equals-sign-before-attribute-name parse error.
            // Start a new attribute in the current tag token. Set that attribute's name to the current input character,
            // and its value to the empty string. Switch to the attribute name state.
            case 0x3D: // =
                // TODO: Implement ParseError
                CurrentToken<StartTagToken>().StartNewAttribute(Encoding.UTF8.GetString(new [] { currentInputCharacter }));
                SwitchState(TokenizerState.AttributeName);
                break;
            
            // Anything else
            // Start a new attribute in the current tag token.
            // Set that attribute's name and value to the empty string.
            default:
                CurrentToken<StartTagToken>().StartNewAttribute();
                SwitchState(TokenizerState.AttributeName, reconsume: true);
                break;  
        }
        
        // TODO: Implement EOF handling
    }

    private void AttributeNameState(byte currentInputCharacter)
    {
        switch (currentInputCharacter)
        {
            // Reconsume in the after attribute name state.
            case 0x09: // \t
            case 0x0A: // \n
            case 0x0C: // \f
            case 0x20: // space
            case 0x2F: // /
            case 0x3E: // >
                SwitchState(TokenizerState.AfterAttributeName, reconsume: true);
                break;
            
            // Switch to the before attribute value state.
            case 0x3D: // =
                SwitchState(TokenizerState.BeforeAttributeValue);
                break;
            
            // ASCII upper alpha character
            // Append the lowercase version of the current input character (add 0x0020 to the character's code point) to the current attribute's name.
            case >= 0x41 and <= 0x5A: // A-Z
                var letter = Encoding.UTF8.GetString(new [] { (byte)(currentInputCharacter + 0x20) });
                CurrentToken<StartTagToken>().CurrentAttribute.Name += letter;
                break;
            
            // This is an unexpected-null-character parse error.
            // Append a U+FFFD REPLACEMENT CHARACTER character to the current attribute's name.
            case 0x00: // NULL
                CurrentToken<StartTagToken>().CurrentAttribute.Name += "\uFFFD";
                break;
            
            // This is an unexpected-character-in-attribute-name parse error.
            // Treat it as per the "anything else" entry below.
            case 0x22: // "
            case 0x27: // '
            case 0x3C: // <
                // TODO: Implement ParseError
                // https://html.spec.whatwg.org/multipage/parsing.html#attribute-name-state
            default:
                var character = Encoding.UTF8.GetString(new [] { currentInputCharacter });
                CurrentToken<StartTagToken>().CurrentAttribute.Name += character;
                break;
        }
    }

    private void MarkupDeclarationOpenState(byte currentInputCharacter)
    {
        // If the next two characters are both U+002D (-) characters
        // Consume those two characters, create a comment token whose data is the empty string, and switch to the comment start state.
        if (currentInputCharacter == 0x2D && _buffer.PeekByte() == 0x2D)
        {
            Consume();
            CurrentToken<CommentToken>();
            SwitchState(TokenizerState.CommentStart);
        }
        
        // If the next seven characters are an ASCII case-insensitive match for the word "DOCTYPE"
        // Consume those characters and switch to the DOCTYPE state.
        else if (_buffer.MatchCaseInsensitiveString("DOCTYPE"))
        {
            Consume(7);
            SwitchState(TokenizerState.DocType);
        }
        
        // If the next seven characters are an ASCII case-insensitive match for the string "[CDATA["
        // Consume those characters and switch to the CDATA section state.
        else if (_buffer.MatchCaseInsensitiveString("[CDATA["))
        {
            // TODO: Implement check for adjusted current node being an element in the HTML namespace
            // TODO: Implement ParseError
            Consume(7);
            SwitchState(TokenizerState.CDataSection);
        }

        // This is an incorrectly-opened-comment parse error.
        // Create a comment token whose data is the empty string.
        // Switch to the bogus comment state (don't consume anything in the current state).
        else
        {
            // TODO: Implement ParseError
            CurrentToken<CommentToken>();
            SwitchState(TokenizerState.BogusComment);
        }
    }

    private void DocTypeState(byte currentInputCharacter)
    {
        switch (currentInputCharacter)
        {
            // Switch to the before DOCTYPE name state.
            case 0x09: // \t
            case 0x0A: // \n
            case 0x0C: // \f
            case 0x20: // space
                SwitchState(TokenizerState.BeforeDocTypeName);
                break;
            
            // Reconsume in the before DOCTYPE name state.
            case 0x3E: // >
                SwitchState(TokenizerState.BeforeDocTypeName, reconsume: true);
                break;
            
            // Anything else
            // This is a missing-whitespace-before-doctype-name parse error.
            // Reconsume in the before DOCTYPE name state.
            default:
                // TODO: Implement ParseError
                SwitchState(TokenizerState.BeforeDocTypeName, reconsume: true);
                break;
        }
        
        // TODO: Implement EOF handling
    }

    private void BeforeDocTypeName(byte currentInputCharacter)
    {
        switch (currentInputCharacter)
        {
            // Ignore the character.
            case 0x09: // \t
            case 0x0A: // \n
            case 0x0C: // \f
            case 0x20: // space
                break;
            
            // ASCII upper alpha character
            // Create a new DOCTYPE token. Set its name to the lowercase version of the current input character (add 0x0020 to the character's code point).
            // Switch to the DOCTYPE name state.
            case >= 0x41 and <= 0x5A: // A-Z
                CurrentToken<DocTypeToken>().Name = Encoding.UTF8.GetString(new [] { (byte)(currentInputCharacter + 0x20) });
                SwitchState(TokenizerState.DocTypeName);
                break;
            
            // This is an unexpected-null-character parse error.
            // Create a new DOCTYPE token. Set its name to a U+FFFD REPLACEMENT CHARACTER character.
            // Switch to the DOCTYPE name state.
            case 0x00: // NULL
                CurrentToken<DocTypeToken>().ForceQuirks = true;
                EmitToken<DocTypeToken>();
                SwitchState(TokenizerState.Data);
                break;
            
            // This is an missing-doctype-name parse error.
            // Create a new DOCTYPE token. Set its force-quirks flag to on.
            // Emit the token.
            // Switch to the data state.
            case 0x3E: // >
                CurrentToken<DocTypeToken>().ForceQuirks = true;
                EmitToken<DocTypeToken>();
                SwitchState(TokenizerState.Data);
                break;
            
            // Anything else
            // Create a new DOCTYPE token. Set its name to the current input character.
            // Switch to the DOCTYPE name state.
            default:
                CurrentToken<DocTypeToken>().Name = Encoding.UTF8.GetString(new [] { currentInputCharacter });
                SwitchState(TokenizerState.DocTypeName);
                break;
        }
    }

    private void DocTypeNameState(byte currentInputCharacter)
    {
        switch (currentInputCharacter)
        {
            // Ignore the character.
            case 0x09: // \t
            case 0x0A: // \n
            case 0x0C: // \f
            case 0x20: // space
                break;
            
            // Switch to the data state.
            // Emit the current DOCTYPE token.
            case 0x3E: // >
                EmitToken<DocTypeToken>();
                SwitchState(TokenizerState.Data);
                break;
            
            // ASCII upper alpha character
            // Append the lowercase version of the current input character (add 0x0020 to the character's code point) to the current DOCTYPE token's name.
            case >= 0x41 and <= 0x5A: // A-Z
                CurrentToken<DocTypeToken>().Name += Encoding.UTF8.GetString(new [] { (byte)(currentInputCharacter + 0x20) });
                break;
            
            // This is an unexpected-null-character parse error.
            // Append a U+FFFD REPLACEMENT CHARACTER character to the current DOCTYPE token's name.
            case 0x00: // NULL
                // TODO: Implement ParseError
                CurrentToken<DocTypeToken>().Name += "\uFFFD";
                break;
            
            // Anything else
            // Append the current input character to the current DOCTYPE token's name.
            default:
                CurrentToken<DocTypeToken>().Name += Encoding.UTF8.GetString(new [] { currentInputCharacter });
                break;
        }
        
        // TODO: Implement EOF handling
    }

    private void BeforeAttributeValueState(byte currentInputCharacter)
    {
        switch (currentInputCharacter)
        {
            // Ignore the character.
            case 0x09: // \t
            case 0x0A: // \n
            case 0x0C: // \f
            case 0x20: // space
                break;
            
            // Switch to the attribute value (double-quoted) state.
            case 0x22: // "
                SwitchState(TokenizerState.AttributeValueDoubleQuoted);
                break;
            
            // Switch to the attribute value (single-quoted) state.
            case 0x27: // '
                SwitchState(TokenizerState.AttributeValueSingleQuoted);
                break;
            
            case 0x3E: // >
                // This is a missing-attribute-value parse error.
                // Switch to the data state.
                // Emit the current tag token.
                // TODO: Implement ParseError
                SwitchState(TokenizerState.Data);
                EmitToken<TagToken>();
                break;
            
            // Reconsume in the attribute value (unquoted) state.
            default:
                SwitchState(TokenizerState.AttributeValueUnquoted, reconsume: true);
                break;
        }
    }

    private void CommentStartState(byte currentInputCharacter)
    {
        switch (currentInputCharacter)
        {
            // Switch to the comment start dash state.
            case 0x2D: // -
                SwitchState(TokenizerState.CommentStartDash);
                break;
            
            // This is an abrupt-closing-of-empty-comment parse error.
            // Switch to the date state.
            // Emit the comment token.
            case 0x3E: // >
                SwitchState(TokenizerState.Data);
                EmitToken<CommentToken>();
                break;
            
            // Anything else
            // Reconsume in the comment state.
            default:
                SwitchState(TokenizerState.Comment, reconsume: true);
                break;
        }
    }

    private void CommentState(byte currentInputCharacter)
    {
        switch (currentInputCharacter)
        {
            // Append the current input character to the comment token's data.
            // Switch to the comment less-than sign state.
            case 0x3C: // <
                CurrentToken<CommentToken>().Data.Add(currentInputCharacter);
                SwitchState(TokenizerState.CommentLessThanSign);
                break;
            
            // Switch to the comment end dash state
            case 0x2D: // -
                SwitchState(TokenizerState.CommentEndDash);
                break;
            
            // This is an unexpected-null-character parse error.
            // Append a U+FFFD REPLACEMENT CHARACTER character to the comment token's data.
            case 0x00: // NULL
                CurrentToken<CommentToken>().Data.AddRange(Encoding.UTF8.GetBytes("\uFFFD"));
                break;
            
            // Anything else
            // Append the current input character to the comment token's data.
            default:
                CurrentToken<CommentToken>().Data.Add(currentInputCharacter);
                break;
        }
        
        // TODO: Implement EOF handling
    }

    private void AttributeValueDoubleQuotedState(byte currentInputCharacter)
    {
        switch (currentInputCharacter)
        {
            // Switch to the after attribute value (quoted) state.
            case 0x22: // "
                SwitchState(TokenizerState.AfterAttributeValueQuoted);
                break;
            
            case 0x26: // &
                // Set the return state to the attribute value (double-quoted) state.
                // Switch to the character reference state.
                _returnState = TokenizerState.AttributeValueDoubleQuoted;
                SwitchState(TokenizerState.CharacterReference);
                break;
            
            case 0x00: // NULL
                // This is an unexpected-null-character parse error.
                // Append a U+FFFD REPLACEMENT CHARACTER character to the current attribute value.
                CurrentToken<StartTagToken>().CurrentAttribute.Value += "\uFFFD";
                break;
            
            // Anything else
            // Append the current input character to the current attribute value.
            default:
                CurrentToken<StartTagToken>().CurrentAttribute.Value += Encoding.UTF8.GetString(new [] { currentInputCharacter });
                break;
        }
    }

    private void AfterAttributeValueQuotedState(byte currentInputCharacter)
    {
        switch (currentInputCharacter)
        {
            // Switch to the before attribute name state.
            case 0x09: // \t
            case 0x0A: // \n
            case 0x0C: // \f
            case 0x20: // space
                SwitchState(TokenizerState.BeforeAttributeName);
                break;
            
            // Switch to the self-closing start tag state.
            case 0x2F: // /
                SwitchState(TokenizerState.SelfClosingStartTag);
                break;
            
            // Switch to the data state.
            case 0x3E: // >
                SwitchState(TokenizerState.Data);
                EmitToken<TagToken>();
                break;
            
            // Anything else
            // This is a missing-whitespace-between-attributes parse error.
            // Reconsume in the before attribute name state.
            default:
                SwitchState(TokenizerState.BeforeAttributeName, reconsume: true);
                break;
        }
        
        // TODO: Implement EOF handling
    }


    private void SwitchState(TokenizerState state, bool reconsume = false)
    {
        _currentState = state;
        _reconsume = reconsume;
    }
    
    public void Tokenize()
    {
        Console.WriteLine("Start tokenizing...");
        _currentState = TokenizerState.Data;
        while (!_buffer.IsEndOfBuffer())
        {
            var currentInputCharacter = Consume();

            switch (_currentState)
            {
                case TokenizerState.Data:
                    DataState(currentInputCharacter);
                    break;
                case TokenizerState.CharacterReference:
                    break;
                case TokenizerState.TagOpen:
                    TagOpenState(currentInputCharacter);
                    break;
                case TokenizerState.EndTagOpen:
                    EndTagOpenState(currentInputCharacter);
                    break;
                case TokenizerState.TagName:
                    TagNameState(currentInputCharacter);
                    break;
                case TokenizerState.BeforeAttributeName:
                    BeforeAttributeNameState(currentInputCharacter);
                    break;
                case TokenizerState.DocType:
                    DocTypeState(currentInputCharacter);
                    break;
                case TokenizerState.MarkupDeclarationOpen:
                    MarkupDeclarationOpenState(currentInputCharacter);
                    break;
                case TokenizerState.CommentStart:
                    CommentStartState(currentInputCharacter);
                    break;
                case TokenizerState.BogusComment:
                    break;
                case TokenizerState.SelfClosingStartTag:
                    break;
                case TokenizerState.AfterAttributeName:
                    break;
                case TokenizerState.AttributeName:
                    AttributeNameState(currentInputCharacter);
                    break;
                case TokenizerState.BeforeAttributeValue:
                    BeforeAttributeValueState(currentInputCharacter);
                    break;
                case TokenizerState.CDataSection:
                    break;
                case TokenizerState.BeforeDocTypeName:
                    BeforeDocTypeName(currentInputCharacter);
                    break;
                case TokenizerState.DocTypeName:
                    DocTypeNameState(currentInputCharacter);
                    break;
                case TokenizerState.AttributeValueDoubleQuoted:
                    AttributeValueDoubleQuotedState(currentInputCharacter);
                    break;
                case TokenizerState.AttributeValueSingleQuoted:
                    break;
                case TokenizerState.AttributeValueUnquoted:
                    break;
                case TokenizerState.CommentStartDash:
                    break;
                case TokenizerState.Comment:
                    CommentState(currentInputCharacter);
                    break;
                case TokenizerState.CommentLessThanSign:
                    break;
                case TokenizerState.CommentEndDash:
                    break;
                case TokenizerState.AfterAttributeValueQuoted:
                    AfterAttributeValueQuotedState(currentInputCharacter);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        Console.WriteLine("Tokenizing finished.");
    }
}