﻿using System.Diagnostics.Contracts;
using System.Text;

namespace HTML_NET;

public class ByteBuffer
{
    private readonly byte[] _data;

    public ByteBuffer(byte[] data)
    {
        Position = 0;
        Length = data.Length;
        _data = data;
    }

    public long Length { get; }
    public long Position { get; private set; }

    [Pure]
    public bool IsEndOfBuffer()
    {
        return !CanPeekByte(1);
    }

    [Pure]
    public Span<byte> PeekRemainingBytes()
    {
        UnreadByte();
        var count = Math.Max(Length - Position, 25);
        return PeekBytes((int)count);
    }

    [Pure]
    public bool MatchCaseInsensitiveString(string word)
    {
        UnreadByte();
        var bytes = PeekBytes(word.Length);
        var str = Encoding.UTF8.GetString(bytes);
        return string.Equals(str, word, StringComparison.OrdinalIgnoreCase);
    }

    [Pure]
    public byte PeekByte(int offset = 0)
    {
        AssertRead();
        return _data[Position + offset];
    }

    [Pure]
    private Span<byte> PeekBytes(int count)
    {
        AssertRead(count);
        return new Span<byte>(_data, (int)Position, count);
    }
    
    public void UnreadByte()
    {
        if (Position <= 0)
            throw new ArgumentOutOfRangeException(nameof(Position), "Cannot unread byte under position 0");
        Position--;
    }

    public void Skip(int count)
    {
        AssertRead(count);
        Position += count;
    }

    [Pure]
    public byte ReadByte()
    {
        AssertRead();
        return _data[Position++];
    }

    [Pure]
    private bool CanPeekByte(int count)
    {
        return Position + count <= Length;
    }

    private void AssertRead(int count = 1)
    {
        if (!CanPeekByte(count))
            throw new ArgumentOutOfRangeException(nameof(Position), "Cannot read past the end of the buffer");
    }
}