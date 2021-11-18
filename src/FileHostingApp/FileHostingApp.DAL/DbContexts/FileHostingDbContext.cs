using FileHostingApp.DAL.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;

namespace FileHostingApp.DAL.DbContexts
{
    public class FileHostingDbContext : IdentityDbContext<IdentityUser>
    {
        public DbSet<FileHistoryEntry> History { get; set; }
        public FileHostingDbContext(DbContextOptions<FileHostingDbContext> options)
        : base(options)
        { }
    }
}
