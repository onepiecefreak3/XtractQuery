using Logic.Domain.CodeAnalysis.Contract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logic.Domain.CodeAnalysis
{
    internal abstract class Buffer<T> : IBuffer<T>
    {
        private readonly IDictionary<int, T> _peeked;

        private int _currentPosition;

        public abstract bool IsEndOfInput { get; protected set; }

        public Buffer()
        {
            _peeked = new Dictionary<int, T>();
        }

        public T Peek(int position = 0)
        {
            if (position < 0)
                throw new ArgumentOutOfRangeException(nameof(position));

            if (_peeked.ContainsKey(_currentPosition + position))
                return _peeked[_currentPosition + position];

            int currentPosition = _currentPosition;
            for (int i = currentPosition; i <= currentPosition + position; i++)
                _peeked[i] = Read();
            _currentPosition -= position + 1;

            return _peeked[_currentPosition + position];
        }

        public T Read()
        {
            if (_peeked.ContainsKey(_currentPosition))
            {
                T next = _peeked[_currentPosition];
                _peeked.Remove(_currentPosition);

                _currentPosition++;
                return next;
            }

            _currentPosition++;
            return ReadInternal();
        }

        protected abstract T ReadInternal();
    }
}
