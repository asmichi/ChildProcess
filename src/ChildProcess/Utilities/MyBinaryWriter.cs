// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

using System;
using System.Buffers;
using System.Diagnostics;
using System.Text;

namespace Asmichi.Utilities
{
    /// <summary>
    /// Generate a byte array with native endianness.
    /// </summary>
    internal ref struct MyBinaryWriter
    {
        private byte[] _buf;
        private int _pos;

        public MyBinaryWriter(int initialCapacity)
        {
            Debug.Assert(initialCapacity >= sizeof(int));
            _buf = ArrayPool<byte>.Shared.Rent(initialCapacity);
            _pos = 0;
        }

        public void Dispose()
        {
            if (_buf != null)
            {
                ArrayPool<byte>.Shared.Return(_buf);
                _buf = null!;
            }
        }

        public long Length => _pos;

        public ReadOnlySpan<byte> GetBuffer() => _buf.AsSpan(0, _pos);

        public void RewritePrefixedLength()
        {
            Debug.Assert(_pos >= sizeof(int));
            WriteBytes(_buf.AsSpan(0, sizeof(int)), (uint)(_pos - sizeof(int)));
        }

        public void Write(uint value)
        {
            const int size = sizeof(uint);
            WriteBytes(EnsureBufferFor(size), value);
            _pos += size;
        }

        public void Write(long value)
        {
            const int size = sizeof(long);
            WriteBytes(EnsureBufferFor(size), value);
            _pos += size;
        }

        // Write a length-prefixed NUL-terminated sequence of UTF-8 code units.
        public void Write(string? value)
        {
            if (value == null)
            {
                Write(0U);
                return;
            }

            var utf8 = Encoding.UTF8;
            var maxByteCount = sizeof(uint) + utf8.GetMaxByteCount(value.Length) + 1;
            var strBuf = EnsureBufferFor(maxByteCount);
            var actualByteCount = WriteBytesWithNul(strBuf.Slice(sizeof(uint)), value);
            WriteBytes(strBuf, (uint)actualByteCount);
            _pos += sizeof(uint) + actualByteCount;
        }

        public void WriteEnvironmentVariable(string name, string value)
        {
            var utf8 = Encoding.UTF8;
            var maxByteCount = sizeof(uint) + utf8.GetMaxByteCount(name.Length) + utf8.GetMaxByteCount(value.Length) + 1 + 1;

            var strBuf = EnsureBufferFor(maxByteCount);
            var actualByteCount1 = utf8.GetBytes(name.AsSpan(), strBuf.Slice(sizeof(uint)));
            strBuf[sizeof(uint) + actualByteCount1] = (byte)'=';
            var actualByteCount2 = WriteBytesWithNul(strBuf.Slice(sizeof(uint) + actualByteCount1 + 1), value);
            var totalActualByteCount = actualByteCount1 + 1 + actualByteCount2;
            WriteBytes(strBuf, (uint)totalActualByteCount);

            _pos += sizeof(uint) + totalActualByteCount;
        }

        private static void WriteBytes(Span<byte> destination, uint value)
        {
            bool successful = BitConverter.TryWriteBytes(destination, value);
            Debug.Assert(successful);
        }

        private static void WriteBytes(Span<byte> destination, long value)
        {
            bool successful = BitConverter.TryWriteBytes(destination, value);
            Debug.Assert(successful);
        }

        private static int WriteBytesWithNul(Span<byte> destination, string value)
        {
            var n = Encoding.UTF8.GetBytes(value.AsSpan(), destination);
            destination[n] = 0;
            return n + 1;
        }

        private Span<byte> EnsureBufferFor(int requestedFreeBytes)
        {
            int capacity = _pos + requestedFreeBytes;
            if (_buf.Length < capacity)
            {
                var newBuffer = ArrayPool<byte>.Shared.Rent(capacity);
                Array.Copy(_buf, newBuffer, _pos);
                ArrayPool<byte>.Shared.Return(_buf);
                _buf = newBuffer;
            }

            return _buf.AsSpan(_pos, _buf.Length - _pos);
        }
    }
}
