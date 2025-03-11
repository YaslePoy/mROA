using System;
using System.Collections.Generic;
using System.Linq;
using mROA.Abstract;

namespace mROA.Implementation
{
    public class ExtensibleStorage<T> : IStorage<T> where T : class
    {
        private const int StartupSize = 1024;
        private const int GrowSize = 128;
        private T?[] _array = new T?[StartupSize];
        private readonly LinkedList<int> _freePlaces = new(Enumerable.Range(0, StartupSize));

        public T? GetValue(int index)
        {
            if (index < 0 || index >= _array.Length)
            {
                return null;
            }
            return _array[index];
        }

        public int GetIndex(T value)
        {
            return Array.IndexOf(_array, value);
        }

        public int Place(T value)
        {
            if (_freePlaces.Count == 0)
            {
                Grow();
            }

            var index = _freePlaces.First.Value;

            _freePlaces.RemoveFirst();
            _array[index] = value;

            return index;
        }

        private void Grow()
        {
            foreach (var index in Enumerable.Range(_array.Length, GrowSize))
                _freePlaces.AddLast(index);

            T?[] nextStorage = new T[_array.Length + GrowSize];
            Array.Copy(_array, nextStorage, _array.Length);
            _array = nextStorage;
        }

        public void Free(int index)
        {
            _freePlaces.AddFirst(index);
            _array[index] = default;
        }
    }
}