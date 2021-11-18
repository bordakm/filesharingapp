using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileHostingApp.BLL.Interfaces
{
    public interface IIdentityService
    {
        IdentityUser GetCurrentUser();
        string GetCurrentUserId();
    }
}
