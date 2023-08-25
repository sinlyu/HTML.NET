using System.Diagnostics;
using HTML_NET;
using HTML_NET.Parser;
using HTML_NET.Parser.Tokens;


var url = "https://en.wikipedia.org/wiki/List_of_The_Big_Bang_Theory_episodes";
var httpClient = new HttpClient();
var html = httpClient.GetByteArrayAsync(url).Result;

/*var html = File.ReadAllBytes(@"E:\projects\LibHtmlNet\Tests\html_basic_document.html");
var url = "file:///E:/projects/LibHtmlNet/Tests/html_basic_document.html";*/

Console.WriteLine("Start parsing url: " + url);

var sw = Stopwatch.StartNew();
var tokenizer =
    new HTMLTokenizer(new ByteBuffer(html));

var tokens = new List<HTMLToken>();

while (tokenizer.NextToken() is { } token)
{
    tokens.Add(token);
}
sw.Stop();

Console.WriteLine("Parsing took: " + sw.ElapsedMilliseconds + "ms");
