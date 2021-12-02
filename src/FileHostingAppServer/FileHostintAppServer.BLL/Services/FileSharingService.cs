using FileHostingAppServer.DAL.DbContexts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FileHostingAppServer.BLL.Services
{
    public class FileSharingService
    {
        private readonly FileHostingDbContext _dbContext;

        public FileSharingService(FileHostingDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public Task ShareFileWithUserAsync(CancellationToken cancellationToken)
        {
            return null;
        }

        public Task MakeFilePublicAsync(CancellationToken cancellationToken)
        {
            return null;
        }
    }
}
