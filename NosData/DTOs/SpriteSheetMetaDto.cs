using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NosData.DTOs
{
    public class SpriteSheetMetaDto
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public Dictionary<int, int[]> Positions = new();
    }
}
