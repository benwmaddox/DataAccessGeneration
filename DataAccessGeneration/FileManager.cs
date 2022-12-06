namespace DataAccessGeneration;

public class FileManager : IFileManager
{
    public void WriteFiles(string rootOutputDirectory, List<(string RelativeFilePath, string FileContent)> files)
    {
        if (!Directory.Exists(rootOutputDirectory))
        {
            Directory.CreateDirectory(rootOutputDirectory);
        }
        foreach (var item in files)
        {
            var fileContent = item.FileContent.IndentBasedOnBraces().StandardizeExtraNewlines();
            var filePath = Path.Join(rootOutputDirectory, item.RelativeFilePath);
            var relativePathDirectory = Path.GetDirectoryName(filePath); 
            if (relativePathDirectory != null && !Directory.Exists(Path.GetDirectoryName(filePath)))
            {                
                Directory.CreateDirectory(relativePathDirectory);
            }
            if (!File.Exists(filePath) || File.ReadAllText(filePath) != fileContent)
            {
                File.WriteAllText(filePath, fileContent);
            }
        }
    }

}