
using HTML_NET;
using HTML_NET.Parser;
using HTML_NET.Parser.Tokens;

// TODO: <div x= y="a"></div> breaks the tokenizer currently because AttributeValueUnquotedState is not implemented yet

var tokenizer = new HTMLTokenizer(new ByteBuffer(File.ReadAllBytes(@"E:\projects\LibHtmlNet\Tests\html_basic_document.html")));
while (tokenizer.NextToken() is { } token)
{
    if (token.Type == HTMLTokenType.StartTag)
    {
        var startTag = (StartTagToken) token;
        if (startTag.TagName == "div")
        {
            var x = 0;
        }
        Console.WriteLine("Tag name: " + ((StartTagToken) token).TagName);
    }
}