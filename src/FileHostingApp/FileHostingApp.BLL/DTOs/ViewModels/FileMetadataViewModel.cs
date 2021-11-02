using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileHostingApp.BLL.DTOs.ViewModels
{
    public class FileMetadataViewModel
    {
        public string RelativePathWithFilename { get; set; }
        public string Hash { get; set; }
        public DateTime LastModified { get; set; }
        public string RelativeFileLocation
        {
            get => RelativePathWithFilename.Replace(FileName, "");
        }
        public string FileName
        {
            get => RelativePathWithFilename.Split("/").Last();
        }
    }
}
