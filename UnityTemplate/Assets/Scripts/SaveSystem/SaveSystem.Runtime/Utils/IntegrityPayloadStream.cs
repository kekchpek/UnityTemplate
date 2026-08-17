using System;
using System.IO;

namespace kekchpek.SaveSystem.Utils
{
    /// <summary>
    /// Read-only view of a stream that optionally skips a leading prefix and excludes trailing bytes (e.g. integrity hash).
    /// Does not take ownership of the inner stream: disposing this wrapper does not dispose <paramref name="innerStream"/>.
    /// </summary>
    public sealed class IntegrityPayloadStream : Stream
    {
        private readonly Stream _innerStream;
        private readonly long _leadingByteCount;
        private readonly long _payloadLength;
        private long _position;

        /// <param name="leadingByteCount">Bytes to hide at the start (e.g. save format version written before codec data).</param>
        public IntegrityPayloadStream(Stream innerStream, int trailingByteCount)
        {
            if (innerStream == null)
            {
                throw new ArgumentNullException(nameof(innerStream));
            }

            if (!innerStream.CanRead)
            {
                throw new ArgumentException("Inner stream must be readable.", nameof(innerStream));
            }

            if (trailingByteCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(trailingByteCount));
            }

            var leadingByteCount = innerStream.Position;

            if (innerStream.Length < (long)leadingByteCount + trailingByteCount)
            {
                throw new ArgumentException("Inner stream is shorter than the excluded leading and trailing byte counts.", nameof(innerStream));
            }

            _innerStream = innerStream;
            _leadingByteCount = leadingByteCount;
            _payloadLength = innerStream.Length - trailingByteCount - leadingByteCount;
        }

        private long InnerPositionForVirtual(long virtualPosition)
        {
            return _leadingByteCount + virtualPosition;
        }

        public override bool CanRead => true;

        public override bool CanSeek => _innerStream.CanSeek;

        public override bool CanWrite => false;

        public override long Length => _payloadLength;

        public override long Position
        {
            get => _position;
            set
            {
                if (!CanSeek)
                {
                    throw new NotSupportedException();
                }

                if (value < 0 || value > _payloadLength)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }

                _position = value;
                _innerStream.Position = InnerPositionForVirtual(value);
            }
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (count == 0)
            {
                return 0;
            }

            long remaining = _payloadLength - _position;
            if (remaining <= 0)
            {
                return 0;
            }

            int toRead = (int)Math.Min(count, remaining);
            long innerPos = InnerPositionForVirtual(_position);
            if (_innerStream.Position != innerPos)
            {
                _innerStream.Position = innerPos;
            }

            int read = _innerStream.Read(buffer, offset, toRead);
            _position += read;
            return read;
        }

        public override int ReadByte()
        {
            if (_position >= _payloadLength)
            {
                return -1;
            }

            long innerPos = InnerPositionForVirtual(_position);
            if (_innerStream.Position != innerPos)
            {
                _innerStream.Position = innerPos;
            }

            int b = _innerStream.ReadByte();
            if (b >= 0)
            {
                _position++;
            }

            return b;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            if (!CanSeek)
            {
                throw new NotSupportedException();
            }

            long newPosition = origin switch
            {
                SeekOrigin.Begin => offset,
                SeekOrigin.Current => _position + offset,
                SeekOrigin.End => _payloadLength + offset,
                _ => throw new ArgumentOutOfRangeException(nameof(origin))
            };

            if (newPosition < 0 || newPosition > _payloadLength)
            {
                throw new IOException("Attempted to seek outside the payload range.");
            }

            _position = newPosition;
            _innerStream.Position = InnerPositionForVirtual(newPosition);
            return _position;
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }
    }
}
