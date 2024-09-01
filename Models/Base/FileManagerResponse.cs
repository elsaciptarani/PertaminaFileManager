using Syncfusion.Blazor.FileManager;

namespace PertaminaFileManager.Models.Base
{

    public class FileManagerResponse
    {
        public FileManagerDirectoryContent CWD { get; set; }
        public IEnumerable<FileManagerDirectoryContent> Files { get; set; }

        public ErrorDetails Error { get; set; }

        public FileDetails Details { get; set; }

    }

}