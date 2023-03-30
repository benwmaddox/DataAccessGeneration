namespace DataAccessGeneration;

public interface IFileManager
{
    void WriteFiles(string rootOutputDirectory, List<OutputFile> files);
    List<string> DeleteFiles(string outputDirectory, HashSet<string> except );
}