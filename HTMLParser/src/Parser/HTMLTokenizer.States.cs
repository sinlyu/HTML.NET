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
        CommentLessThanSignBangDashDash
    }

    private void DataState(byte currentInputCharacter)
    {
        switch (currentInputCharacter)
        {
            // Switch to TagOpen state if we encounter a 0x3C '<' character
            // <
            case 0x3C:
                SwitchState(HtmlTokenizerState.TagOpen);
                break;

            // Switch to CharacterReference state if we encounter a 0x26 '&' character
            // Set the return state to Data
            // &
            case 0x26:
                SwitchState(HtmlTokenizerState.CharacterReference);
                _returnState = HtmlTokenizerState.Data;
                break;

            // Emit the current input character as a character token
            case 0x00:
                // TODO: Implement ParseError
                // unexpected-null-character
                var token = CurrentToken<CharacterToken>();

                // TODO: Implement an elegant way to populate the token data
                token.Data.Clear();
                token.Data.Add(currentInputCharacter);
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
                SwitchState(HtmlTokenizerState.MarkupDeclarationOpen);
                break;

            // Switch to EndTagOpen state if we encounter a 0x2F '/' character
            case 0x2F: // /
                SwitchState(HtmlTokenizerState.EndTagOpen);
                break;

            // Check if current input character is an ASCII alpha character
            // Create a new start tag token, set its tag name to the empty string. Reconsume in the tag name state.
            case >= 0x41 and <= 0x5A: // A-Z
            case >= 0x61 and <= 0x7A: // a-z
                CurrentToken<StartTagToken>();
                SwitchState(HtmlTokenizerState.TagName, true);
                break;

            // This is an unexpected-question-mark-instead-of-tag-name parse error.
            // Create a comment token whose data is the empty string. Reconsume in the bogus comment state.
            case 0x3F: // ?
                // TODO: Implement ParseError
                EmitToken<CommentToken>();
                SwitchState(HtmlTokenizerState.BogusComment, true);
                break;

            // This is an invalid-first-character-of-tag-name parse error.
            // Emit a U+003C LESS-THAN SIGN character token. Reconsume in the data state.
            default:
                // TODO: Implement ParseError
                EmitToken<CharacterToken>(0x3C);
                SwitchState(HtmlTokenizerState.Data, true);
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
                SwitchState(HtmlTokenizerState.TagName, true);
                break;

            // This is a missing-end-tag-name parse error.
            // Switch to the data state.
            case 0x3E: // >
                // TODO: Implement ParseError
                SwitchState(HtmlTokenizerState.Data);
                break;

            // This is an invalid-first-character-of-tag-name parse error.
            // Create a comment token whose data is the empty string.
            // Reconsume in the bogus comment state.
            default:
                // TODO: Implement ParseError
                EmitToken<CommentToken>();
                SwitchState(HtmlTokenizerState.BogusComment);
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
                SwitchState(HtmlTokenizerState.BeforeAttributeName);
                break;

            // Switch to the self-closing start tag state.
            case 0x2F: // /
                SwitchState(HtmlTokenizerState.SelfClosingStartTag);
                break;

            // Switch to the data state. Emit the current tag token.
            case 0x3E: // >
                EmitToken<TagToken>();
                SwitchState(HtmlTokenizerState.Data);
                break;

            // ASCII upper alpha character
            // Append the lowercase version of the current input character (add 0x0020 to the character's code point) to the current tag token's tag name.
            case >= 0x41 and <= 0x5A: // A-Z
                CurrentToken<TagToken>().TagName +=
                    Encoding.UTF8.GetString(new[] { (byte)(currentInputCharacter + 0x20) });
                break;

            // Anything else
            // Append the current input character to the current tag token's tag name.
            default:
                CurrentToken<TagToken>().TagName +=
                    Encoding.UTF8.GetString(new[] { currentInputCharacter });
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
                SwitchState(HtmlTokenizerState.AfterAttributeName, true);
                break;

            // This is an unexpected-equals-sign-before-attribute-name parse error.
            // Start a new attribute in the current tag token. Set that attribute's name to the current input character,
            // and its value to the empty string. Switch to the attribute name state.
            case 0x3D: // =
                // TODO: Implement ParseError
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
                SwitchState(HtmlTokenizerState.AfterAttributeName, true);
                break;

            // Switch to the before attribute value state.
            case 0x3D: // =
                SwitchState(HtmlTokenizerState.BeforeAttributeValue);
                break;

            // ASCII upper alpha character
            // Append the lowercase version of the current input character (add 0x0020 to the character's code point) to the current attribute's name.
            case >= 0x41 and <= 0x5A: // A-Z
                CurrentToken<StartTagToken>().AddAttributeName((byte)(currentInputCharacter + 0x20));
                break;

            // This is an unexpected-null-character parse error.
            // Append a U+FFFD REPLACEMENT CHARACTER character to the current attribute's name.
            case 0x00: // NULL
                CurrentToken<StartTagToken>().AddAttributeName("\uFFFD");
                break;

            // This is an unexpected-character-in-attribute-name parse error.
            // Treat it as per the "anything else" entry below.
            case 0x22: // "
            case 0x27: // '
            case 0x3C: // <
            // TODO: Implement ParseError
            // https://html.spec.whatwg.org/multipage/parsing.html#attribute-name-state
            default:
                CurrentToken<StartTagToken>().AddAttributeName(currentInputCharacter);
                break;
        }
    }

    private void MarkupDeclarationOpenState(byte currentInputCharacter)
    {
        // If the next two characters are both U+002D (-) characters
        // Consume those two characters, create a comment token whose data is the empty string, and switch to the comment start state.
        if (currentInputCharacter == 0x2D && _buffer.PeekByte() == 0x2D)
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
                SwitchState(HtmlTokenizerState.BeforeDocTypeName, true);
                break;

            // Anything else
            // This is a missing-whitespace-before-doctype-name parse error.
            // Reconsume in the before DOCTYPE name state.
            default:
                // TODO: Implement ParseError
                SwitchState(HtmlTokenizerState.BeforeDocTypeName, true);
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
                CurrentToken<DOCTYPEToken>().Name =
                    Encoding.UTF8.GetString(new[] { (byte)(currentInputCharacter + 0x20) });
                SwitchState(HtmlTokenizerState.DocTypeName);
                break;

            // This is an unexpected-null-character parse error.
            // Create a new DOCTYPE token. Set its name to a U+FFFD REPLACEMENT CHARACTER character.
            // Switch to the DOCTYPE name state.
            case 0x00: // NULL
                CurrentToken<DOCTYPEToken>().ForceQuirks = true;
                EmitToken<DOCTYPEToken>();
                SwitchState(HtmlTokenizerState.Data);
                break;

            // This is an missing-doctype-name parse error.
            // Create a new DOCTYPE token. Set its force-quirks flag to on.
            // Emit the token.
            // Switch to the data state.
            case 0x3E: // >
                CurrentToken<DOCTYPEToken>().ForceQuirks = true;
                EmitToken<DOCTYPEToken>();
                SwitchState(HtmlTokenizerState.Data);
                break;

            // Anything else
            // Create a new DOCTYPE token. Set its name to the current input character.
            // Switch to the DOCTYPE name state.
            default:
                CurrentToken<DOCTYPEToken>().Name = Encoding.UTF8.GetString(new[] { currentInputCharacter });
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
                EmitToken<DOCTYPEToken>();
                SwitchState(HtmlTokenizerState.Data);
                break;

            // ASCII upper alpha character
            // Append the lowercase version of the current input character (add 0x0020 to the character's code point) to the current DOCTYPE token's name.
            case >= 0x41 and <= 0x5A: // A-Z
                CurrentToken<DOCTYPEToken>().Name +=
                    Encoding.UTF8.GetString(new[] { (byte)(currentInputCharacter + 0x20) });
                break;

            // This is an unexpected-null-character parse error.
            // Append a U+FFFD REPLACEMENT CHARACTER character to the current DOCTYPE token's name.
            case 0x00: // NULL
                // TODO: Implement ParseError
                CurrentToken<DOCTYPEToken>().Name += "\uFFFD";
                break;

            // Anything else
            // Append the current input character to the current DOCTYPE token's name.
            default:
                CurrentToken<DOCTYPEToken>().Name += Encoding.UTF8.GetString(new[] { currentInputCharacter });
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
                SwitchState(HtmlTokenizerState.AttributeValueUnquoted, true);
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
                SwitchState(HtmlTokenizerState.Comment, true);
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
                CurrentToken<StartTagToken>().AddAttributeValue("\uFFFD");
                break;

            // Anything else
            // Append the current input character to the current attribute value.
            default:
                CurrentToken<StartTagToken>().AddAttributeValue(currentInputCharacter);
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
                SwitchState(HtmlTokenizerState.BeforeAttributeName, true);
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
                SwitchState(HtmlTokenizerState.Comment, true);
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
                SwitchState(HtmlTokenizerState.Comment, true);
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
                SwitchState(HtmlTokenizerState.Comment, true);
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
                SwitchState(HtmlTokenizerState.Comment, true);
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
                SwitchState(HtmlTokenizerState.Comment, true);
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
                SwitchState(HtmlTokenizerState.CommentEndDash, true);
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
                SwitchState(HtmlTokenizerState.CommentEnd, true);
                break;

            // Anything else
            // This is a nested-comment parse error.
            // Reconsume in the comment end state.
            default:
                // TODO: Implement ParseError
                SwitchState(HtmlTokenizerState.CommentEnd, true);
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
                SwitchState(HtmlTokenizerState.BeforeAttributeName, true);
                break;
        }
    }

    // 13.2.5.34 After attribute name state
    // https://html.spec.whatwg.org/multipage/parsing.html#after-attribute-name-state
    private void AfterAttributeNameState(byte currentInputCharacter)
    {
        switch (currentInputCharacter)
        {
            // Ignore the character.
            case 0x09: // \t
            case 0x0A: // \n
            case 0x0C: // \f
            case 0x20: // space
                break;

            // Switch to the self-closing start tag state.
            case 0x2F: // /
                SwitchState(HtmlTokenizerState.SelfClosingStartTag);
                break;

            // Switch to the before attribute value state.
            case 0x3D: // =
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
}