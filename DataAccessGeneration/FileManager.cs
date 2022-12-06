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

    public List<string> DeleteFiles(string outputDirectory, HashSet<string>? except = null)
    {
        List<string> filesToDelete = new List<string>();
        except ??= new HashSet<string>();
        except = except.Select(x => Path.GetFullPath(x, outputDirectory)).ToHashSet();
        outputDirectory = Path.GetFullPath(outputDirectory);
        if (!string.IsNullOrEmpty(outputDirectory))
        {
            if (!Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }
    
            filesToDelete = Directory.GetFiles( outputDirectory, "*", SearchOption.AllDirectories)
                .Select(Path.GetFullPath)
                .Where(d => d.Contains("generated.cs") && !except.Contains(d)).ToList();
            foreach (var filePath in filesToDelete)
            {
                File.Delete(filePath);
            }
        }
    
        return filesToDelete;
    }
}