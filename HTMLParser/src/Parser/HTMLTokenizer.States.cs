﻿using System.Text;
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
        AmbiguousAmpersand,
        HexadecimalCharacterReferenceStart,
        DecimalCharacterReferenceStart,
        NumericCharacterReferenceEnd,
        DecimalCharacterReference
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
                var token = CurrentToken(HTMLTokenType.Character);
                LogParseError("unexpected-null-character", token);
                EmitToken(HTMLTokenType.Character, currentInputCharacter.ToString());
                break;

            // Anything else
            default:
                // Emit the current input character as a character token
                EmitToken(HTMLTokenType.Character, currentInputCharacter);
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
                CurrentToken(HTMLTokenType.StartTag);
                SwitchState(HtmlTokenizerState.TagName, true);
                break;

            // This is an unexpected-question-mark-instead-of-tag-name parse error.
            // Create a comment token whose data is the empty string. Reconsume in the bogus comment state.
            case '?': // ?
                LogParseError("unexpected-question-mark-instead-of-tag-name", CurrentToken(HTMLTokenType.Comment));
                EmitToken(HTMLTokenType.Comment);
                SwitchState(HtmlTokenizerState.BogusComment, true);
                break;

            // This is an invalid-first-character-of-tag-name parse error.
            // Emit a U+003C LESS-THAN SIGN character token. Reconsume in the data state.
            default:
                LogParseError("invalid-first-character-of-tag-name", CurrentToken(HTMLTokenType.Character));
                EmitToken(HTMLTokenType.Character, '<');
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
                CurrentToken(HTMLTokenType.EndTag);
                SwitchState(HtmlTokenizerState.TagName, true);
                break;

            // This is a missing-end-tag-name parse error.
            // Switch to the data state.
            case '>': // >
                LogParseError("missing-end-tag-name", CurrentToken(HTMLTokenType.Character));
                SwitchState(HtmlTokenizerState.Data);
                break;

            // This is an invalid-first-character-of-tag-name parse error.
            // Create a comment token whose data is the empty string.
            // Reconsume in the bogus comment state.
            default:
                LogParseError("invalid-first-character-of-tag-name", CurrentToken(HTMLTokenType.Comment));
                EmitToken(HTMLTokenType.Comment);
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
                EmitToken(HTMLTokenType.Tag);
                SwitchState(HtmlTokenizerState.Data);
                break;

            // ASCII upper alpha character
            // Append the lowercase version of the current input character (add 0x0020 to the character's code point) to the current tag token's tag name.
            case >= 'A' and <= 'Z': // A-Z
                CurrentToken(HTMLTokenType.Tag).TagName += currentInputCharacter + 0x20;
                break;

            // Anything else
            // Append the current input character to the current tag token's tag name.
            default:
                CurrentToken(HTMLTokenType.Tag).TagName += currentInputCharacter;
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
                LogParseError("unexpected-equals-sign-before-attribute-name", CurrentToken(HTMLTokenType.Character));
                CurrentToken(HTMLTokenType.StartTag).NewAttribute(currentInputCharacter);
                SwitchState(HtmlTokenizerState.AttributeName);
                break;

            // Anything else
            // Start a new attribute in the current tag token.
            // Set that attribute's name and value to the empty string.
            default:
                CurrentToken(HTMLTokenType.StartTag).NewAttribute();
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
                CurrentToken(HTMLTokenType.StartTag).AddAttributeName((char)(currentInputCharacter + 0x20));
                break;

            // This is an unexpected-null-character parse error.
            // Append a U+FFFD REPLACEMENT CHARACTER character to the current attribute's name.
            case '\0': // NULL
                LogParseError("unexpected-null-character", CurrentToken(HTMLTokenType.Character));
                CurrentToken(HTMLTokenType.StartTag).AddAttributeName("\uFFFD");
                break;

            // This is an unexpected-character-in-attribute-name parse error.
            // Treat it as per the "anything else" entry below.
            case '"': // "
            case '\'': // '
            case '<': // <
                LogParseError("unexpected-character-in-attribute-name", CurrentToken(HTMLTokenType.Character));
                CurrentToken(HTMLTokenType.StartTag).AddAttributeName(currentInputCharacter);
                break;

            // Anything else
            // Append the current input character to the current attribute's name.
            default:
                CurrentToken(HTMLTokenType.StartTag).AddAttributeName(currentInputCharacter);
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
            CurrentToken(HTMLTokenType.Comment);
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
            LogParseError("incorrectly-opened-comment", CurrentToken(HTMLTokenType.Comment));
            CurrentToken(HTMLTokenType.Comment);
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
                LogParseError("missing-whitespace-before-doctype-name", CurrentToken(HTMLTokenType.Character));
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
                CurrentToken(HTMLTokenType.DOCTYPE).Name =
                    Encoding.UTF8.GetString(new[] { (byte)(currentInputCharacter + 0x20) });
                SwitchState(HtmlTokenizerState.DocTypeName);
                break;

            // This is an unexpected-null-character parse error.
            // Create a new DOCTYPE token. Set its name to a U+FFFD REPLACEMENT CHARACTER character.
            // Switch to the DOCTYPE name state.
            case '\0': // NULL
                CurrentToken(HTMLTokenType.DOCTYPE).ForceQuirks = true;
                EmitToken(HTMLTokenType.DOCTYPE);
                SwitchState(HtmlTokenizerState.Data);
                break;

            // This is an missing-doctype-name parse error.
            // Create a new DOCTYPE token. Set its force-quirks flag to on.
            // Emit the token.
            // Switch to the data state.
            case '>': // >
                CurrentToken(HTMLTokenType.DOCTYPE).ForceQuirks = true;
                EmitToken(HTMLTokenType.DOCTYPE);
                SwitchState(HtmlTokenizerState.Data);
                break;

            // Anything else
            // Create a new DOCTYPE token. Set its name to the current input character.
            // Switch to the DOCTYPE name state.
            default:
                CurrentToken(HTMLTokenType.DOCTYPE).Name = currentInputCharacter.ToString();
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
                EmitToken(HTMLTokenType.DOCTYPE);
                SwitchState(HtmlTokenizerState.Data);
                break;

            // ASCII upper alpha character
            // Append the lowercase version of the current input character (add 0x0020 to the character's code point) to the current DOCTYPE token's name.
            case >= 'A' and <= 'Z': // A-Z
                CurrentToken(HTMLTokenType.DOCTYPE).Name += (char)(currentInputCharacter + 0x20);
                break;

            // This is an unexpected-null-character parse error.
            // Append a U+FFFD REPLACEMENT CHARACTER character to the current DOCTYPE token's name.
            case '\0': // NULL
                LogParseError("unexpected-null-character", CurrentToken(HTMLTokenType.Character));
                CurrentToken(HTMLTokenType.DOCTYPE).Name += "\uFFFD";
                break;

            // Anything else
            // Append the current input character to the current DOCTYPE token's name.
            default:
                CurrentToken(HTMLTokenType.DOCTYPE).Name += currentInputCharacter;
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
                LogParseError("missing-attribute-value", CurrentToken(HTMLTokenType.Tag));
                SwitchState(HtmlTokenizerState.Data);
                EmitToken(HTMLTokenType.Tag);
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
                EmitToken(HTMLTokenType.Comment);
                break;

            // Anything else
            // Reconsume in the comment state.
            default:
                SwitchState(HtmlTokenizerState.Comment, true);
                break;
        }
    }

    // 13.2.5.75 Numeric character reference state
    // https://html.spec.whatwg.org/multipage/parsing.html#character-reference-code
    private void NumericCharacterReferenceState(char currentInputCharacter)
    {
        _characterReferenceCode = 0;

        switch (currentInputCharacter)
        {
            // Append the current input character to the temporary buffer.
            // Switch to the hexadecimal character reference start state.
            case 'x':
            case 'X':
                _temporaryBuffer.Append(currentInputCharacter);
                SwitchState(HtmlTokenizerState.HexadecimalCharacterReferenceStart);
                break;

            // Anything else
            // Reconsume in the decimal character reference start state.
            default:
                SwitchState(HtmlTokenizerState.DecimalCharacterReferenceStart, true);
                break;
        }
    }


    // 13.2.5.76 Hexadecimal character reference start state
    // https://html.spec.whatwg.org/multipage/parsing.html#hexadecimal-character-reference-start-state
    private void HexadecimalCharacterReferenceStartState(char currentInputCharacter)
    {
        switch (currentInputCharacter)
        {
            // ASCII digit
            // https://infra.spec.whatwg.org/#ascii-digit
            // Multiply the character reference code by 16.
            // Add a numeric version of the current input character (subtract 0x0030 from the character's code point) to the character reference code.
            case >= '0' and <= '9': // 0-9
                _characterReferenceCode *= 16;
                _characterReferenceCode += currentInputCharacter - 0x0030;
                break;

            // ASCII upper hex digit
            // https://infra.spec.whatwg.org/#ascii-upper-hex-digit
            // Multiply the character reference code by 16.
            // Add a numeric version of the current input character as a hexadecimal digit (subtract 0x0037 from the character's code point) to the character reference code.
            case >= 'A' and <= 'F': // A-F
                _characterReferenceCode *= 16;
                _characterReferenceCode += currentInputCharacter - 0x0037;
                break;

            // ASCII lower hex digit
            // https://infra.spec.whatwg.org/#ascii-lower-hex-digit
            // Multiply the character reference code by 16.
            // Add a numeric version of the current input character as a hexadecimal digit (subtract 0x0057 from the character's code point) to the character reference code.
            case >= 'a' and <= 'f': // a-f
                _characterReferenceCode *= 16;
                _characterReferenceCode += currentInputCharacter - 0x0057;
                break;

            // Switch to the numeric character reference end state.
            case ';':
                SwitchState(HtmlTokenizerState.NumericCharacterReferenceEnd);
                break;

            // Anything else
            // This is a missing-semicolon-after-character-reference parse error.
            // Reconsume in the numeric character reference end state.
            default:
                LogParseError("missing-semicolon-after-character-reference", CurrentToken(HTMLTokenType.Character));
                SwitchState(HtmlTokenizerState.NumericCharacterReferenceEnd, true);
                break;
        }
    }

    // 13.2.5.77 Decimal character reference start state
    // https://html.spec.whatwg.org/multipage/parsing.html#decimal-character-reference-start-state
    private void DecimalCharacterReferenceStartState(char currentInputCharacter)
    {
        switch (currentInputCharacter)
        {
            // ASCII digit
            // Reconsume in the decimal character reference state.
            case >= '0' and <= '9': // 0-9
                SwitchState(HtmlTokenizerState.DecimalCharacterReference, true);
                break;

            // Anything else
            // This is an absence-of-digits-in-numeric-character-reference parse error.
            // Flush code points consumed as a character reference.
            // Reconsume in the return state.
            default:
                LogParseError("absence-of-digits-in-numeric-character-reference", CurrentToken(HTMLTokenType.Character));
                FlushCodePointsConsumedAsCharacterReference();
                SwitchState(_returnState, true);
                break;
        }
    }

    // 13.2.5.80 Numeric character reference end state
    // https://html.spec.whatwg.org/multipage/parsing.html#(numeric-character-reference-end-state
    private void NumericCharacterReferenceEndState(char currentInputCharacter)
    {
        // If the number is 0x00, then this is a null-character-reference parse error.
        // Set the character reference code to 0xFFFD.
        if (_characterReferenceCode == 0x00)
        {
            LogParseError("null-character-reference", CurrentToken(HTMLTokenType.Character));
            _characterReferenceCode = 0xFFFD;
        }

        // If the number is greater than 0x10FFFF, then this is a character-reference-outside-unicode-range parse error.
        // Set the character reference code to 0xFFFD.
        else if (_characterReferenceCode > 0x10FFFF)
        {
            LogParseError("character-reference-outside-unicode-range", CurrentToken(HTMLTokenType.Character));
            _characterReferenceCode = 0xFFFD;
        }

        // If the number is a surrogate, then this is a surrogate-character-reference parse error.
        // Set the character reference code to 0xFFFD.
        else if (IsSurrogate(_characterReferenceCode))
        {
            LogParseError("surrogate-character-reference", CurrentToken(HTMLTokenType.Character));
            _characterReferenceCode = 0xFFFD;
        }

        // If the number is a noncharacter, then this is a noncharacter-character-reference parse error.
        else if (IsNonCharacter(_characterReferenceCode))
        {
            LogParseError("noncharacter-character-reference", CurrentToken(HTMLTokenType.Character));
        }

        // If the number is 0x0D, or a control that's not ASCII whitespace, then this is a control-character-reference parse error.
        else if (_characterReferenceCode == 0x0D ||
                 (IsControl(_characterReferenceCode) && !IsWhiteSpace(_characterReferenceCode)))
        {
            LogParseError("control-character-reference", CurrentToken(HTMLTokenType.Character));

            // If the number is one of the numbers in the first column of the following table,
            // then find the row with that number in the first column,
            // and set the character reference code to the number in the second column of that row.

            // FIXME: use a dictionary instead of a tuple array

            (int Number, int CodePoint)[] conversionTable =
            {
                (0x80, 0x20AC), // EURO SIGN (€)
                (0x82, 0x201A), // SINGLE LOW-9 QUOTATION MARK (‚) 
                (0x83, 0x0192), // LATIN SMALL LETTER F WITH HOOK (ƒ)
                (0x84, 0x201E), // DOUBLE LOW-9 QUOTATION MARK („)
                (0x85, 0x2026), // HORIZONTAL ELLIPSIS (…)
                (0x86, 0x2020), // DAGGER (†)
                (0x87, 0x2021), // DOUBLE DAGGER (‡)
                (0x88, 0x02C6), // MODIFIER LETTER CIRCUMFLEX ACCENT (ˆ)
                (0x89, 0x2030), // PER MILLE SIGN (‰)
                (0x8A, 0x0160), // LATIN CAPITAL LETTER S WITH CARON (Š) 
                (0x8B, 0x2039), // SINGLE LEFT-POINTING ANGLE QUOTATION MARK (‹) 
                (0x8C, 0x0152), // LATIN CAPITAL LIGATURE OE (Œ)
                (0x8E, 0x017D), // LATIN CAPITAL LETTER Z WITH CARON (Ž)
                (0x91, 0x2018), // LEFT SINGLE QUOTATION MARK (‘)
                (0x92, 0x2019), // RIGHT SINGLE QUOTATION MARK (’) 
                (0x93, 0x201C), // LEFT DOUBLE QUOTATION MARK (“)
                (0x94, 0x201D), // RIGHT DOUBLE QUOTATION MARK (”)
                (0x95, 0x2022), // BULLET (•)
                (0x96, 0x2013), // EN DASH (–)
                (0x97, 0x2014), // EM DASH (—)
                (0x98, 0x02DC), // SMALL TILDE (˜)
                (0x99, 0x2122), // TRADE MARK SIGN (™)
                (0x9A, 0x0161), // LATIN SMALL LETTER S WITH CARON (š)
                (0x9B, 0x203A), // SINGLE RIGHT-POINTING ANGLE QUOTATION MARK (›)
                (0x9C, 0x0153), // LATIN SMALL LIGATURE OE (œ)
                (0x9E, 0x017E), // LATIN SMALL LETTER Z WITH CARON (ž)
                (0x9F, 0x0178) // LATIN CAPITAL LETTER Y WITH DIAERESIS (Ÿ)
            };

            foreach (var (number, codePoint) in conversionTable)
            {
                if (_characterReferenceCode != number) continue;
                _characterReferenceCode = codePoint;
                break;
            }
        }

        // Set the temporary buffer to the empty string.
        // Append a code point equal to the character reference code to the temporary buffer.
        // Flush code points consumed as a character reference.
        // Switch to the return state.

        _temporaryBuffer.Clear();
        _temporaryBuffer.Append((char)_characterReferenceCode);
        FlushCodePointsConsumedAsCharacterReference();
        SwitchState(_returnState);
    }

    // 13.2.5.79 Decimal character reference state
    // https://html.spec.whatwg.org/multipage/parsing.html#decimal-character-reference-state
    private void DecimalCharacterReferenceState(char currentInputCharacter)
    {
        switch (currentInputCharacter)
        {
            // ASCII digit
            // Multiply the character reference code by 10.
            // Add a numeric version of the current input character (subtract 0x0030 from the character's code point) to the character reference code.
            case >= '0' and <= '9': // 0-9
                _characterReferenceCode *= 10;
                _characterReferenceCode += currentInputCharacter - 0x0030;
                break;

            // Switch to the numeric character reference end state.
            case ';': // ;
                SwitchState(HtmlTokenizerState.NumericCharacterReferenceEnd);
                break;

            // Anything else
            // This is a missing-semicolon-after-character-reference parse error.
            // Reconsume in the numeric character reference end state.
            default:
                LogParseError("missing-semicolon-after-character-reference", CurrentToken(HTMLTokenType.Character));
                SwitchState(HtmlTokenizerState.NumericCharacterReferenceEnd, true);
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
                CurrentToken(HTMLTokenType.Comment).Data.Append(currentInputCharacter);
                SwitchState(HtmlTokenizerState.CommentLessThanSign);
                break;

            // Switch to the comment end dash state
            case '-': // -
                SwitchState(HtmlTokenizerState.CommentEndDash);
                break;

            // This is an unexpected-null-character parse error.
            // Append a U+FFFD REPLACEMENT CHARACTER character to the comment token's data.
            case '\0': // NULL
                CurrentToken(HTMLTokenType.Comment).Data.Append('\uFFFD');
                break;

            // Anything else
            // Append the current input character to the comment token's data.
            default:
                CurrentToken(HTMLTokenType.Comment).Data.Append(currentInputCharacter);
                break;
        }

        // TODO: Implement EOF handling
    }

    // 13.2.5.38 Attribute value (unquoted) state
    // https://html.spec.whatwg.org/multipage/parsing.html#attribute-value-(unquoted)-state
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
                EmitToken(HTMLTokenType.Tag);
                break;

            // This is an unexpected-null-character parse error.
            // Append a U+FFFD REPLACEMENT CHARACTER character to the current attribute's value.
            case '\0':
                LogParseError("unexpected-null-character", CurrentToken(HTMLTokenType.StartTag));
                CurrentToken(HTMLTokenType.StartTag).AddAttributeValue("\uFFFD");
                break;

            // This is an unexpected-character-in-unquoted-attribute-value parse error.
            // Treat it as per the "anything else" entry below.
            // Append the current input character to the current attribute's value.
            case '"':
            case '\'':
            case '<':
            case '=':
            case '`':
                LogParseError("unexpected-character-in-unquoted-attribute-value", CurrentToken(HTMLTokenType.StartTag));
                CurrentToken(HTMLTokenType.StartTag).AddAttributeValue(currentInputCharacter);
                break;

            // Anything else
            // Append the current input character to the current attribute's value.
            default:
                CurrentToken(HTMLTokenType.StartTag).AddAttributeValue(currentInputCharacter);
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
                LogParseError("unexpected-null-character", CurrentToken(HTMLTokenType.StartTag));
                CurrentToken(HTMLTokenType.StartTag).AddAttributeValue("\uFFFD");
                break;

            // Anything else
            // Append the current input character to the current attribute's value.
            default:
                CurrentToken(HTMLTokenType.StartTag).AddAttributeValue(currentInputCharacter);
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
                CurrentToken(HTMLTokenType.StartTag).AddAttributeValue("\uFFFD");
                break;

            // Anything else
            // Append the current input character to the current attribute value.
            default:
                CurrentToken(HTMLTokenType.StartTag).AddAttributeValue(currentInputCharacter);
                break;
        }
    }

    // 13.2.5.39 After attribute value (quoted) state
    // https://html.spec.whatwg.org/multipage/parsing.html#after-attribute-value-(quoted)-state
    private void AfterAttributeValueQuotedState(char currentInputCharacter)
    {
        CurrentToken(HTMLTokenType.Tag).FinishAttribute();
        
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
                EmitToken(HTMLTokenType.Tag);
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
                CurrentToken(HTMLTokenType.Comment).Data.Append('-');
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
                EmitToken(HTMLTokenType.Comment, CurrentToken(HTMLTokenType.Comment).Data.ToString());
                break;

            // Switch to the comment end bang state.
            case '!': // !
                SwitchState(HtmlTokenizerState.CommentEndBang);
                break;

            // Append a U+002D HYPHEN-MINUS character (-) to the comment token's data.
            case '-': // -
                CurrentToken(HTMLTokenType.Comment).Data.Append('-');
                break;

            // Append two U+002D HYPHEN-MINUS characters (-) to the comment token's data.
            // Reconsume in the comment state.
            default:
                CurrentToken(HTMLTokenType.Comment).Data.Append("--");
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
                CurrentToken(HTMLTokenType.Comment).Data.Append("--!");
                SwitchState(HtmlTokenizerState.CommentEndDash);
                break;

            // This is an incorrectly-closed-comment parse error.
            // Switch to the data state.
            // Emit the current comment token.
            case '>': // >
                LogParseError("incorrectly-closed-comment", CurrentToken(HTMLTokenType.Comment));
                SwitchState(HtmlTokenizerState.Data);
                EmitToken(HTMLTokenType.Comment);
                break;

            // Anything else
            // Append two U+002D HYPHEN-MINUS characters (-) and a U+0021 EXCLAMATION MARK character (!) to the comment token's data.
            // Reconsume in the comment state. 
            default:
                CurrentToken(HTMLTokenType.Comment).Data.Append("--!");
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
                LogParseError("abrupt-closing-of-empty-comment", CurrentToken(HTMLTokenType.Comment));
                SwitchState(HtmlTokenizerState.Data);
                EmitToken(HTMLTokenType.Comment);
                break;

            // Append a U+002D HYPHEN-MINUS character (-) to the comment token's data.
            // Reconsume in the comment state.
            default:
                CurrentToken(HTMLTokenType.Comment).Data.Append('-');
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
                EmitToken(HTMLTokenType.Comment);
                break;

            // This is an unexpected-null-character parse error.
            // Append a U+FFFD REPLACEMENT CHARACTER character to the comment token's data.
            case '\0': // NULL
                LogParseError("unexpected-null-character", CurrentToken(HTMLTokenType.Comment));
                CurrentToken(HTMLTokenType.Comment).Data.Append('\uFFFD');
                break;

            // Anything else
            // Append the current input character to the comment token's data.
            default:
                CurrentToken(HTMLTokenType.Comment).Data.Append(currentInputCharacter);
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
                CurrentToken(HTMLTokenType.Comment).Data.Append(currentInputCharacter);
                SwitchState(HtmlTokenizerState.CommentLessThanSignBang);
                break;

            // Append the current input character to the comment token's data.
            case '<': // <
                CurrentToken(HTMLTokenType.Comment).Data.Append(currentInputCharacter);
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
        
        var match =  Entities.CodePointsFromEntity(_buffer.PeekRemainingBytes());
        if (match.HasMatch)
        {
            // TODO: This is not completely implemented
            // If the character reference was consumed as part of an attribute,
            // and the last character matched is not a U+003B SEMICOLON character (;),
            // and the next input character is either a U+003D EQUALS SIGN character (=) or an ASCII alphanumeric,
            // then, for historical reasons, flush code points consumed as a character reference and switch to the return state.

            Skip(match.Entity.Length);

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

    private void AmbiguousAmpersandState(char currentInputCharacter)
    {
        switch (currentInputCharacter)
        {
            // If the character reference was consumed as part of an attribute,
            // then append the current input character to the current attribute's value.
            // Otherwise, emit the current input character as a character token.
            case >= 'A' and <= 'Z': // A-Z
            case >= 'a' and <= 'z': // a-z
            case >= '0' and <= '9': // 0-9
                if (ConsumedAsPartOfAnAttribute())
                {
                    CurrentToken(HTMLTokenType.Tag).AddAttributeValue(currentInputCharacter);
                }
                else
                {
                    CurrentToken(HTMLTokenType.Character).Data.Append(currentInputCharacter);
                    EmitToken(HTMLTokenType.Character, CurrentToken(HTMLTokenType.Character).Data.ToString());
                }

                break;

            // This is an unknown-named-character-reference parse error. Reconsume in the return state.
            case ';':
                LogParseError("unknown-named-character-reference", CurrentToken(HTMLTokenType.Character));
                SwitchState(_returnState, true);
                break;

            // Reconsume in the return state.
            default:
                SwitchState(_returnState, true);
                break;
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
                LogParseError("nested-comment", CurrentToken(HTMLTokenType.Comment));
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
                CurrentToken(HTMLTokenType.StartTag).SelfClosing = true;
                SwitchState(HtmlTokenizerState.Data);
                EmitToken(HTMLTokenType.Tag);
                break;

            // Anything else
            // This is an unexpected-solidus-in-tag parse error.
            // Reconsume in the before attribute name state.
            default:
                LogParseError("unexpected-solidus-in-tag", CurrentToken(HTMLTokenType.Tag));
                SwitchState(HtmlTokenizerState.BeforeAttributeName, true);
                break;
        }
    }

    // 13.2.5.34 After attribute name state
    // https://html.spec.whatwg.org/multipage/parsing.html#after-attribute-name-state
    private void AfterAttributeNameState(char currentInputCharacter)
    {
        CurrentToken(HTMLTokenType.Tag).FinishAttribute();
        
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

            // Switch to the data state.
            // Emit the current tag token.
            case '>': // >
                SwitchState(HtmlTokenizerState.Data);
                EmitToken(HTMLTokenType.Tag);
                break;

            // Anything else
            // Start a new attribute in the current tag token.
            // Set that attribute name and value to the empty string.
            // Reconsume in the attribute name state.
            default:
                CurrentToken(HTMLTokenType.Tag).NewAttribute();
                SwitchState(HtmlTokenizerState.AttributeName, true);
                break;
        }

        // TODO: Implement EOF handling
    }

    // A leading surrogate is a code point that is in the range U+D800 to U+DBFF, inclusive.
    // https://infra.spec.whatwg.org/#leading-surrogate
    private static bool IsLeadingSurrogate(int codePoint)
    {
        // TODO: use char instead of int
        return codePoint is >= 0xD800 and <= 0xDBFF;
    }

    // A trailing surrogate is a code point that is in the range U+DC00 to U+DFFF, inclusive.
    // https://infra.spec.whatwg.org/#leading-surrogate
    private static bool IsTrailingSurrogate(int codePoint)
    {
        // TODO: use char instead of int
        return codePoint is >= 0xDC00 and <= 0xDFFF;
    }

    // A surrogate is a leading surrogate or a trailing surrogate.
    // https://infra.spec.whatwg.org/#surrogate
    private static bool IsSurrogate(int codePoint)
    {
        // TODO: use char instead of int
        return IsLeadingSurrogate(codePoint) ||
               IsTrailingSurrogate(codePoint);
    }

    // A scalar value is a code point that is not a surrogate.
    // https://infra.spec.whatwg.org/#scalar-value
    private static bool IsScalarValue(int codePoint)
    {
        // TODO: use char instead of int
        return !IsSurrogate(codePoint);
    }


    // https://infra.spec.whatwg.org/#noncharacter
    private static bool IsNonCharacter(int codePoint)
    {
        // TODO: use char instead of int
        return codePoint
            is >= 0xFDD0 and <= 0xFDEF
            or 0xFFFE or 0xFFFF
            or 0x1FFFE or 0x1FFFE
            or 0x2FFFF or 0x2FFFE
            or 0x3FFFF or 0x3FFFE
            or 0x4FFFF or 0x4FFFE
            or 0x5FFFF or 0x5FFFE
            or 0x6FFFF or 0x6FFFE
            or 0x7FFFF or 0x7FFFE
            or 0x8FFFF or 0x8FFFE
            or 0x9FFFF or 0x9FFFE
            or 0xAFFFF or 0xAFFFE
            or 0xBFFFF or 0xBFFFE
            or 0xCFFFF or 0xCFFFE
            or 0xDFFFF or 0xDFFFE
            or 0xEFFFF or 0xEFFFE
            or 0xFFFFF or 0xFFFFE
            or 0x10FFFE or 0x10FFFE;
    }

    // https://infra.spec.whatwg.org/#c0-control
    private static bool IsC0Control(int codePoint)
    {
        // TODO: use char instead of int
        // A C0 control is a code point in the range U+0000 NULL to U+001F INFORMATION SEPARATOR ONE, inclusive. 
        return codePoint is >= 0x0000 and <= 0x001F;
    }

    // https://infra.spec.whatwg.org/#c0-control-or-space
    private static bool IsC0ControlOrSpace(int codePoint)
    {
        // TODO: use char instead of int
        // A C0 control or space is a C0 control or U+0020 SPACE. 
        return IsC0Control(codePoint) || codePoint == ' ';
    }

    // https://infra.spec.whatwg.org/#control
    private static bool IsControl(int codePoint)
    {
        // TODO: use char instead of int
        // A control is a C0 control or a code point in the range U+007F DELETE to U+009F APPLICATION PROGRAM COMMAND, inclusive. 
        return IsC0Control(codePoint) || codePoint is >= 0x007F and <= 0x009F;
    }

    // https://infra.spec.whatwg.org/#ascii-whitespace
    private static bool IsWhiteSpace(int codePoint)
    {
        // TODO: use char instead of int
        // ASCII whitespace is U+0009 TAB, U+000A LF, U+000C FF, U+000D CR, or U+0020 SPACE. 
        return codePoint is '\t' or '\n' or '\f' or '\r' or ' ';
    }


    // https://html.spec.whatwg.org/multipage/parsing.html#flush-code-points-consumed-as-a-character-reference
    private void FlushCodePointsConsumedAsCharacterReference()
    {
        // Each code point in the temporary buffer (in the order they were added to the buffer )
        // user agent must append to the code point from the buffer to the current attribute's value
        // if the character reference was consumed as part of an attribute, or emit the code point as a character token otherwise.
        if (ConsumedAsPartOfAnAttribute())
            CurrentToken(HTMLTokenType.Tag).AddAttributeValue(_temporaryBuffer.ToString());
        else
            EmitToken(HTMLTokenType.Character, _temporaryBuffer.ToString());
        _temporaryBuffer.Clear();
    }

    // https://html.spec.whatwg.org/multipage/parsing.html#charref-in-attribute
    private bool ConsumedAsPartOfAnAttribute()
    {
        return _returnState is
            HtmlTokenizerState.AttributeValueDoubleQuoted or
            HtmlTokenizerState.AttributeValueSingleQuoted or
            HtmlTokenizerState.AttributeValueUnquoted;
    }

    private static void LogParseError(string reason, HTMLToken token)
    {
        Console.WriteLine(
            $"\u001b[33mParse error\u001b[0m: \u001b[34m{reason}\u001b[0m at \u001b[34m{token.Position}\u001b[0m");
    }
}