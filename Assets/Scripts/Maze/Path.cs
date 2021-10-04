using System.Collections.Generic;
using System.Linq;

namespace QTea.MazeGeneration
{
    public struct Path<T>
    {
        public int Count => count;

        private IEnumerable<T> enumerable;
        private readonly int count;

        private readonly IEnumerator<T> enumerator;
        private int currentElement;

        public Path(IEnumerable<T> enumerable, int count)
        {
            this.enumerable = enumerable;
            this.count = count;
            enumerator = enumerable.GetEnumerator();
            enumerator.MoveNext();
            currentElement = 0;
        }

        public Path(IEnumerable<T> enumerable) : this(enumerable, enumerable.Count())
        {
        }

        public T Next()
        {
            T current = enumerator.Current;
            enumerator.MoveNext();
            currentElement++;
            return current;
        }

        public bool TryNext(out T element)
        {
            if (currentElement >= count)
            {
                element = default;
                return false;
            }

            element = Next();
            return true;
        }

        public T Peek()
        {
            return enumerator.Current;
        }

        /// <summary>
        /// Reset the Path back to the start.
        /// </summary>
        public void Reset()
        {
            enumerator.Reset();
            enumerator.MoveNext();
            currentElement = 0;
        }

        public Path<T> Clone()
        {
            return new Path<T>(enumerable, count);
        }
    }
}