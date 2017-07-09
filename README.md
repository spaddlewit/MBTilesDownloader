# MBTilesDownloader
Given WGS84 bounds and a tile source, downloads tiles into an mbtiles database for offline use.
If the mbtiles database already exists, it will append the new tiles to the existing database, rather than overwrite.

It also supports passing a list of bounds, and applying an age to already-downloaded tiles.

# Usage

    using BruTile;
    using BruTile.Predefined;

    double[] wgs84Bounds = new double[4] { minLon, minLat, maxLon, maxLat };

    string urlFormat = "http://a.tile.openstreetmap.org/{z}/{x}/{y}.png";
    BruTile.Web.HttpTileSource ts = new HttpTileSource(new GlobalSphericalMercator(0, 18),
                                    urlFormat, null, "OpenStreetMap",
                                    null, null, null);

    TileDownloadOptions tdo = new TileDownloadOptions
    {
        DBFilename = "myDatabase.mbtiles",
        Level = 14,
        UriFormat = urlFormat,
        DBName = "Offline",
        DBDescription = "Offline",
    };

    MBTilesDownloader.TileCacher.Cache(tdo, wgs84Bounds, ts);

# Note

Most tile servers have usage restrictions.
Please be sure you are following the usage policy of the tile server you are accessing.
