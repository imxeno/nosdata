using System.Collections.Generic;

namespace NosData.DTOs
{
    public class NosTalePatchDto
    {
        public List<NosTalePatchEntryDto> Entries { get; set; }
        public ulong TotalSize { get; set; }
        public ulong Build { get; set; }
    }
}