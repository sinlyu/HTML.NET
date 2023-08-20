using LibHtmlNet;

var byteBuffer = ByteBuffer.FromFile(@"E:\projects\LibHtmlNet\Tests\html_basic_document.html");
var tokenizer = new Tokenizer(byteBuffer);
tokenizer.Tokenize();