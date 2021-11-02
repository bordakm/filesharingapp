using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileHostingApp.BLL.Interfaces
{
    public interface IHashingService
    {
        string MD5FromStream(Stream stream);
    }
}
