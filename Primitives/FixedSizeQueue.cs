using System.Collections.Concurrent;

namespace Primitives
{
	public class FixedSizeQueue<T>
	{
		private readonly ConcurrentQueue<T> _internalQueue;

		public int Size { get; }

		public FixedSizeQueue(int size)
		{
			Size = size;
			_internalQueue = new ConcurrentQueue<T>();
		}

		public void Enqueue(T item)
		{
			_internalQueue.Enqueue(item);

			while (_internalQueue.Count > Size)
				_internalQueue.TryDequeue(out _);
		}

		public T Dequeue()
		{
			var success = _internalQueue.TryDequeue(out var value);

			return success ? value : default;
		}
	}
}