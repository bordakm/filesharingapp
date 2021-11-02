using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileHostingAppDesktopClient.Domain
{
    public class FileMetadata
    {
        public string RelativeFileLocation { get; set; }
        public string FileName { get; set; }
        public string Hash { get; set; }
        public DateTime LastModified { get; set; }
        public string RelativePathWithFilename { get => RelativeFileLocation + FileName; }
    }
}
