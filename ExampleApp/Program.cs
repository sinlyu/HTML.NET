using System.Diagnostics;
using HTML_NET;
using HTML_NET.Parser;
using HTML_NET.Parser.Tokens;


var url = "https://en.wikipedia.org/wiki/List_of_The_Big_Bang_Theory_episodes";
var httpClient = new HttpClient();
var html = httpClient.GetByteArrayAsync(url).Result;

Console.WriteLine("Start parsing url: " + url);

var sw = Stopwatch.StartNew();
var tokenizer =
    new HTMLTokenizer(new ByteBuffer(html));

while (tokenizer.NextToken() is { } token)
{
    
}
sw.Stop();

Console.WriteLine("Parsing took: " + sw.ElapsedMilliseconds + "ms");
