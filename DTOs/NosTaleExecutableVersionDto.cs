using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NosCDN.DTOs
{
    public class NosTaleExecutableVersionDto
    {
        public NosTaleExecutableVersionMd5Dto Md5 { get; set; } = new();
        public string Version { get; set; }
    }

    public class NosTaleExecutableVersionMd5Dto
    {
        public string Client { get; set; }
        public string ClientX { get; set; }
    }
}
