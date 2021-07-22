﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NosCDN.DTOs
{
    public class NosTalePatchEntryDto
    {
        public string Path { get; set; }
        public string Sha1 { get; set; }
        public string File { get; set; }
        public uint Flags { get; set; }
        public ulong Size { get; set; }
        public bool Folder { get; set; }
    }
}
