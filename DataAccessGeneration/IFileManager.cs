namespace DataAccessGeneration;

public interface IFileManager
{
    void WriteFiles(string rootOutputDirectory, List<(string RelativeFilePath, string FileContent)> files);
    List<string> DeleteFiles(string outputDirectory, HashSet<string> except );
}