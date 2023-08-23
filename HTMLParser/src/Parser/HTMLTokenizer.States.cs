using System.Text;
using HTML_NET.Parser.Tokens;

namespace HTML_NET.Parser;

public partial class HTMLTokenizer
{
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
        CommentLessThanSignBangDashDash,
        NamedCharacterReference,
        NumericCharacterReference,
        AmbiguousAmpersand
    }

    // 13.2.5.1 Data state
    // https://html.spec.whatwg.org/multipage/parsing.html#data-state
    private void DataState(char currentInputCharacter)
    {
        switch (currentInputCharacter)
        {
            // Switch to TagOpen state if we encounter a 0x3C '<' character
            // <
            case '<':
                SwitchState(HtmlTokenizerState.TagOpen);
                break;

            // Switch to CharacterReference state if we encounter a 0x26 '&' character
            // Set the return state to Data
            // &
            case '&':
                SwitchState(HtmlTokenizerState.CharacterReference);
                _returnState = HtmlTokenizerState.Data;
                break;

            // Emit the current input character as a character token
            case '\0':
                var token = CurrentToken<CharacterToken>();
                LogParseError("unexpected-null-character", token);

                token.Data.Clear();
                token.Data.Append(currentInputCharacter);

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

    // 13.2.5.6 Tag open state
    // https://html.spec.whatwg.org/multipage/parsing.html#tag-open-state
    private void TagOpenState(char currentInputCharacter)
    {
        switch (currentInputCharacter)
        {
            // Switch to MarkupDeclaration state if we encounter a 0x21 '!' character
            case '!': // !
                SwitchState(HtmlTokenizerState.MarkupDeclarationOpen);
                break;

            // Switch to EndTagOpen state if we encounter a 0x2F '/' character
            case '/': // /
                SwitchState(HtmlTokenizerState.EndTagOpen);
                break;

            // Check if current input character is an ASCII alpha character
            // Create a new start tag token, set its tag name to the empty string. Reconsume in the tag name state.
            case >= 'A' and <= 'Z': // A-Z
            case >= 'a' and <= 'z': // a-z
                CurrentToken<StartTagToken>();
                SwitchState(HtmlTokenizerState.TagName, true);
                break;

            // This is an unexpected-question-mark-instead-of-tag-name parse error.
            // Create a comment token whose data is the empty string. Reconsume in the bogus comment state.
            case '?': // ?
                LogParseError("unexpected-question-mark-instead-of-tag-name", CurrentToken<CommentToken>());
                EmitToken<CommentToken>();
                SwitchState(HtmlTokenizerState.BogusComment, true);
                break;

            // This is an invalid-first-character-of-tag-name parse error.
            // Emit a U+003C LESS-THAN SIGN character token. Reconsume in the data state.
            default:
                LogParseError("invalid-first-character-of-tag-name", CurrentToken<CharacterToken>());
                EmitToken<CharacterToken>('<');
                SwitchState(HtmlTokenizerState.Data, true);
                break;
        }

        // TODO: Implement EOF handling
    }

    // 13.2.5.7 End tag open state
    // https://html.spec.whatwg.org/multipage/parsing.html#end-tag-open-state
    private void EndTagOpenState(char currentInputCharacter)
    {
        switch (currentInputCharacter)
        {
            // Create a new end tag token, set its tag name to the empty string.
            // Reconsume in the tag name state.
            case >= 'A' and <= 'Z': // A-Z
            case >= 'a' and <= 'z': // a-z
                CurrentToken<EndTagToken>();
                SwitchState(HtmlTokenizerState.TagName, true);
                break;

            // This is a missing-end-tag-name parse error.
            // Switch to the data state.
            case '>': // >
                LogParseError("missing-end-tag-name", CurrentToken<CharacterToken>());
                SwitchState(HtmlTokenizerState.Data);
                break;

            // This is an invalid-first-character-of-tag-name parse error.
            // Create a comment token whose data is the empty string.
            // Reconsume in the bogus comment state.
            default:
                LogParseError("invalid-first-character-of-tag-name", CurrentToken<CommentToken>());
                EmitToken<CommentToken>();
                SwitchState(HtmlTokenizerState.BogusComment);
                break;
        }

        // TODO: Implement EOF handling
    }

    // 13.2.5.8 Tag name state
    // https://html.spec.whatwg.org/multipage/parsing.html#tag-name-state
    private void TagNameState(char currentInputCharacter)
    {
        switch (currentInputCharacter)
        {
            // Switch to the before attribute name state.
            case '\t': // \t
            case '\n': // \n
            case '\f': // \f
            case ' ': // space
                SwitchState(HtmlTokenizerState.BeforeAttributeName);
                break;

            // Switch to the self-closing start tag state.
            case '/': // /
                SwitchState(HtmlTokenizerState.SelfClosingStartTag);
                break;

            // Switch to the data state. Emit the current tag token.
            case '>': // >
                EmitToken<TagToken>();
                SwitchState(HtmlTokenizerState.Data);
                break;

            // ASCII upper alpha character
            // Append the lowercase version of the current input character (add 0x0020 to the character's code point) to the current tag token's tag name.
            case >= 'A' and <= 'Z': // A-Z
                CurrentToken<TagToken>().TagName += currentInputCharacter + 0x20;
                break;

            // Anything else
            // Append the current input character to the current tag token's tag name.
            default:
                CurrentToken<TagToken>().TagName += currentInputCharacter;
                break;
        }

        // TODO: Implement EOF handling
    }

    // 13.2.5.32 Before attribute name state
    // https://html.spec.whatwg.org/multipage/parsing.html#before-attribute-name-state
    private void BeforeAttributeNameState(char currentInputCharacter)
    {
        switch (currentInputCharacter)
        {
            // Ignore the character.
            case '\t': // \t
            case '\n': // \n
            case '\f': // \f
            case ' ': // space
                break;

            // Reconsume in the after attribute name state.
            case '/': // /
            case '>': // >
                SwitchState(HtmlTokenizerState.AfterAttributeName, true);
                break;

            // This is an unexpected-equals-sign-before-attribute-name parse error.
            // Start a new attribute in the current tag token. Set that attribute's name to the current input character,
            // and its value to the empty string. Switch to the attribute name state.
            case '=': // =
                LogParseError("unexpected-equals-sign-before-attribute-name", CurrentToken<CharacterToken>());
                CurrentToken<StartTagToken>().NewAttribute(currentInputCharacter);
                SwitchState(HtmlTokenizerState.AttributeName);
                break;

            // Anything else
            // Start a new attribute in the current tag token.
            // Set that attribute's name and value to the empty string.
            default:
                CurrentToken<StartTagToken>().NewAttribute();
                SwitchState(HtmlTokenizerState.AttributeName, true);
                break;
        }

        // TODO: Implement EOF handling
    }

    // 13.2.5.33 Attribute name state
    // https://html.spec.whatwg.org/multipage/parsing.html#attribute-name-state
    private void AttributeNameState(char currentInputCharacter)
    {
        switch (currentInputCharacter)
        {
            // Reconsume in the after attribute name state.
            case '\t': // \t
            case '\n': // \n
            case '\f': // \f
            case ' ': // space
            case '/': // /
            case '>': // >
                SwitchState(HtmlTokenizerState.AfterAttributeName, true);
                break;

            // Switch to the before attribute value state.
            case '=': // =
                SwitchState(HtmlTokenizerState.BeforeAttributeValue);
                break;

            // ASCII upper alpha character
            // Append the lowercase version of the current input character (add 0x0020 to the character's code point) to the current attribute's name.
            case >= 'A' and <= 'Z': // A-Z
                CurrentToken<StartTagToken>().AddAttributeName((char)(currentInputCharacter + 0x20));
                break;

            // This is an unexpected-null-character parse error.
            // Append a U+FFFD REPLACEMENT CHARACTER character to the current attribute's name.
            case '\0': // NULL
                CurrentToken<StartTagToken>().AddAttributeName("\uFFFD");
                break;

            // This is an unexpected-character-in-attribute-name parse error.
            // Treat it as per the "anything else" entry below.
            case '"': // "
            case '\'': // '
            case '<': // <
                LogParseError("unexpected-character-in-attribute-name", CurrentToken<CharacterToken>());
                break;
            // https://html.spec.whatwg.org/multipage/parsing.html#attribute-name-state
            default:
                CurrentToken<StartTagToken>().AddAttributeName(currentInputCharacter);
                break;
        }
    }

    // 13.2.5.42 Markup declaration open state
    // https://html.spec.whatwg.org/multipage/parsing.html#markup-declaration-open-state
    private void MarkupDeclarationOpenState(char currentInputCharacter)
    {
        // If the next two characters are both U+002D (-) characters
        // Consume those two characters, create a comment token whose data is the empty string, and switch to the comment start state.
        if (currentInputCharacter == '-' && _buffer.PeekByte() == '-')
        {
            Skip(1);
            CurrentToken<CommentToken>();
            SwitchState(HtmlTokenizerState.CommentStart);
        }

        // If the next seven characters are an ASCII case-insensitive match for the word "DOCTYPE"
        // Consume those characters and switch to the DOCTYPE state.
        else if (_buffer.MatchCaseInsensitiveString("DOCTYPE"))
        {
            Skip(7);
            SwitchState(HtmlTokenizerState.DocType);
        }

        // If the next seven characters are an ASCII case-insensitive match for the string "[CDATA["
        // Consume those characters and switch to the CDATA section state.
        else if (_buffer.MatchCaseInsensitiveString("[CDATA["))
        {
            // TODO: Implement check for adjusted current node being an element in the HTML namespace
            // TODO: Implement ParseError
            Skip(7);
            SwitchState(HtmlTokenizerState.CDataSection);
        }

        // This is an incorrectly-opened-comment parse error.
        // Create a comment token whose data is the empty string.
        // Switch to the bogus comment state (don't consume anything in the current state).
        else
        {
            LogParseError("incorrectly-opened-comment", CurrentToken<CommentToken>());
            CurrentToken<CommentToken>();
            SwitchState(HtmlTokenizerState.BogusComment);
        }
    }

    // 13.2.5.53 DOCTYPE state
    // https://html.spec.whatwg.org/multipage/parsing.html#doctype-state
    private void DocTypeState(char currentInputCharacter)
    {
        switch (currentInputCharacter)
        {
            // Switch to the before DOCTYPE name state.
            case '\t': // \t
            case '\n': // \n
            case '\f': // \f
            case ' ': // space
                SwitchState(HtmlTokenizerState.BeforeDocTypeName);
                break;

            // Reconsume in the before DOCTYPE name state.
            case '>': // >
                SwitchState(HtmlTokenizerState.BeforeDocTypeName, true);
                break;

            // Anything else
            // This is a missing-whitespace-before-doctype-name parse error.
            // Reconsume in the before DOCTYPE name state.
            default:
                LogParseError("missing-whitespace-before-doctype-name", CurrentToken<CharacterToken>());
                SwitchState(HtmlTokenizerState.BeforeDocTypeName, true);
                break;
        }

        // TODO: Implement EOF handling
    }

    // 13.2.5.54 Before DOCTYPE name state
    // https://html.spec.whatwg.org/multipage/parsing.html#before-doctype-name-state
    private void BeforeDocTypeName(char currentInputCharacter)
    {
        switch (currentInputCharacter)
        {
            // Ignore the character.
            case '\t': // \t
            case '\n': // \n
            case '\f': // \f
            case ' ': // space
                break;

            // ASCII upper alpha character
            // Create a new DOCTYPE token. Set its name to the lowercase version of the current input character (add 0x0020 to the character's code point).
            // Switch to the DOCTYPE name state.
            case >= 'A' and <= 'Z': // A-Z
                CurrentToken<DOCTYPEToken>().Name =
                    Encoding.UTF8.GetString(new[] { (byte)(currentInputCharacter + 0x20) });
                SwitchState(HtmlTokenizerState.DocTypeName);
                break;

            // This is an unexpected-null-character parse error.
            // Create a new DOCTYPE token. Set its name to a U+FFFD REPLACEMENT CHARACTER character.
            // Switch to the DOCTYPE name state.
            case '\0': // NULL
                CurrentToken<DOCTYPEToken>().ForceQuirks = true;
                EmitToken<DOCTYPEToken>();
                SwitchState(HtmlTokenizerState.Data);
                break;

            // This is an missing-doctype-name parse error.
            // Create a new DOCTYPE token. Set its force-quirks flag to on.
            // Emit the token.
            // Switch to the data state.
            case '>': // >
                CurrentToken<DOCTYPEToken>().ForceQuirks = true;
                EmitToken<DOCTYPEToken>();
                SwitchState(HtmlTokenizerState.Data);
                break;

            // Anything else
            // Create a new DOCTYPE token. Set its name to the current input character.
            // Switch to the DOCTYPE name state.
            default:
                CurrentToken<DOCTYPEToken>().Name = currentInputCharacter.ToString();
                SwitchState(HtmlTokenizerState.DocTypeName);
                break;
        }
    }

    // 13.2.5.55 DOCTYPE name state
    // https://html.spec.whatwg.org/multipage/parsing.html#doctype-name-state
    private void DocTypeNameState(char currentInputCharacter)
    {
        switch (currentInputCharacter)
        {
            // Ignore the character.
            case '\t': // \t
            case '\n': // \n
            case '\f': // \f
            case ' ': // space
                break;

            // Switch to the data state.
            // Emit the current DOCTYPE token.
            case '>': // >
                EmitToken<DOCTYPEToken>();
                SwitchState(HtmlTokenizerState.Data);
                break;

            // ASCII upper alpha character
            // Append the lowercase version of the current input character (add 0x0020 to the character's code point) to the current DOCTYPE token's name.
            case >= 'A' and <= 'Z': // A-Z
                CurrentToken<DOCTYPEToken>().Name += (char)(currentInputCharacter + 0x20);
                break;

            // This is an unexpected-null-character parse error.
            // Append a U+FFFD REPLACEMENT CHARACTER character to the current DOCTYPE token's name.
            case '\0': // NULL
                LogParseError("unexpected-null-character", CurrentToken<CharacterToken>());
                CurrentToken<DOCTYPEToken>().Name += "\uFFFD";
                break;

            // Anything else
            // Append the current input character to the current DOCTYPE token's name.
            default:
                CurrentToken<DOCTYPEToken>().Name += currentInputCharacter;
                break;
        }

        // TODO: Implement EOF handling
    }

    // 13.2.5.35 Before attribute value state
    // https://html.spec.whatwg.org/multipage/parsing.html#before-attribute-value-state
    private void BeforeAttributeValueState(char currentInputCharacter)
    {
        switch (currentInputCharacter)
        {
            // Ignore the character.
            case '\t': // \t
            case '\n': // \n
            case '\f': // \f
            case ' ': // space
                break;

            // Switch to the attribute value (double-quoted) state.
            case '"': // "
                SwitchState(HtmlTokenizerState.AttributeValueDoubleQuoted);
                break;

            // Switch to the attribute value (single-quoted) state.
            case '\'': // '
                SwitchState(HtmlTokenizerState.AttributeValueSingleQuoted);
                break;

            case '>': // >
                // This is a missing-attribute-value parse error.
                // Switch to the data state.
                // Emit the current tag token.
                LogParseError("missing-attribute-value", CurrentToken<CharacterToken>());
                SwitchState(HtmlTokenizerState.Data);
                EmitToken<TagToken>();
                break;

            // Reconsume in the attribute value (unquoted) state.
            default:
                SwitchState(HtmlTokenizerState.AttributeValueUnquoted, true);
                break;
        }
    }

    // 13.2.5.43 Comment start state
    // https://html.spec.whatwg.org/multipage/parsing.html#comment-start-state
    private void CommentStartState(char currentInputCharacter)
    {
        switch (currentInputCharacter)
        {
            // Switch to the comment start dash state.
            case '-': // -
                SwitchState(HtmlTokenizerState.CommentStartDash);
                break;

            // This is an abrupt-closing-of-empty-comment parse error.
            // Switch to the date state.
            // Emit the comment token.
            case '>': // >
                SwitchState(HtmlTokenizerState.Data);
                EmitToken<CommentToken>();
                break;

            // Anything else
            // Reconsume in the comment state.
            default:
                SwitchState(HtmlTokenizerState.Comment, true);
                break;
        }
    }

    // 13.2.5.45 Comment state
    // https://html.spec.whatwg.org/multipage/parsing.html#comment-state
    private void CommentState(char currentInputCharacter)
    {
        switch (currentInputCharacter)
        {
            // Append the current input character to the comment token's data.
            // Switch to the comment less-than sign state.
            case '<': // <
                CurrentToken<CommentToken>().Data.Append(currentInputCharacter);
                SwitchState(HtmlTokenizerState.CommentLessThanSign);
                break;

            // Switch to the comment end dash state
            case '-': // -
                SwitchState(HtmlTokenizerState.CommentEndDash);
                break;

            // This is an unexpected-null-character parse error.
            // Append a U+FFFD REPLACEMENT CHARACTER character to the comment token's data.
            case '\0': // NULL
                CurrentToken<CommentToken>().Data.Append('\uFFFD');
                break;

            // Anything else
            // Append the current input character to the comment token's data.
            default:
                CurrentToken<CommentToken>().Data.Append(currentInputCharacter);
                break;
        }

        // TODO: Implement EOF handling
    }

    private void AttributeValueUnquotedState(char currentInputCharacter)
    {
        switch (currentInputCharacter)
        {
            // Switch to the before attribute name state.
            case '\t': // \t
            case '\n': // \n
            case '\f': // \f
            case ' ': // space
                SwitchState(HtmlTokenizerState.BeforeAttributeName);
                break;
            
            // Set the return state to the attribute value (unquoted) state.
            // Switch to the character reference state.
            case '&':
                _returnState = HtmlTokenizerState.AttributeValueUnquoted;
                SwitchState(HtmlTokenizerState.CharacterReference);
                break;
            
            // Switch to the data state.
            // Emit the current tag token.
            case '>':
                SwitchState(HtmlTokenizerState.Data);
                EmitToken<TagToken>();
                break;
            
            // This is an unexpected-null-character parse error.
            // Append a U+FFFD REPLACEMENT CHARACTER character to the current attribute's value.
            case '\0':
                LogParseError("unexpected-null-character", CurrentToken<CharacterToken>());
                CurrentToken<StartTagToken>().AddAttributeValue("\uFFFD");
                break;
            
            // This is an unexpected-character-in-unquoted-attribute-value parse error.
            // Treat it as per the "anything else" entry below.
            // Append the current input character to the current attribute's value.
            case '"':
            case '\'':
            case '<':
            case '=':
            case '`':
                LogParseError("unexpected-character-in-unquoted-attribute-value", CurrentToken<CharacterToken>());
                CurrentToken<StartTagToken>().AddAttributeValue(currentInputCharacter);
                break;
            
            // Anything else
            // Append the current input character to the current attribute's value.
            default:
                CurrentToken<StartTagToken>().AddAttributeValue(currentInputCharacter);
                break;
        }
        
        // TODO: Implement EOF handling
    }

    // 13.2.5.37 Attribute value (single-quoted) state
    // https://html.spec.whatwg.org/multipage/parsing.html#attribute-value-(single-quoted)-state
    private void AttributeValueSingleQuotedState(char currentInputCharacter)
    {
        switch (currentInputCharacter)
        {
            // Switch to the after attribute value (quoted) state.
            case '\'':
                SwitchState(HtmlTokenizerState.AfterAttributeValueQuoted);
                break;

            // Set the return state to the attribute value (single-quoted) state.
            // Switch to the character reference state.
            case '&':
                _returnState = HtmlTokenizerState.AttributeValueSingleQuoted;
                SwitchState(HtmlTokenizerState.CharacterReference);
                break;

            // This is an unexpected-null-character parse error.
            // Append a U+FFFD REPLACEMENT CHARACTER character to the current attribute's value.
            case '\0':
                LogParseError("unexpected-null-character", CurrentToken<CharacterToken>());
                CurrentToken<StartTagToken>().AddAttributeValue("\uFFFD");
                break;

            // Anything else
            // Append the current input character to the current attribute's value.
            default:
                CurrentToken<StartTagToken>().AddAttributeValue(currentInputCharacter);
                break;
        }
    }

    // 13.2.5.36 Attribute value (double-quoted) state 
    // https://html.spec.whatwg.org/multipage/parsing.html#after-attribute-value-(quoted)-state
    private void AttributeValueDoubleQuotedState(char currentInputCharacter)
    {
        switch (currentInputCharacter)
        {
            // Switch to the after attribute value (quoted) state.
            case '"': // "
                SwitchState(HtmlTokenizerState.AfterAttributeValueQuoted);
                break;

            case '&': // &
                // Set the return state to the attribute value (double-quoted) state.
                // Switch to the character reference state.
                _returnState = HtmlTokenizerState.AttributeValueDoubleQuoted;
                SwitchState(HtmlTokenizerState.CharacterReference);
                break;

            case '\0': // NULL
                // This is an unexpected-null-character parse error.
                // Append a U+FFFD REPLACEMENT CHARACTER character to the current attribute value.
                CurrentToken<StartTagToken>().AddAttributeValue("\uFFFD");
                break;

            // Anything else
            // Append the current input character to the current attribute value.
            default:
                CurrentToken<StartTagToken>().AddAttributeValue(currentInputCharacter);
                break;
        }
    }

    // 13.2.5.39 After attribute value (quoted) state
    // https://html.spec.whatwg.org/multipage/parsing.html#after-attribute-value-(quoted)-state
    private void AfterAttributeValueQuotedState(char currentInputCharacter)
    {
        switch (currentInputCharacter)
        {
            // Switch to the before attribute name state.
            case '\t': // \t
            case '\n': // \n
            case '\f': // \f
            case ' ': // space
                SwitchState(HtmlTokenizerState.BeforeAttributeName);
                break;

            // Switch to the self-closing start tag state.
            case '/': // /
                SwitchState(HtmlTokenizerState.SelfClosingStartTag);
                break;

            // Switch to the data state.
            case '>': // >
                SwitchState(HtmlTokenizerState.Data);
                EmitToken<TagToken>();
                break;

            // Anything else
            // This is a missing-whitespace-between-attributes parse error.
            // Reconsume in the before attribute name state.
            default:
                SwitchState(HtmlTokenizerState.BeforeAttributeName, true);
                break;
        }

        // TODO: Implement EOF handling
    }

    // 13.2.5.50 Comment end dash state
    // https://html.spec.whatwg.org/multipage/parsing.html#comment-end-dash-state
    private void CommentEndDashState(char currentInputCharacter)
    {
        switch (currentInputCharacter)
        {
            // Switch to the comment end state.
            case '-': // -
                SwitchState(HtmlTokenizerState.CommentEnd);
                break;

            // Anything else
            // Append a U+002D HYPHEN-MINUS character (-) to the comment token's data.
            // Reconsume in the comment state.
            default:
                CurrentToken<CommentToken>().Data.Append('-');
                SwitchState(HtmlTokenizerState.Comment, true);
                break;
        }

        // TODO: Implement EOF handling
    }

    // 13.2.5.51 Comment end state
    // https://html.spec.whatwg.org/multipage/parsing.html#comment-end-state
    private void CommentEndState(char currentInputCharacter)
    {
        switch (currentInputCharacter)
        {
            // Switch to the data state.
            // Emit the current comment token.
            case '>': // >
                SwitchState(HtmlTokenizerState.Data);
                EmitToken<CommentToken>(CurrentToken<CommentToken>().Data.ToString());
                break;

            // Switch to the comment end bang state.
            case '!': // !
                SwitchState(HtmlTokenizerState.CommentEndBang);
                break;

            // Append a U+002D HYPHEN-MINUS character (-) to the comment token's data.
            case '-': // -
                CurrentToken<CommentToken>().Data.Append('-');
                break;

            // Append two U+002D HYPHEN-MINUS characters (-) to the comment token's data.
            // Reconsume in the comment state.
            default:
                CurrentToken<CommentToken>().Data.Append("--");
                SwitchState(HtmlTokenizerState.Comment, true);
                break;
        }
    }

    // 13.2.5.52 Comment end bang state
    // https://html.spec.whatwg.org/multipage/parsing.html#comment-end-bang-state
    private void CommentEndBangState(char currentInputCharacter)
    {
        switch (currentInputCharacter)
        {
            // Append two U+002D HYPHEN-MINUS characters (-) and a U+0021 EXCLAMATION MARK character (!) to the comment token's data.
            // Switch to the comment end dash state.
            case '-': // -
                CurrentToken<CommentToken>().Data.Append("--!");
                SwitchState(HtmlTokenizerState.CommentEndDash);
                break;

            // This is an incorrectly-closed-comment parse error.
            // Switch to the data state.
            // Emit the current comment token.
            case '>': // >
                LogParseError("incorrectly-closed-comment", CurrentToken<CommentToken>());
                SwitchState(HtmlTokenizerState.Data);
                EmitToken<CommentToken>();
                break;

            // Anything else
            // Append two U+002D HYPHEN-MINUS characters (-) and a U+0021 EXCLAMATION MARK character (!) to the comment token's data.
            // Reconsume in the comment state. 
            default:
                CurrentToken<CommentToken>().Data.Append("--!");
                SwitchState(HtmlTokenizerState.Comment, true);
                break;
        }

        // TODO: Implement EOF handling
    }

    // 13.2.5.44 Comment start dash state
    // https://html.spec.whatwg.org/multipage/parsing.html#comment-start-dash-state
    private void CommentStartDashState(char currentInputCharacter)
    {
        switch (currentInputCharacter)
        {
            // Switch to the comment end state.
            case '-': // -
                SwitchState(HtmlTokenizerState.CommentEnd);
                break;

            // This is an abrupt-closing-of-empty-comment parse error.
            // Switch to the data state.
            // Emit the current comment token.
            case '>': // >
                LogParseError("abrupt-closing-of-empty-comment", CurrentToken<CommentToken>());
                SwitchState(HtmlTokenizerState.Data);
                EmitToken<CommentToken>();
                break;

            // Append a U+002D HYPHEN-MINUS character (-) to the comment token's data.
            // Reconsume in the comment state.
            default:
                CurrentToken<CommentToken>().Data.Append('-');
                SwitchState(HtmlTokenizerState.Comment, true);
                break;
        }
    }

    // 13.2.5.41 Bogus comment state
    // https://html.spec.whatwg.org/multipage/parsing.html#bogus-comment-state
    private void BogusCommentState(char currentInputCharacter)
    {
        switch (currentInputCharacter)
        {
            // Switch to the data state.
            // Emit the current comment token.
            case '>': // >
                SwitchState(HtmlTokenizerState.Data);
                EmitToken<CommentToken>();
                break;

            // This is an unexpected-null-character parse error.
            // Append a U+FFFD REPLACEMENT CHARACTER character to the comment token's data.
            case '\0': // NULL
                LogParseError("unexpected-null-character", CurrentToken<CommentToken>());
                CurrentToken<CommentToken>().Data.Append('\uFFFD');
                break;

            // Anything else
            // Append the current input character to the comment token's data.
            default:
                CurrentToken<CommentToken>().Data.Append(currentInputCharacter);
                break;
        }

        // TODO: Implement EOF handling
    }

    // 13.2.5.46 Comment less-than sign state
    // https://html.spec.whatwg.org/multipage/parsing.html#comment-less-than-sign-state
    private void CommentLessThanSignState(char currentInputCharacter)
    {
        switch (currentInputCharacter)
        {
            // Append the current input character to the comment token's data.
            // Switch to the comment less-than sign bang state.
            case '!': // !
                CurrentToken<CommentToken>().Data.Append(currentInputCharacter);
                SwitchState(HtmlTokenizerState.CommentLessThanSignBang);
                break;

            // Append the current input character to the comment token's data.
            case '<': // <
                CurrentToken<CommentToken>().Data.Append(currentInputCharacter);
                break;

            // Anything else
            // Reconsume in the comment state.
            default:
                SwitchState(HtmlTokenizerState.Comment, true);
                break;
        }
    }

    private void CharacterReferenceState(char currentInputCharacter)
    {
        _temporaryBuffer.Clear();
        _temporaryBuffer.Append('&');

        switch (currentInputCharacter)
        {
            // ASCII alphanumeric
            // Reconsume in the named character reference state.
            case >= 'A' and <= 'Z': // A-Z
            case >= 'a' and <= 'z': // a-z
            case >= '0' and <= '9': // 0-9
                SwitchState(HtmlTokenizerState.NamedCharacterReference, true);
                break;

            // Append the current input character to the temporary buffer.
            // Switch to the numeric character reference state.
            case '#':
                _temporaryBuffer.Append(currentInputCharacter);
                SwitchState(HtmlTokenizerState.NumericCharacterReference);
                break;

            // Anything else
            // Flush code points consumed as a character reference.
            // Reconsume in the return state
            default:
                FlushCodePointsConsumedAsCharacterReference();
                SwitchState(_returnState, true);
                break;
        }
    }

    // 13.2.5.73 Named character reference state
    // https://html.spec.whatwg.org/multipage/parsing.html#named-character-reference-state
    private void NamedCharacterReferenceState(char currentInputCharacter)
    {
        // FIXME: I dont think that my implementation is correct
        // The spec is kinda confusing here

        // Consume the maximum number of characters possible,
        // where the consumed characters are one of the identifiers in the first column of the named character references table.
        // Append each character to the temporary buffer when it's consumed.
        var match = Entities.CodePointsFromEntity(Encoding.UTF8.GetString(_buffer.PeekRemainingBytes()));
        if (match.HasMatch)
        {
            // TODO: This is not completely implemented
            // If the character reference was consumed as part of an attribute,
            // and the last character matched is not a U+003B SEMICOLON character (;),
            // and the next input character is either a U+003D EQUALS SIGN character (=) or an ASCII alphanumeric,
            // then, for historical reasons, flush code points consumed as a character reference and switch to the return state.

            Skip(match.Entity.Length - 1);
            foreach (var ch in match.Entity)
                _temporaryBuffer.Append(ch);

            // TODO: Implement consumed as part of an attribute

            _temporaryBuffer.Clear();

            // Append all code points consumed to the temporary buffer.
            foreach (var codePoint in match.CodePoints)
                _temporaryBuffer.Append((char)codePoint);

            FlushCodePointsConsumedAsCharacterReference();
            SwitchState(_returnState);
        }
        else
        {
            // Otherwise
            // Flush code points consumed as a character reference.
            // Switch to the ambiguous ampersand state.

            FlushCodePointsConsumedAsCharacterReference();
            SwitchState(HtmlTokenizerState.AmbiguousAmpersand);
        }
    }


    // 13.2.5.47 Comment less-than sign bang state
    // https://html.spec.whatwg.org/multipage/parsing.html#comment-less-than-sign-bang-state
    private void CommentLessThanSignBangState(char currentInputCharacter)
    {
        switch (currentInputCharacter)
        {
            // Switch to the comment less-than sign bang dash dash state.
            case '-': // -
                SwitchState(HtmlTokenizerState.CommentLessThanSignBangDashDash);
                break;

            // Anything else
            // Reconsume in the comment end dash state.
            default:
                SwitchState(HtmlTokenizerState.CommentEndDash, true);
                break;
        }
    }

    // 13.2.5.49 Comment less-than sign bang dash dash state
    // https://html.spec.whatwg.org/multipage/parsing.html#comment-less-than-sign-bang-dash-dash-state
    private void CommentLessThanSignBangDashDashState(char currentInputCharacter)
    {
        switch (currentInputCharacter)
        {
            // Reconsume in the comment end state.
            case '<': // <
                SwitchState(HtmlTokenizerState.CommentEnd, true);
                break;

            // Anything else
            // This is a nested-comment parse error.
            // Reconsume in the comment end state.
            default:
                LogParseError("nested-comment", CurrentToken<CommentToken>());
                SwitchState(HtmlTokenizerState.CommentEnd, true);
                break;
        }

        // TODO: Implement EOF handling
    }

    // 13.2.5.40 Self-closing start tag state
    // https://html.spec.whatwg.org/multipage/parsing.html#self-closing-start-tag-state
    private void SelfClosingStartTagState(char currentInputCharacter)
    {
        switch (currentInputCharacter)
        {
            // Set the self-closing flag of the current tag token.
            // Switch to the data state.
            // Emit the current tag token.
            case '>': // >
                CurrentToken<StartTagToken>().SelfClosing = true;
                SwitchState(HtmlTokenizerState.Data);
                EmitToken<TagToken>();
                break;

            // Anything else
            // This is an unexpected-solidus-in-tag parse error.
            // Reconsume in the before attribute name state.
            default:
                LogParseError("unexpected-solidus-in-tag", CurrentToken<CharacterToken>());
                SwitchState(HtmlTokenizerState.BeforeAttributeName, true);
                break;
        }
    }

    // 13.2.5.34 After attribute name state
    // https://html.spec.whatwg.org/multipage/parsing.html#after-attribute-name-state
    private void AfterAttributeNameState(char currentInputCharacter)
    {
        switch (currentInputCharacter)
        {
            // Ignore the character.
            case '\t': // \t
            case '\n': // \n
            case '\f': // \f
            case ' ': // space
                break;

            // Switch to the self-closing start tag state.
            case '/': // /
                SwitchState(HtmlTokenizerState.SelfClosingStartTag);
                break;

            // Switch to the before attribute value state.
            case '=': // =
                SwitchState(HtmlTokenizerState.BeforeAttributeValue);
                break;

            // Anything else
            // Start a new attribute in the current tag token.
            // Set that attribute name and value to the empty string.
            // Reconsume in the attribute name state.
            default:
                CurrentToken<StartTagToken>().NewAttribute();
                SwitchState(HtmlTokenizerState.AttributeName, true);
                break;
        }

        // TODO: Implement EOF handling
    }

    private void FlushCodePointsConsumedAsCharacterReference()
    {
        // Each code point in the temporary buffer (in the order they were added to the buffer )
        // user agent must append to the code point from the buffer to the current attribute's value
        // if the character reference was consumed as part of an attribute, or emit the code point as a character token otherwise.

        // TODO: Implement consumed as part of an attribute

        foreach (var codePoint in _temporaryBuffer.ToString())
        {
            CurrentToken<CharacterToken>().Data.Append(codePoint);
            EmitToken<CharacterToken>();
        }
    }

    private void LogParseError(string reason, HTMLToken token)
    {
        Console.WriteLine("ParseError: " + reason + " at " + token.Position + " in " + token.GetType().Name + "");
    }
}