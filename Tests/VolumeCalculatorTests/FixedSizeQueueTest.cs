using Primitives;

namespace VolumeCalculatorTests
{
	[TestFixture]
	internal class FixedSizeQueueTest
	{
		[Test]
		public void Dequeue_WhenEnqueuedTooManyItems_RetunsTheLatestItem()
		{
			const int sequenceSize = 8;
			var items = Enumerable.Range(0, sequenceSize).ToList();
			const int queueSize = 1;

			var queue = new FixedSizeQueue<int>(queueSize);
			foreach (var item in items)
				queue.Enqueue(item);

			var expectedItem1 = queue.Dequeue();

			Assert.Multiple(() =>
			{
				Assert.That(queue.Size, Is.EqualTo(queueSize));
				Assert.That(expectedItem1, Is.EqualTo(items[sequenceSize - 1]));
			});
		}

		[Test]
		public void Dequeue_WhenEnqueuedTooManyItems_RetunsTwoLatestItems()
		{
			const int sequenceSize = 8;
			var items = Enumerable.Range(0, sequenceSize).ToList();
			const int queueSize = 2;

			var queue = new FixedSizeQueue<int>(queueSize);
			foreach (var item in items)
				queue.Enqueue(item);

			var expectedItem1 = queue.Dequeue();
			var expectedItem2 = queue.Dequeue();

			Assert.Multiple(() =>
			{
				Assert.That(queue.Size, Is.EqualTo(queueSize));
				Assert.That(expectedItem1, Is.EqualTo(items[sequenceSize - 2]));
				Assert.That(expectedItem2, Is.EqualTo(items[sequenceSize - 1]));
			});
		}

		[Test]
		public void Enqueue_WhenGivenTheNumberOfElementsBelowTheLimit_HasCorrectSize()
		{
			const int queueSize = 2;
			var queue = new FixedSizeQueue<int>(queueSize);

			queue.Enqueue(1);

			Assert.Multiple(() =>
			{
				Assert.That(queue.Size, Is.EqualTo(queueSize));
				Assert.That(queue, Has.Count.EqualTo(1));
			});
		}

		[Test]
		public void Enqueue_WhenGivenTheNumberOfElementsEqualToTheLimit_HasCorrectSize()
		{
			const int queueSize = 2;
			var queue = new FixedSizeQueue<int>(queueSize);

			queue.Enqueue(1);
			queue.Enqueue(1);

			Assert.Multiple(() =>
			{
				Assert.That(queue.Size, Is.EqualTo(queueSize));
				Assert.That(queue, Has.Count.EqualTo(queueSize));
			});
		}

		[Test]
		public void Enqueue_WhenGivenTheNumberOfElementsAboveTheLimit_HasCorrectSize()
		{
			const int queueSize = 2;
			var queue = new FixedSizeQueue<int>(queueSize);

			for (var i = 0; i < queueSize + 1; i++)
				queue.Enqueue(1);

			Assert.Multiple(() =>
			{
				Assert.That(queue.Size, Is.EqualTo(queueSize));
				Assert.That(queue, Has.Count.EqualTo(queueSize));
			});
		}
	}
}
