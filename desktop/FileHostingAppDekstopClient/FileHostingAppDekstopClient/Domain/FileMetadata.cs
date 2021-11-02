using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileHostingAppDekstopClient.Domain
{
    public class FileMetadata
    {
        public string FullFilePath { get; set; }
        public string FileName { get; set; }
        public string Hash { get; set; }
        public DateTime LastModified { get; set; }
    }
}
