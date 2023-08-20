namespace LibHtmlNet.Tokens;

public class StartTagToken : TagToken
{
    public TagAttribute CurrentAttribute { get; set; }
 
    public List<TagAttribute> Attributes { get; set; }

    public StartTagToken()
    {
        TagName = string.Empty;
        Attributes = new List<TagAttribute>();
        CurrentAttribute = null;
    }
    
    public void StartNewAttribute(string name = "")
    {
        CurrentAttribute = new TagAttribute
        {
            Name = name,
            Value = string.Empty
        };
        Attributes.Add(CurrentAttribute);
    }
}