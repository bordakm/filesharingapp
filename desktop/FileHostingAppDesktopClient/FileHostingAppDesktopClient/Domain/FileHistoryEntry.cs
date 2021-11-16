using FileHostingAppDesktopClient.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileHostingAppDesktopClient.Domain
{
    public class FileHistoryEntry
    {
        [Key]
        public int Id { get; set; }
        public string Path { get; set; }
        public string Hash { get; set; }
        public DateTime? Timestamp { get; set; }
        public FileAction Action { get; set; }
    }
}
