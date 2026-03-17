using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VinhKhanhTour.Models
{
    public class RouteRequest
    {
        public double Lat { get; set; }
        public double Lng { get; set; }
        public string Name { get; set; } = "";
    }

    public class OsrmResponse
    {
        public List<OsrmRoute>? Routes { get; set; }
    }

    public class OsrmRoute
    {
        public double Distance { get; set; }
        public double Duration { get; set; }
        public OsrmGeometry Geometry { get; set; } = new();
    }

    public class OsrmGeometry
    {
        public List<double[]> Coordinates { get; set; } = new();
    }
}