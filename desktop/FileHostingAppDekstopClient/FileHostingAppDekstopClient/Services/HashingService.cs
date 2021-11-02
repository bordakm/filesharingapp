using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileHostingAppDekstopClient.Services
{
    public class HashingService
    {
        public string MD5FromStream(Stream stream)
        {
            var md5 = System.Security.Cryptography.MD5.Create();
            var hashBytes = md5.ComputeHash(stream);
            stream.Position = 0;

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < hashBytes.Length; i++)
            {
                sb.Append(hashBytes[i].ToString("X2"));
            }
            return sb.ToString();
        }
    }
}
