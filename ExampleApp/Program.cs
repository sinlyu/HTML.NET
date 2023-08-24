using System.Text;
using HTML_NET;
using HTML_NET.Parser;
using HTML_NET.Parser.Tokens;

var tokenizer =
    new HTMLTokenizer(new ByteBuffer(File.ReadAllBytes(@"E:\projects\LibHtmlNet\Tests\html_basic_document.html")));

var tokens = new List<HTMLToken>();

while (tokenizer.NextToken() is { } token)
{
    if (token.Type == HTMLTokenType.Character)
    {
        Console.Write(token.Data);
    }
}

var x = 0;