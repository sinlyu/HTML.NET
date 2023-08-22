namespace HTML_NET.Parser.Tokens;

public class DOCTYPEToken : HTMLToken
{
    public DOCTYPEToken() : base(HTMLTokenType.DOCTYPE)
    {
        Name = "";
        ForceQuirks = false;
    }

    public string Name { get; set; }
    public bool ForceQuirks { get; set; }
}