using System.Net;
using System.Text;

namespace LibHtmlNet;

public class ByteBuffer
{
    private long _position;
    
    private readonly byte[] _data;
    public long Length { get; }
    
    public ByteBuffer(byte[] data)
    {
        _position = 0;
        Length = data.Length;
        _data = data;
    }
    
    public static ByteBuffer FromFile(string path) => new (File.ReadAllBytes(path));

    public static ByteBuffer FromWebsite(string url)
    {
        var httpClient = new HttpClient();
        httpClient.GetAsync(new Uri(url)).Wait();
        var response = httpClient.GetByteArrayAsync(new Uri(url)).Result;
        return new ByteBuffer(response);
    }
    
    private bool CanPeek(int count) => _position + count <= Length;
    
    public byte PeekByte()
    {
        ValidateRead();
        return _data[_position];
    }
    
    public byte[] PeekBytes(int count)
    {
        ValidateRead(count);
        var result = new byte[count];
        Array.Copy(_data, _position, result, 0, count);
        return result;
    }

    public bool IsEndOfBuffer() => !CanPeek(1);
    private void ValidateRead(int count = 1)
    {
        if (CanPeek(count)) return;
        // TODO: Implement EndOfBufferException or something like that
        throw new Exception("End of buffer");
    }

    public bool MatchCaseInsensitiveString(string word)
    {
        UnreadByte();
        var count = word.Length;
        var bytes = PeekBytes(count);
        var str = Encoding.UTF8.GetString(bytes);
        return string.Equals(str, word, StringComparison.OrdinalIgnoreCase);
    }
    
    public void UnreadByte()
    {
        // TODO: Implement BufferUnderflowException or something like that
        if (_position <= 0) throw new Exception("Cannot unread byte");
        _position--;
    }
    
    public byte ReadByte()
    {
        ValidateRead();
        return _data[_position++];
    }

    public byte[] ReadBytes(int count)
    {
        ValidateRead(count);
        var result = new byte[count];
        Array.Copy(_data, _position, result, 0, count);
        _position += count;
        return result;
    }
}