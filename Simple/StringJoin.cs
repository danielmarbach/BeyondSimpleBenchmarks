using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Jobs;
using System;
using System.Text;
using System.Linq;

[SimpleJob]
[MemoryDiagnoser]
public class StringJoinBenchmarks {

  [Benchmark]
  public string StringJoin() {
    return string.Join(", ", Enumerable.Range(0, 10).Select(i => i.ToString()));
  }

  [Benchmark]
  public string StringBuilder() {
    var sb = new StringBuilder();
    for (int i = 0; i < 10; i++)
    {
        sb.Append(i);
        sb.Append(", ");
    }

    return sb.ToString(0, sb.Length - 2);
  }

  [Benchmark]
  public string ValueStringBuilder() {
    var seperator = new ReadOnlySpan<char>(new char[] { ',', ' '});
    using var sb = new ValueStringBuilder(stackalloc char[30]);
    for (int i = 0; i < 10; i++)
    {
        sb.Append(i);
        sb.Append(seperator);
    }

    return sb.AsSpan(0, sb.Length - 2).ToString();
  }
}