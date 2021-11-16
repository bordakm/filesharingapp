using FileHostingApp.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using System;

namespace FileHostingApp.DAL.DbContexts
{
    public class FileHostingDbContext : DbContext
    {
        public DbSet<FileHistoryEntry> History { get; set; }
        public FileHostingDbContext(DbContextOptions<FileHostingDbContext> options)
        : base(options)
        { }
    }
}
