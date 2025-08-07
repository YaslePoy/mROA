using System;
using System.Threading;

namespace mROA.Implementation
{
    public class CircularMemoryManager
    {
        private readonly Memory<byte> _buffer;
        private Memory<byte> _current;
        private SpinLock _spinLock = new(false);

        public CircularMemoryManager(int size)
        {
            _buffer = new Memory<byte>(new byte[size]);
            _current = _buffer;
        }

        public Span<byte> AllocSlice(int size)
        {
            Span<byte> order;
            var lockTaken = false;
            try
            {
                _spinLock.Enter(ref lockTaken);
                if (_current.Length < size)
                    _current = _buffer;

                order = _current.Span[..size];
                _current = _current[size..];
            }
            finally
            {
                if (lockTaken) _spinLock.Exit(false);
            }

            return order;
        }

        public Memory<byte> AllocMemory(int size)
        {
            Memory<byte> order;
            var lockTaken = false;
            try
            {
                _spinLock.Enter(ref lockTaken);
                if (_current.Length < size)
                    _current = _buffer;

                order = _current[..size];
                _current = _current[size..];
            }
            finally
            {
                if (lockTaken) _spinLock.Exit(false);
            }

            return order;
        }
    }
}