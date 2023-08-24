namespace HTML_NET.Parser.Tokens;

public class CharacterToken : HTMLToken
{
    public CharacterToken() : base(HTMLTokenType.Character)
    {
    }

    public override int GetLength()
    {
        return Data.Length;
    }
}