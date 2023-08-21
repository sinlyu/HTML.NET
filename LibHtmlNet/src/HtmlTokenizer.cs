using System.Text;
using LibHtmlNet.Tokens;

namespace LibHtmlNet;

public class HtmlTokenizer
{
    private readonly ByteBuffer _buffer;
    private HtmlTokenizerState _currentState = HtmlTokenizerState.Data;
    private HtmlTokenizerState _returnState = HtmlTokenizerState.Data;
    private List<Token> _tokens;
    private readonly Dictionary<Type, Token> _currentTokens;
    
    private bool _reconsume = false;
    
    public HtmlTokenizer(ByteBuffer buffer)
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
    
    private void EmitToken<T>(IEnumerable<byte> data) where T : Token, new()
    {
        // TODO: Remove this if statement
        // If T is CharacterToken, we ignore
        if (typeof(T) == typeof(CharacterToken)) return;
        
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
                SwitchState(state: HtmlTokenizerState.TagOpen);
                break;
            
            // Switch to CharacterReference state if we encounter a 0x26 '&' character
            // Set the return state to Data
            // &
            case 0x26:
                SwitchState(state: HtmlTokenizerState.CharacterReference);
                _returnState = HtmlTokenizerState.Data;
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
                SwitchState(state: HtmlTokenizerState.MarkupDeclarationOpen);
                break;
            
            // Switch to EndTagOpen state if we encounter a 0x2F '/' character
            case 0x2F: // /
                SwitchState(state: HtmlTokenizerState.EndTagOpen);
                break;
            
            // Check if current input character is an ASCII alpha character
            // Create a new start tag token, set its tag name to the empty string. Reconsume in the tag name state.
            case >= 0x41 and <= 0x5A: // A-Z
            case >= 0x61 and <= 0x7A: // a-z
                CurrentToken<StartTagToken>();
                SwitchState(state: HtmlTokenizerState.TagName, reconsume: true);
                break;
            
            // This is an unexpected-question-mark-instead-of-tag-name parse error.
            // Create a comment token whose data is the empty string. Reconsume in the bogus comment state.
            case 0x3F: // ?
                // TODO: Implement ParseError
                EmitToken<CommentToken>();
                SwitchState(state: HtmlTokenizerState.BogusComment, reconsume: true);
                break;
            
            // This is an invalid-first-character-of-tag-name parse error.
            // Emit a U+003C LESS-THAN SIGN character token. Reconsume in the data state.
            default:
                // TODO: Implement ParseError
                EmitToken<CharacterToken>(0x3C);
                SwitchState(state: HtmlTokenizerState.Data, reconsume: true);
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
                SwitchState(state: HtmlTokenizerState.TagName, reconsume: true);
                break;
            
            // This is a missing-end-tag-name parse error.
            // Switch to the data state.
            case 0x3E: // >
                // TODO: Implement ParseError
                SwitchState(state: HtmlTokenizerState.Data);
                break;
            
            // This is an invalid-first-character-of-tag-name parse error.
            // Create a comment token whose data is the empty string.
            // Reconsume in the bogus comment state.
            default:
                // TODO: Implement ParseError
                EmitToken<CommentToken>();
                SwitchState(state: HtmlTokenizerState.BogusComment);
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
                SwitchState(state: HtmlTokenizerState.BeforeAttributeName);
                break;
            
            // Switch to the self-closing start tag state.
            case 0x2F: // /
                SwitchState(state: HtmlTokenizerState.SelfClosingStartTag);
                break;
            
            // Switch to the data state. Emit the current tag token.
            case 0x3E: // >
                EmitToken<TagToken>();
                SwitchState(state: HtmlTokenizerState.Data);
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
                SwitchState(state: HtmlTokenizerState.AfterAttributeName, reconsume: true);
                break;
            
            // This is an unexpected-equals-sign-before-attribute-name parse error.
            // Start a new attribute in the current tag token. Set that attribute's name to the current input character,
            // and its value to the empty string. Switch to the attribute name state.
            case 0x3D: // =
                // TODO: Implement ParseError
                CurrentToken<StartTagToken>().StartNewAttribute(Encoding.UTF8.GetString(new [] { currentInputCharacter }));
                SwitchState(HtmlTokenizerState.AttributeName);
                break;
            
            // Anything else
            // Start a new attribute in the current tag token.
            // Set that attribute's name and value to the empty string.
            default:
                CurrentToken<StartTagToken>().StartNewAttribute();
                SwitchState(HtmlTokenizerState.AttributeName, reconsume: true);
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
                SwitchState(HtmlTokenizerState.AfterAttributeName, reconsume: true);
                break;
            
            // Switch to the before attribute value state.
            case 0x3D: // =
                SwitchState(HtmlTokenizerState.BeforeAttributeValue);
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
            SwitchState(HtmlTokenizerState.CommentStart);
        }
        
        // If the next seven characters are an ASCII case-insensitive match for the word "DOCTYPE"
        // Consume those characters and switch to the DOCTYPE state.
        else if (_buffer.MatchCaseInsensitiveString("DOCTYPE"))
        {
            Consume(7);
            SwitchState(HtmlTokenizerState.DocType);
        }
        
        // If the next seven characters are an ASCII case-insensitive match for the string "[CDATA["
        // Consume those characters and switch to the CDATA section state.
        else if (_buffer.MatchCaseInsensitiveString("[CDATA["))
        {
            // TODO: Implement check for adjusted current node being an element in the HTML namespace
            // TODO: Implement ParseError
            Consume(7);
            SwitchState(HtmlTokenizerState.CDataSection);
        }

        // This is an incorrectly-opened-comment parse error.
        // Create a comment token whose data is the empty string.
        // Switch to the bogus comment state (don't consume anything in the current state).
        else
        {
            // TODO: Implement ParseError
            CurrentToken<CommentToken>();
            SwitchState(HtmlTokenizerState.BogusComment);
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
                SwitchState(HtmlTokenizerState.BeforeDocTypeName);
                break;
            
            // Reconsume in the before DOCTYPE name state.
            case 0x3E: // >
                SwitchState(HtmlTokenizerState.BeforeDocTypeName, reconsume: true);
                break;
            
            // Anything else
            // This is a missing-whitespace-before-doctype-name parse error.
            // Reconsume in the before DOCTYPE name state.
            default:
                // TODO: Implement ParseError
                SwitchState(HtmlTokenizerState.BeforeDocTypeName, reconsume: true);
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
                SwitchState(HtmlTokenizerState.DocTypeName);
                break;
            
            // This is an unexpected-null-character parse error.
            // Create a new DOCTYPE token. Set its name to a U+FFFD REPLACEMENT CHARACTER character.
            // Switch to the DOCTYPE name state.
            case 0x00: // NULL
                CurrentToken<DocTypeToken>().ForceQuirks = true;
                EmitToken<DocTypeToken>();
                SwitchState(HtmlTokenizerState.Data);
                break;
            
            // This is an missing-doctype-name parse error.
            // Create a new DOCTYPE token. Set its force-quirks flag to on.
            // Emit the token.
            // Switch to the data state.
            case 0x3E: // >
                CurrentToken<DocTypeToken>().ForceQuirks = true;
                EmitToken<DocTypeToken>();
                SwitchState(HtmlTokenizerState.Data);
                break;
            
            // Anything else
            // Create a new DOCTYPE token. Set its name to the current input character.
            // Switch to the DOCTYPE name state.
            default:
                CurrentToken<DocTypeToken>().Name = Encoding.UTF8.GetString(new [] { currentInputCharacter });
                SwitchState(HtmlTokenizerState.DocTypeName);
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
                SwitchState(HtmlTokenizerState.Data);
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
                SwitchState(HtmlTokenizerState.AttributeValueDoubleQuoted);
                break;
            
            // Switch to the attribute value (single-quoted) state.
            case 0x27: // '
                SwitchState(HtmlTokenizerState.AttributeValueSingleQuoted);
                break;
            
            case 0x3E: // >
                // This is a missing-attribute-value parse error.
                // Switch to the data state.
                // Emit the current tag token.
                // TODO: Implement ParseError
                SwitchState(HtmlTokenizerState.Data);
                EmitToken<TagToken>();
                break;
            
            // Reconsume in the attribute value (unquoted) state.
            default:
                SwitchState(HtmlTokenizerState.AttributeValueUnquoted, reconsume: true);
                break;
        }
    }

    private void CommentStartState(byte currentInputCharacter)
    {
        switch (currentInputCharacter)
        {
            // Switch to the comment start dash state.
            case 0x2D: // -
                SwitchState(HtmlTokenizerState.CommentStartDash);
                break;
            
            // This is an abrupt-closing-of-empty-comment parse error.
            // Switch to the date state.
            // Emit the comment token.
            case 0x3E: // >
                SwitchState(HtmlTokenizerState.Data);
                EmitToken<CommentToken>();
                break;
            
            // Anything else
            // Reconsume in the comment state.
            default:
                SwitchState(HtmlTokenizerState.Comment, reconsume: true);
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
                SwitchState(HtmlTokenizerState.CommentLessThanSign);
                break;
            
            // Switch to the comment end dash state
            case 0x2D: // -
                SwitchState(HtmlTokenizerState.CommentEndDash);
                break;
            
            // This is an unexpected-null-character parse error.
            // Append a U+FFFD REPLACEMENT CHARACTER character to the comment token's data.
            case 0x00: // NULL
                CurrentToken<CommentToken>().Data.AddRange("\uFFFD"u8.ToArray());
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
                SwitchState(HtmlTokenizerState.AfterAttributeValueQuoted);
                break;
            
            case 0x26: // &
                // Set the return state to the attribute value (double-quoted) state.
                // Switch to the character reference state.
                _returnState = HtmlTokenizerState.AttributeValueDoubleQuoted;
                SwitchState(HtmlTokenizerState.CharacterReference);
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
                SwitchState(HtmlTokenizerState.BeforeAttributeName);
                break;
            
            // Switch to the self-closing start tag state.
            case 0x2F: // /
                SwitchState(HtmlTokenizerState.SelfClosingStartTag);
                break;
            
            // Switch to the data state.
            case 0x3E: // >
                SwitchState(HtmlTokenizerState.Data);
                EmitToken<TagToken>();
                break;
            
            // Anything else
            // This is a missing-whitespace-between-attributes parse error.
            // Reconsume in the before attribute name state.
            default:
                SwitchState(HtmlTokenizerState.BeforeAttributeName, reconsume: true);
                break;
        }
        
        // TODO: Implement EOF handling
    }

    // 13.2.5.50 Comment end dash state
    // https://html.spec.whatwg.org/multipage/parsing.html#comment-end-dash-state
    private void CommentEndDashState(byte currentInputCharacter)
    {
        switch (currentInputCharacter)
        {
            // Switch to the comment end state.
            case 0x2D: // -
                SwitchState(HtmlTokenizerState.CommentEnd);
                break;
            
            // Anything else
            // Append a U+002D HYPHEN-MINUS character (-) to the comment token's data.
            // Reconsume in the comment state.
            default:
                CurrentToken<CommentToken>().Data.Add(0x2D);
                SwitchState(HtmlTokenizerState.Comment, reconsume: true);
                break;
        }
        
        // TODO: Implement EOF handling
    }
    
    // 13.2.5.51 Comment end state
    // https://html.spec.whatwg.org/multipage/parsing.html#comment-end-state
    private void CommentEndState(byte currentInputCharacter)
    {
        switch (currentInputCharacter)
        {
            // Switch to the data state.
            // Emit the current comment token.
            case 0x3E: // >
                SwitchState(HtmlTokenizerState.Data);
                EmitToken<CommentToken>(CurrentToken<CommentToken>().Data);
                break;
            
            // Switch to the comment end bang state.
            case 0x21: // !
                SwitchState(HtmlTokenizerState.CommentEndBang);
                break;
            
            // Append a U+002D HYPHEN-MINUS character (-) to the comment token's data.
            case 0x2D: // -
                CurrentToken<CommentToken>().Data.Add(0x2D);
                break;
            
            // Append two U+002D HYPHEN-MINUS characters (-) to the comment token's data.
            // Reconsume in the comment state.
            default:
                CurrentToken<CommentToken>().Data.AddRange(new byte[] { 0x2D, 0x2D });
                SwitchState(HtmlTokenizerState.Comment, reconsume: true);
                break;
        }
    }

    // 13.2.5.52 Comment end bang state
    // https://html.spec.whatwg.org/multipage/parsing.html#comment-end-bang-state
    private void CommentEndBangState(byte currentInputCharacter)
    {
        switch (currentInputCharacter)
        {
            // Append two U+002D HYPHEN-MINUS characters (-) and a U+0021 EXCLAMATION MARK character (!) to the comment token's data.
            // Switch to the comment end dash state.
            case 0x2D: // -
                CurrentToken<CommentToken>().Data.AddRange(new byte[] { 0x2D, 0x2D, 0x21 });
                SwitchState(HtmlTokenizerState.CommentEndDash);
                break;
            
            // This is an incorrectly-closed-comment parse error.
            // Switch to the data state.
            // Emit the current comment token.
            case 0x3E: // >
                // TODO: Implement ParseError
                SwitchState(HtmlTokenizerState.Data);
                EmitToken<CommentToken>();
                break;
            
            // Anything else
            // Append two U+002D HYPHEN-MINUS characters (-) and a U+0021 EXCLAMATION MARK character (!) to the comment token's data.
            // Reconsume in the comment state. 
            default:
                CurrentToken<CommentToken>().Data.AddRange(new byte[] { 0x2D, 0x2D, 0x21 });
                SwitchState(HtmlTokenizerState.Comment, reconsume: true);
                break;
        }
        
        // TODO: Implement EOF handling
    }

    // 13.2.5.44 Comment start dash state
    // https://html.spec.whatwg.org/multipage/parsing.html#comment-start-dash-state
    private void CommentStartDashState(byte currentInputCharacter)
    {
        switch (currentInputCharacter)
        {
            // Switch to the comment end state.
            case 0x2D: // -
                SwitchState(HtmlTokenizerState.CommentEnd);
                break;
            
            // This is an abrupt-closing-of-empty-comment parse error.
            // Switch to the data state.
            // Emit the current comment token.
            case 0x3E: // >
                // TODO: Implement ParseError
                SwitchState(HtmlTokenizerState.Data);
                EmitToken<CommentToken>();
                break;
            
            // Append a U+002D HYPHEN-MINUS character (-) to the comment token's data.
            // Reconsume in the comment state.
            default:
                CurrentToken<CommentToken>().Data.Add(0x2D);
                SwitchState(HtmlTokenizerState.Comment, reconsume: true);
                break;
        }
    }

    // 13.2.5.41 Bogus comment state
    // https://html.spec.whatwg.org/multipage/parsing.html#bogus-comment-state
    private void BogusCommentState(byte currentInputCharacter)
    {
        switch (currentInputCharacter)
        {
            // Switch to the data state.
            // Emit the current comment token.
            case 0x3E: // >
                SwitchState(HtmlTokenizerState.Data);
                EmitToken<CommentToken>();
                break;
            
            // This is an unexpected-null-character parse error.
            // Append a U+FFFD REPLACEMENT CHARACTER character to the comment token's data.
            case 0x00: // NULL
                // TODO: Implement ParseError
                CurrentToken<CommentToken>().Data.AddRange("\uFFFD"u8.ToArray());
                break;
            
            // Anything else
            // Append the current input character to the comment token's data.
            default:
                CurrentToken<CommentToken>().Data.Add(currentInputCharacter);
                break;
        }
        
        // TODO: Implement EOF handling
    }

    // 13.2.5.46 Comment less-than sign state
    // https://html.spec.whatwg.org/multipage/parsing.html#comment-less-than-sign-state
    private void CommentLessThanSignState(byte currentInputCharacter)
    {
        switch (currentInputCharacter)
        {
            // Append the current input character to the comment token's data.
            // Switch to the comment less-than sign bang state.
            case 0x21: // !
                CurrentToken<CommentToken>().Data.Add(currentInputCharacter);
                SwitchState(HtmlTokenizerState.CommentLessThanSignBang);
                break;
            
            // Append the current input character to the comment token's data.
            case 0x3C:
                CurrentToken<CommentToken>().Data.Add(currentInputCharacter);
                break;
            
            // Anything else
            // Reconsume in the comment state.
            default:
                SwitchState(HtmlTokenizerState.Comment, reconsume: true);
                break;
        }
    }


    // 13.2.5.47 Comment less-than sign bang state
    // https://html.spec.whatwg.org/multipage/parsing.html#comment-less-than-sign-bang-state
    private void CommentLessThanSignBangState(byte currentInputCharacter)
    {
        switch (currentInputCharacter)
        {
            // Switch to the comment less-than sign bang dash dash state.
            case 0x2D: // -
                SwitchState(HtmlTokenizerState.CommentLessThanSignBangDashDash);
                break;
            
            // Anything else
            // Reconsume in the comment end dash state.
            default:
                SwitchState(HtmlTokenizerState.CommentEndDash, reconsume: true);
                break;
        }
    }
    
    // 13.2.5.49 Comment less-than sign bang dash dash state
    // https://html.spec.whatwg.org/multipage/parsing.html#comment-less-than-sign-bang-dash-dash-state
    private void CommentLessThanSignBangDashDashState(byte currentInputCharacter)
    {
        switch (currentInputCharacter)
        {
            // Reconsume in the comment end state.
            case 0x3E: // <
                SwitchState(HtmlTokenizerState.CommentEnd, reconsume: true);
                break;
            
            // Anything else
            // This is a nested-comment parse error.
            // Reconsume in the comment end state.
            default:
                // TODO: Implement ParseError
                SwitchState(HtmlTokenizerState.CommentEnd, reconsume: true);
                break;
        }
        
        // TODO: Implement EOF handling
    }

    // 13.2.5.40 Self-closing start tag state
    // https://html.spec.whatwg.org/multipage/parsing.html#self-closing-start-tag-state
    private void SelfClosingStartTagState(byte currentInputCharacter)
    {
        switch (currentInputCharacter)
        {
            // Set the self-closing flag of the current tag token.
            // Switch to the data state.
            // Emit the current tag token.
            case 0x3E: // >
                CurrentToken<StartTagToken>().SelfClosing = true;
                SwitchState(HtmlTokenizerState.Data);
                EmitToken<TagToken>();
                break;
            
            // Anything else
            // This is an unexpected-solidus-in-tag parse error.
            // Reconsume in the before attribute name state.
            default:
                // TODO: Implement ParseError
                SwitchState(HtmlTokenizerState.BeforeAttributeName, reconsume: true);
                break;
        }
    }

    private void SwitchState(HtmlTokenizerState state, bool reconsume = false)
    {
        _currentState = state;
        _reconsume = reconsume;
    }
    
    public void Tokenize()
    {
        Console.WriteLine("Start tokenizing...");
        _currentState = HtmlTokenizerState.Data;
        while (!_buffer.IsEndOfBuffer())
        {
            var currentInputCharacter = Consume();

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

        Console.WriteLine("Tokenizing finished.");
    }
}