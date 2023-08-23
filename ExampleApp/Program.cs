using HTML_NET;
using HTML_NET.Parser;
using HTML_NET.Parser.Tokens;

// TODO: <div x= y="a"></div> breaks the tokenizer currently because AttributeValueUnquotedState is not implemented yet

var tokenizer =
    new HTMLTokenizer(new ByteBuffer(File.ReadAllBytes(@"/home/utherali/RiderProjects/HTML.NET/Tests/html_basic_document.html")));
while (tokenizer.NextToken() is { } token)
{
    if (token.Type == HTMLTokenType.Character)
    {
        Console.WriteLine(token.Data);
    }
}