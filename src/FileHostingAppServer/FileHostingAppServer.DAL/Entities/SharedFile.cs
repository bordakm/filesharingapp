using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileHostingAppServer.DAL.Entities
{
    public class SharedFile
    {
        public string OwnerUserId { get; set; }
        public bool Public { get; set; }
        public IEnumerable<Share> Shares { get; set; }
    }
}
