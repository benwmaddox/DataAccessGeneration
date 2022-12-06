namespace DataAccessGeneration;

public interface IFileManager
{
    void WriteFiles(string rootOutputDirectory, List<(string RelativeFilePath, string FileContent)> files);
}