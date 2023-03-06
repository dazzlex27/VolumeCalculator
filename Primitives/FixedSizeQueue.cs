using System;
using System.Collections.Concurrent;

namespace Primitives
{
	public sealed class FixedSizeQueue<T> : IDisposable
	{
		private readonly BlockingCollection<T> _internalQueue;

		public int Size { get; }

		public int Count => _internalQueue.Count;

		public FixedSizeQueue(int size)
		{
			Size = size;
			_internalQueue = new BlockingCollection<T>();
		}

		public void Dispose()
		{
			if (_internalQueue != null)
				_internalQueue.Dispose();
		}

		public void Enqueue(T item)
		{
			_internalQueue.Add(item);

			while (_internalQueue.Count > Size)
				_internalQueue.Take();
		}

		public T Dequeue()
		{
			return _internalQueue.Take();
		}
	}
}
