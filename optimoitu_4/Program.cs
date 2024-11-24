using System;

namespace Optimoitu4;

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

	private static int ParseNumber(ReadOnlySpan<char> s)
	{
		bool negative = false;
		int value = 0;
		for (int i = 0; i < s.Length; i++)
		{
			char temp = s[i];
			if (temp == '-')
			{
				negative = true;
			}
			else if (temp >= '0' && temp <= '9')
			{
				value *= 10;
				value += (temp - '0');
			}
		}

		if (negative)
		{
			value *= -1;
		}

		return value;
	}

	static void Main(string[] args)
	{
		if (args.Length != 1)
		{
			Console.WriteLine("Give input file as parameter");
			return;
		}

		var watch = System.Diagnostics.Stopwatch.StartNew();

		var lookup = data.GetAlternateLookup<ReadOnlySpan<char>>();
		var lines = File.ReadLines(args[0]);
		foreach (var line in lines)
		{
			int index = line.IndexOf(';', 1);
			ReadOnlySpan<char> key = line.AsSpan(0, index);
			int newValue = ParseNumber(line.AsSpan(index + 1));
			if (lookup.TryGetValue(key, out var collected))
			{
				collected.Update(newValue);
			}
			else
			{
				lookup[key] = new Collected(newValue);
			}
		}
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
