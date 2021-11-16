using FileHostingAppDesktopClient.Domain;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileHostingAppDesktopClient.Context
{
    public class FileHostingDbContext : DbContext
    {
        public DbSet<FileHistoryEntry> History { get; set; }
        public DbSet<FileEntry> Files { get; set; }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Data Source=filehosting.db");
        }
    }
}
