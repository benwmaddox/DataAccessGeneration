using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using CommandLine;
using CommandLine.Text;

namespace DataAccessGeneration
{
	public partial class Program
	{
	    public const string VERSION = "2022-12-13 V1.17";
		public static void Main(string[] args)
		{
			var parser = Parser.Default;
			var parserResults = parser.ParseArguments<CommandOptions>(args);
			parserResults
				.WithParsed(Run)
				.WithNotParsed(errors => { });
		}

		private static void Run(CommandOptions CLIOptions)
		{
			Stopwatch stopwatch = new Stopwatch();
			stopwatch.Start();
			
			var settingPath = CLIOptions.Path 
			                  ?? Path.Join(Path.GetDirectoryName(System.AppContext.BaseDirectory)!, "DataAccessGenerationSettings.json");
			if (!File.Exists(settingPath))
			{
				throw new Exception("Could not find settings file: \n" + settingPath);	
			}
			var settingJson = File.ReadAllText(settingPath);
			var settingsList = JsonSerializer.Deserialize<List<Settings>>(settingJson, options: new JsonSerializerOptions()
			                   {
				                   AllowTrailingCommas = true,
				                   Converters = { new JsonStringEnumConverter() }
			                   })
			                   ?? throw new ArgumentException("Missing DataAccessGenerationSettings.json file");
			var fileManager = new FileManager();

			var generators = settingsList.Select(x => new Generator(fileManager)).ToList();
			var timer = new Timer((callback) =>
			{
				PrintStatus(generators, stopwatch);
			}, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));

			Parallel.ForEach(settingsList, (settings, state, index) =>
			{
				try
				{
					var path = Path.Join(Path.GetDirectoryName(settingPath), settings.OutputRelativePath);
					generators[(int)index].Generate(settings, new DataLookup(settings.ConnectionString), path);
				}
				catch (Exception e)
				{
					generators[(int)index].Errors.Add(e.Message + e.StackTrace);
				}
			});
			
			stopwatch.Stop();
			timer.Dispose();
			PrintStatus(generators, stopwatch);
			Console.WriteLine("Press enter to exit");
			var value = Console.ReadLine();
		}
		
		private static void PrintStatus(List<Generator> generators, Stopwatch stopwatch)
		{
			Console.Clear();
			for (var index = 0; index < generators.Count; index++)
			{
				var generator = generators[index];
				Console.WriteLine($"{index + 1}: {generator.CurrentActivity}");
				if (generator.Errors.Any())
				{
					Console.WriteLine("  Errors:");
					foreach (var error in generator.Errors)
					{
						Console.WriteLine("    " + error);
						Console.WriteLine();
					}
					Console.WriteLine();
				}
			}
			Console.WriteLine($"Generation run time: {stopwatch.ElapsedMilliseconds:N0} ms");
		}

	}
}