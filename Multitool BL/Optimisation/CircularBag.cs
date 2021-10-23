namespace Multitool.Optimisation
{
    public class CircularBag<T>
    {
        private readonly object _lock = new();
        private readonly T[] buffer;
        private readonly int capacity;
        private int head;

        public CircularBag(int capacity)
        {
            buffer = new T[capacity];
            this.capacity = capacity;
        }

        public int Length => buffer.Length;

        public bool Full => Length == capacity;

        public T this[int index] => buffer[index];

        public void Add(T value)
        {
            lock (_lock)
            {
                buffer[head] = value;
                head = (head + 1) % capacity;
            }
        }
    }
}
