using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NosData.DTOs
{
    public class NosTaleMapZoneDto
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public int[,] Cells { get; set; } = new int[0, 0];
    }
}
