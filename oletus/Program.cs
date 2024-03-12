using System;
using System.Globalization;

namespace Oletus;

public record Measurement(string station, double value) 
{
	public Measurement(string[] parts) : this(parts[0], double.Parse(parts[1], CultureInfo.InvariantCulture))
	{

	}
}

public record ResultRow(double min, double mean, double max) 
{
	public override string ToString() => round(min).ToString("F1", CultureInfo.InvariantCulture) + "/" + round(mean).ToString("F1", CultureInfo.InvariantCulture) + "/" + round(max).ToString("F1", CultureInfo.InvariantCulture);

	private static double round(double value) 
	{
		return Math.Round(value * 10.0) / 10.0;
	}
}

internal class Program
{
	static void Main(string[] args)
	{
		if (args.Length != 1)
		{
			Console.WriteLine("Give input file as parameter");
			return;
		}

		List<Measurement> measurements = new List<Measurement>();

		var watch = System.Diagnostics.Stopwatch.StartNew();
		var lines = File.ReadLines(args[0]);
		foreach (var line in lines)
		{
			measurements.Add(new Measurement(line.Split(';')));
		}
		watch.Stop();
		Console.WriteLine($"Reading lines took: {watch.Elapsed}");

		watch.Restart();
		IEnumerable<IGrouping<string, Measurement>> query = measurements.GroupBy(measurements => measurements.station).OrderBy(a => a.Key);
		watch.Stop();
		Console.WriteLine($"Grouping took: {watch.Elapsed}");

		watch.Restart();
		foreach (var group in query)
		{
			Console.Write($"{group.Key}=");
			ResultRow resultRow = new ResultRow(group.Min(g => g.value), (Math.Round(group.Sum(g => g.value) * 10.0) / 10.0) / group.Count(), group.Max(g => g.value));
			Console.Write(resultRow);
			Console.Write(", ");
		}
		watch.Stop();
		Console.WriteLine($"Aggregation + print took: {watch.Elapsed}");
	}
}
