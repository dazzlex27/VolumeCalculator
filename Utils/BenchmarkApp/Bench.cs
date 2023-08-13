using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using System;

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class Benchmark1
{
	//private MemoryStream ms = new MemoryStream();

	static void FakeRead(byte[] buffer, int start, int length)
	{
		for (int i = start; i < length; i++)
			buffer[i] = (byte)(i % 250);
	}

	static void FakeRead(Span<byte> buffer)
	{
		for (int i = 0; i < buffer.Length; i++)
			buffer[i] = (byte)(i % 250);
	}

	[Benchmark]
	public void AllocatingOnHeap()
	{
		var buffer = new byte[1024];
		FakeRead(buffer, 0, buffer.Length);
	}

	[Benchmark]
	public void ConvertingToSpan()
	{
		var buffer = new Span<byte>(new byte[1024]);
		FakeRead(buffer);
	}

	[Benchmark]
	public void UsingStackAlloc()
	{
		Span<byte> buffer = stackalloc byte[1024];
		FakeRead(buffer);
	}
}