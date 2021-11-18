using FileHostingApp.BLL.Interfaces;
using FileHostingApp.DAL.DbContexts;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileHostingApp.BLL.Services
{
    public class IdentityService : IIdentityService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly FileHostingDbContext _dbContext;

        public IdentityService(
            IHttpContextAccessor httpContextAccessor,
            FileHostingDbContext dbContext)
        {
            _httpContextAccessor = httpContextAccessor;
            _dbContext = dbContext;
        }
        public IdentityUser GetCurrentUser()
        {
            if (!_httpContextAccessor.HttpContext.User.Identity.IsAuthenticated) return null;
            return _dbContext.Users.FirstOrDefault(x => x.UserName == _httpContextAccessor.HttpContext.User.Identity.Name);
        }


        public string GetCurrentUserId()
        {
            return GetCurrentUser().Id;
        }
    }
}
