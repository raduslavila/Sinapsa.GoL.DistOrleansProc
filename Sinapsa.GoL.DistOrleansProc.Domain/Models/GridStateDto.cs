namespace Sinapsa.GoL.DistOrleansProc.Domain.Models
{
    public class GridStateDto
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public bool[][] Cells { get; set; } = Array.Empty<bool[]>();
    }
}
