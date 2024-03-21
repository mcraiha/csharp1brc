using System;
using System.Globalization;

namespace Optimoitu2;

public sealed class Collected
{
	public double min;
	public double max;
	public double sum;
	public int count;

	public Collected(double firstValue)
	{
		min = firstValue;
		max = firstValue;
		sum = firstValue;
		count = 1;
	}

	public void Update(double nextValue)
	{
		min = Math.Min(min, nextValue);
		max = Math.Max(max, nextValue);
		sum += nextValue;
		count++;
	}
}

internal class Program
{
	private static Dictionary<string, Collected> data = new Dictionary<string, Collected>();

	private static double round(double value) 
	{
		return Math.Round(value * 10.0) / 10.0;
	}

	static void Main(string[] args)
	{
		if (args.Length != 1)
		{
			Console.WriteLine("Give input file as parameter");
			return;
		}

		var watch = System.Diagnostics.Stopwatch.StartNew();
		var lines = File.ReadLines(args[0]);
		foreach (var line in lines)
		{
			int index = line.IndexOf(';', 1);
			string key = line.Substring(0, index);
			double newValue = double.Parse(line.AsSpan(index + 1), CultureInfo.InvariantCulture);
			if (data.TryGetValue(key, out var collected))
			{
				collected.Update(newValue);
			}
			else
			{
				data[key] = new Collected(newValue);
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
			string resultRow = round(data[key].min).ToString("F1", CultureInfo.InvariantCulture) + "/" + round(data[key].sum / data[key].count).ToString("F1", CultureInfo.InvariantCulture) + "/" + round(data[key].max).ToString("F1", CultureInfo.InvariantCulture);
			Console.Write(resultRow);
			Console.Write(", ");
		}
		watch.Stop();
		Console.WriteLine($"Print took: {watch.Elapsed}");
	}
}
