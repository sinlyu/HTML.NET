using LibHtmlNet;

var byteBuffer = ByteBuffer.FromFile("/home/utherali/RiderProjects/LibHtmlNet/Tests/html_basic_document.html");
var tokenizer = new HtmlTokenizer(byteBuffer);
tokenizer.Tokenize();