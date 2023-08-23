using System.Text;
using HTML_NET;
using HTML_NET.Parser;
using HTML_NET.Parser.Tokens;

// TODO: <div x= y="a"></div> breaks the tokenizer currently because AttributeValueUnquotedState is not implemented yet

var tokenizer =
    new HTMLTokenizer(new ByteBuffer(File.ReadAllBytes(@"E:\projects\LibHtmlNet\Tests\html_basic_document.html")));

var tokens = new List<HTMLToken>();

while (tokenizer.NextToken() is { } token)
{
    tokens.Add(token);
}

var x = 0;