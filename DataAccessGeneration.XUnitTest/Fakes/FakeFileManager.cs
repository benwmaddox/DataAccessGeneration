using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DataAccessGeneration.XUnitTest;

public class FakeFileManager : IFileManager
{
    public Dictionary<string, string> SavedFiles = new Dictionary<string, string>();
    public void WriteFiles(string rootOutputDirectory, List<OutputFile> files)
    {
        foreach (var file in files)
        {
            var path = Path.Combine(rootOutputDirectory, file.RelativeFilePath);
            SavedFiles[path] = file.FileContent;
        }
    }

    public List<string> DeleteFiles(string rootOutputDirectory, HashSet<string> files)
    {
        foreach (var file in files)
        {
            var path = Path.Combine(rootOutputDirectory, file);
            SavedFiles.Remove(file);
        }

        return files.ToList();
    }
}