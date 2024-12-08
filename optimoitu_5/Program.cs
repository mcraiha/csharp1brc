using System;
using System.Text;
using System.Buffers;
using System.Buffers.Text;
using System.IO.Pipelines;

namespace Optimoitu5;

public sealed class Collected
{
	public int min;
	public int max;
	public int sum;
	public int count;

	public Collected(int firstValue)
	{
		min = firstValue;
		max = firstValue;
		sum = firstValue;
		count = 1;
	}

	public void Update(int nextValue)
	{
		min = Math.Min(min, nextValue);
		max = Math.Max(max, nextValue);
		sum += nextValue;
		count++;
	}
}

internal class Program
{
	private static readonly Dictionary<string, Collected> data = new Dictionary<string, Collected>();

	private static string IntToDecimal(int value)
	{
		if (value < 10 && value > 0)
		{
			return $"0.{value}";
		}
		else if (value > -10 && value < 0)
		{
			return $"-0.{Math.Abs(value)}";
		}
		
		return $"{value / 10}.{Math.Abs(value) % 10}";
	}

	private static string IntDecimalDivide(int divident, int divisor)
	{
		divident *= 10;
		int result = divident / divisor;
		int mod = result % 10;
		if (mod > 4)
		{
			result += (10 - mod);
		}
		else if (mod < -4)
		{
			result -= (10 + mod);
		} 
		return IntToDecimal(result/10); //$"{result / 10}.{Math.Abs(result) % 10}";
	}

	private static int ParseNumber(ReadOnlySpan<byte> s)
	{
		bool negative = false;
		int value = 0;
		for (int i = 0; i < s.Length; i++)
		{
			if (s[i] == '-')
			{
				negative = true;
			}
			else if (s[i] >= (byte)'0' && s[i] <= (byte)'9')
			{
				value *= 10;
				value += (s[i] - '0');
			}
		}

		if (negative)
		{
			value *= -1;
		}

		return value;
	}

	static void ProcessLines( Dictionary<string, Collected>.AlternateLookup<ReadOnlySpan<char>> lookup, PipeReader pipeReader, ReadResult readResult)
	{
		var sr = new SequenceReader<byte>(readResult.Buffer);
		ReadOnlySpan<byte> span;
		const int maybeCityLength = 16;
		Span<char> maybeCity = stackalloc char[maybeCityLength];

		while (true)
		{
			ReadOnlySequence<byte> line;
			if (sr.TryReadTo(out line, (byte)'\n') == false) 
			{
				pipeReader.AdvanceTo(consumed: sr.Position, examined: readResult.Buffer.End);
				break;
			}

			var lr = new SequenceReader<byte>(line);
			var shouldBeTrue = lr.TryReadTo(out span, (byte)';');

			bool useShortCut = false;
			if (span.Length <= maybeCityLength)
			{
				useShortCut = true;
				for (int i = 0; i < span.Length; i++)
				{
					if (span[i] < 127)
					{
						maybeCity[i] = (char)span[i];
					}
					else
					{
						useShortCut = false;
						break;
					}
				}
			}

			ReadOnlySpan<char> key = useShortCut ? maybeCity.Slice(0, span.Length) : Encoding.UTF8.GetString(span);

			ReadOnlySpan<byte> number = lr.UnreadSpan;

			int newValue = ParseNumber(number);

			if (lookup.TryGetValue(key, out var collected))
			{
				collected.Update(newValue);
			}
			else
			{
				lookup[key] = new Collected(newValue);
			}
		}
	}

	static async Task Main(string[] args)
	{
		if (args.Length != 1)
		{
			Console.WriteLine("Give input file as parameter");
			return;
		}

		var watch = System.Diagnostics.Stopwatch.StartNew();

		var lookup = data.GetAlternateLookup<ReadOnlySpan<char>>();
		FileStream fs = new FileStream(args[0], FileMode.Open);
		var pipeReader = PipeReader.Create(fs, new StreamPipeReaderOptions(bufferSize: 64 * 1024));
		while (true) 
		{
			var valueTask = pipeReader.ReadAsync();
			var read = valueTask.IsCompleted ? valueTask.Result : await valueTask.AsTask();
			ProcessLines(lookup, pipeReader, read);

			if (read.IsCompleted)
			{
				break;
			}
    	}

		await pipeReader.CompleteAsync();

		watch.Stop();
		Console.WriteLine($"Reading lines took: {watch.Elapsed}");

		watch.Restart();
		List<string> keys = data.Keys.ToList();
		keys.Sort();
		watch.Stop();
		Console.WriteLine($"Sorting took: {watch.Elapsed}");

		watch.Restart();
		foreach (var key in keys)
		{
			Console.Write($"{key}=");
			string resultRow = IntToDecimal(data[key].min) + "/" + IntDecimalDivide(data[key].sum, data[key].count) + "/" + IntToDecimal(data[key].max);
			Console.Write(resultRow);
			Console.Write(", ");
		}
		watch.Stop();
		Console.WriteLine($"Print took: {watch.Elapsed}");
	}
}
