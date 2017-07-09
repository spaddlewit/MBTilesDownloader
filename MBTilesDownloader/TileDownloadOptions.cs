using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MBTilesDownloader
{
    public struct TileDownloadOptions
    {
        /// <summary>
        /// The path to the MBTiles database (i.e., myDatabase.mbtiles)
        /// </summary>
        public string DBFilename { get; set; }

        /// <summary>
        /// The zoom level to download.
        /// </summary>
        public int Level { get; set; }

        /// <summary>
        /// The URI, with zxy indicators, (i.e., http://a.tile.openstreetmap.org/{z}/{x}/{y}.png)
        /// </summary>
        public string UriFormat { get; set; }

        /// <summary>
        /// The name of the Layer, in the MBTiles database.
        /// </summary>
        public string DBName { get; set; }
        /// <summary>
        /// The description of the Layer, in the MBTiles database.
        /// </summary>
        public string DBDescription { get; set; }

        /// <summary>
        /// If the existing tile is younger than this length of time, don't download it.
        /// </summary>
        public TimeSpan? MaxAge { get; set; }
    }
}
