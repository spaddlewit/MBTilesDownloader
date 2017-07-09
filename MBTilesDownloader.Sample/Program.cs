using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BruTile;
using BruTile.Predefined;

namespace MBTilesDownloader.Sample
{
    class Program
    {
        static void Main(string[] args)
        {
            double minLon = -81.4822;
            double minLat = 27.4582;
            double maxLon = -81.3475;
            double maxLat = 27.5832;

            double[] wgs84Bounds = new double[4] { minLon, minLat, maxLon, maxLat };

            string urlFormat = "http://a.tile.openstreetmap.org/{z}/{x}/{y}.png";
            var ts = new BruTile.Web.HttpTileSource(new GlobalSphericalMercator(0, 18),
                        urlFormat,
                        null, "OpenStreetMap",
                        null, null,
                        null);

            TileDownloadOptions tdo = new TileDownloadOptions
            {
                DBFilename = "myDatabase.mbtiles",
                Level = 14,
                UriFormat = urlFormat,
                DBName = "Offline",
                DBDescription = "Offline",
                MaxAge = new TimeSpan(60, 0, 0, 0),
            };

            MBTilesDownloader.TileCacher.Cache(tdo, wgs84Bounds, ts);
        }
    }
}
