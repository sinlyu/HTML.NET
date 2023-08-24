using System.Text;
using HTML_NET;
using HTML_NET.Parser;
using HTML_NET.Parser.Tokens;



var url = "https://html.spec.whatwg.org/multipage/parsing.html#numeric-character-reference-end-state";
var httpClient = new HttpClient();
var html = httpClient.GetByteArrayAsync(url).Result;

var tokenizer =
    new HTMLTokenizer(new ByteBuffer(html));

while (tokenizer.NextToken() is { } token)
{
    if (token.Type == HTMLTokenType.Character)
    {
        Console.Write(token.Data);
    }
}

