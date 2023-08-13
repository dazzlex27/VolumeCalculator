using BenchmarkDotNet.Running;
using System;

var summary = BenchmarkRunner.Run<Benchmark1>();
Console.WriteLine(summary);