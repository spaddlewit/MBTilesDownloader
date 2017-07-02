# MBTilesDownloader
Given WGS84 bounds and a tile source, downloads tiles into an mbtiles database for offline use.
If the mbtiles database already exists, it will append the new tiles to the existing database, rather than overwrite.

# Usage

    double[] wgs84Bounds = new double[4] = { minLon, minLat, maxLon, maxLat };
    MBTilesDownloader.Cache("myDatabase.mbtiles", wgs84Bounds, "14", "http://a.tile.openstreetmap.org/{z}/{x}/{y}.png");

# Note

    Most tile servers have usage restrictions. Please be sure you are following the usage policy of the tile server you are accessing.