namespace LibHtmlNet.Tokens;

public abstract class Token
{
    public List<byte> Data { get; set; } = new List<byte>();
}