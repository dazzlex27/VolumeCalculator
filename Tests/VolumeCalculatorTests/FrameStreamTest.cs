using DeviceIntegration.FrameProviders;
using Primitives.Logging;
using System.Threading;

namespace VolumeCalculatorTests
{
	[TestFixture]
	internal class FrameStreamTest
	{
		[Test]
		public void Ctor_WhenCreatingWithDefaultParams_ReturnsNewObjectWithFpsSetToMinusOne()
		{
			var frameStream = CreateStringFrameStream();

			Assert.That(frameStream.Fps, Is.EqualTo(-1));
		}

		[Test]
		public void Ctor_WhenCreatingWithDefaultParams_ReturnsNewObjectThatIsNotSuspended()
		{
			var frameStream = CreateStringFrameStream();

			Assert.That(frameStream.IsSuspended, Is.False);
		}

		[Test]
		public void PushFrame_WhenHavingASubscriber_RaisesFrameReadyEvent()
		{
			var frameStream = CreateStringFrameStream();
			var frame = "";
			var gtFrame = "test";
			var frameCallback = (string s) => { frame = s; };

			frameStream.FrameReady += frameCallback;
			frameStream.PushFrame(gtFrame);
			Thread.Sleep(500);

			Assert.That(frame, Is.EqualTo(gtFrame));
		}

		[Test]
		public void Suspend_WhenRunning_MakesSuspended()
		{
			var frameStream = CreateStringFrameStream();

			frameStream.Suspend();

			Assert.That(frameStream.IsSuspended, Is.True);
		}

		[Test]
		public void Resumed_WhenSuspended_MakesRunning()
		{
			var frameStream = CreateStringFrameStream();

			frameStream.Suspend();
			frameStream.Resume();

			Assert.That(frameStream.IsSuspended, Is.False);
		}


		[Test]
		public void NeedUnrestrictedFrame_WhenNoSubscribers_ReturnsFalse()
		{
			var frameStream = CreateStringFrameStream();

			Assert.That(frameStream.NeedUnrestrictedFrame, Is.False);
		}

		[Test]
		public void NeedRestrictedFrame_WhenNoSubscribers_ReturnsFalse()
		{
			var frameStream = CreateStringFrameStream();

			Assert.That(frameStream.NeedRestrictedFrame, Is.False);
		}

		[Test]
		public void NeedAnydFrame_WhenNoSubscribers_ReturnsFalse()
		{
			var frameStream = CreateStringFrameStream();

			Assert.That(frameStream.NeedAnyFrame, Is.False);
		}

		[Test]
		public void NeedUnrestrictedFrame_WhenGivenFrameWithUnrestrictedSubscriber_ReturnsTrue()
		{
			var frameStream = CreateStringFrameStream();
			var frameCallback = (string s) => { };

			frameStream.UnrestrictedFrameReady += frameCallback;
			frameStream.Suspend();
			frameStream.PushFrame("test");

			Assert.That(frameStream.NeedUnrestrictedFrame, Is.True);
		}

		[Test]
		public void NeedUnrestrictedFrame_WhenGivenFrameWithRestrictedSubscriber_ReturnsFalse()
		{
			var frameStream = CreateStringFrameStream();
			var frameCallback = (string s) => { };

			frameStream.FrameReady += frameCallback;
			frameStream.Suspend();
			frameStream.PushFrame("test");

			Assert.That(frameStream.NeedUnrestrictedFrame, Is.False);
		}

		[Test]
		public void NeedAnyFrame_WhenGivenFrameWithUnrestrictedSubscriber_ReturnsTrue()
		{
			var frameStream = CreateStringFrameStream();
			var frameCallback = (string s) => { };

			frameStream.UnrestrictedFrameReady += frameCallback;
			frameStream.Suspend();
			frameStream.PushFrame("test");

			Assert.That(frameStream.NeedAnyFrame, Is.True);
		}

		[Test]
		public void NeedAnyFrame_WhenGivenFrameWithRestrictedSubscriber_ReturnsTrue()
		{
			var frameStream = CreateStringFrameStream();
			var frameCallback = (string s) => { };

			frameStream.FrameReady += frameCallback;
			frameStream.PushFrame("test");
			Thread.Sleep(20);

			Assert.That(frameStream.NeedAnyFrame, Is.True);
		}

		[Test]
		public void NeedAnyFrame_WhenGivenFrameWithBothRestrictedAndUnrestrictedSubscriber_ReturnsTrue()
		{
			var frameStream = CreateStringFrameStream();
			var frameCallback = (string s) => { };

			frameStream.FrameReady += frameCallback;
			frameStream.UnrestrictedFrameReady += frameCallback;
			frameStream.Suspend(); // TODO: why isn't unrestricted stream suspended?
			frameStream.PushFrame("test");

			Assert.That(frameStream.NeedAnyFrame, Is.True);
		}

		[Test]
		public void NeedRestrictedFrame_WhenGivenFrameWithRestrictedSubscriberAndNoFpsLimit_ReturnsTrue()
		{
			var frameStream = CreateStringFrameStream();
			var frameCallback = (string s) => { };

			frameStream.Fps = -1;
			frameStream.FrameReady += frameCallback;
			frameStream.PushFrame("test");
			Thread.Sleep(20);

			Assert.That(frameStream.NeedRestrictedFrame, Is.True);
		}

		[Test]
		public void NeedRestrictedFrame_WhenGivenFrameWithRestrictedSubscriberAndFpsLimit_ReturnsFalse()
		{
			var frameStream = CreateStringFrameStream();
			var frameCallback = (string s) => { };

			frameStream.Fps = 1;
			frameStream.FrameReady += frameCallback;
			frameStream.Suspend();
			frameStream.PushFrame("test");

			Assert.That(frameStream.NeedRestrictedFrame, Is.False);
		}

		private static FrameStream<string> CreateStringFrameStream()
		{
			var token = new CancellationToken();
			return new FrameStream<string>(new DummyLogger(), "", token);
		}
	}
}
