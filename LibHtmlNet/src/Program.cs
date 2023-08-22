using LibHtmlNet;
using LibHtmlNet.Parser;

var byteBuffer = new ByteBuffer(File.ReadAllBytes(@"E:\projects\LibHtmlNet\Tests\html_basic_document.html"));
var tokenizer = new HTMLTokenizer(byteBuffer);

var token = tokenizer.NextToken();
while (token != null)
{
    Console.WriteLine($"Type: {token.Type}");
    token = tokenizer.NextToken();
}


