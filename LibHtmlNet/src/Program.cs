using LibHtmlNet;
using LibHtmlNet.Parser;

var byteBuffer = new ByteBuffer(File.ReadAllBytes(@"E:\projects\LibHtmlNet\Tests\html_basic_document.html"));
var tokenizer = new HTMLTokenizer(byteBuffer);

var tokens = tokenizer.Tokenize();
