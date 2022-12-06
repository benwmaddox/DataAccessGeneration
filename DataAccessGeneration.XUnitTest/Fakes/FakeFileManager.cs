using System.Collections.Generic;
using System.IO;

namespace DataAccessGeneration.XUnitTest;

public class FakeFileManager : IFileManager
{
    public Dictionary<string, string> SavedFiles = new Dictionary<string, string>();
    public void WriteFiles(string rootOutputDirectory, List<(string RelativeFilePath, string FileContent)> files)
    {
        foreach (var file in files)
        {
            var path = Path.Combine(rootOutputDirectory, file.RelativeFilePath);
            SavedFiles[path] = file.FileContent;
        }
    }
}