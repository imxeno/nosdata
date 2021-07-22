using System;
using System.Collections.Generic;
using NosCDN.DTOs;

namespace NosCDN.DTOs {
    public class NosTalePatchDto
    {
        public List<NosTalePatchEntryDto> Entries { get; set; }
        public UInt64 TotalSize { get; set; }
        public UInt64 Build { get; set; }
    }
}