
using HTML_NET;
using HTML_NET.Parser;

var tokenizer = new HTMLTokenizer(new ByteBuffer(File.ReadAllBytes(@"E:\projects\LibHtmlNet\Tests\html_basic_document.html")));
var token = tokenizer.NextToken();

Console.WriteLine($"First token type: {token.Type}");