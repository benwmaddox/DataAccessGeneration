using CommandLine;

namespace DataAccessGeneration;

public class CommandOptions
{
    [Option('p', "path", Required = false, HelpText = "Path to the settings file.")]
    public string? Path { get; set; }
}