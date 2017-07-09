/*
Copyright (c) 2017 Spaddlewit Inc.

Licensed under the Apache License, Version 2.0 (the "License"); you
may not use this file except in compliance with the License. You may
obtain a copy of the License at

http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or
implied. See the License for the specific language governing permissions
and limitations under the License.
*/
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using BruTile;
using BruTile.Predefined;
using System.Threading.Tasks;
using System.Globalization;

namespace MBTilesDownloader
{
    public static class TileCacher
    {
        class LoadedTile
        {
            public TileInfo ti;
            public byte[] data;
            public string mimeType;
        }

        /// <summary>
        /// Converts WGS84 coordinates to SphericalMercator. Shamelessly taken from BruTile.MbTiles.Pcl.MbTilesTileSource. Why not make your method public, guys??
        /// </summary>
        /// <param name="mercatorXLon">longitude</param>
        /// <param name="mercatorYLat">latitude</param>
        public static void ToMercator(ref double mercatorXLon, ref double mercatorYLat)
        {
            if ((Math.Abs(mercatorXLon) > 180 || Math.Abs(mercatorYLat) > 90))
                return;

            double num = mercatorXLon * 0.017453292519943295;
            double x = 6378137.0 * num;
            double a = mercatorYLat * 0.017453292519943295;

            mercatorXLon = x;
            mercatorYLat = 3189068.5 * Math.Log((1.0 + Math.Sin(a)) / (1.0 - Math.Sin(a)));
        }

        /// <summary>
        /// Fetches a single tile and adds it to the ConcurrentBag.
        /// </summary>
        /// <param name="ti"></param>
        /// <param name="client"></param>
        /// <param name="bag"></param>
        /// <param name="uriFormat"></param>
        /// <returns></returns>
        static async Task FetchTile(TileInfo ti, System.Net.Http.HttpClient client, ConcurrentBag<LoadedTile> bag, string uriFormat)
        {
            uriFormat = uriFormat.Replace("{z}", ti.Index.Level);
            uriFormat = uriFormat.Replace("{y}", ti.Index.Row.ToString(CultureInfo.InvariantCulture));
            uriFormat = uriFormat.Replace("{x}", ti.Index.Col.ToString(CultureInfo.InvariantCulture));

            System.Net.Http.HttpResponseMessage response = await client.GetAsync(uriFormat);
            byte[] data = await response.Content.ReadAsByteArrayAsync();

            if (data == null)
                return;

            LoadedTile lt = new LoadedTile();
            lt.ti = ti;
            lt.data = data;
            lt.mimeType = response.Content.Headers.ContentType.MediaType;
            bag.Add(lt);
        }

        /// <summary>
        /// Flips the Y coordinate from OSM to TMS format and vice versa.
        /// </summary>
        /// <param name="level">zoom level</param>
        /// <param name="row">Y coordinate</param>
        /// <returns>inverted Y coordinate</returns>
        static int OSMtoTMS(int level, int row)
        {
            return (1 << level) - row - 1;
        }

        public static void Cache(TileDownloadOptions options, List<double[]> boundsList, BruTile.Web.HttpTileSource tileSource)
        {
            double[] originalBounds = new double[4] { double.MaxValue, double.MaxValue, double.MinValue, double.MinValue }; // In WGS1984, the total extent of all bounds

            List<TileInfo> tileInfos = new List<TileInfo>();
            foreach (double[] xy in boundsList)
            {
                if (xy[0] < originalBounds[0])
                    originalBounds[0] = xy[0];
                if (xy[1] < originalBounds[1])
                    originalBounds[1] = xy[1];
                if (xy[2] > originalBounds[2])
                    originalBounds[2] = xy[2];
                if (xy[3] > originalBounds[3])
                    originalBounds[3] = xy[3];

                ToMercator(ref xy[0], ref xy[1]);
                ToMercator(ref xy[2], ref xy[3]);

                // xy is now in SphericalMercator projection

                BruTile.Extent extent = new BruTile.Extent(xy[0], xy[1], xy[2], xy[3]);

                var curTileInfos = tileSource.Schema.GetTileInfos(extent, options.Level.ToString());

                foreach (var tileInfo in curTileInfos)
                {
                    TileInfo ti = tileInfos.Where(x => x.Index.Col == tileInfo.Index.Col && x.Index.Row == tileInfo.Index.Row && x.Index.Level == tileInfo.Index.Level).FirstOrDefault();

                    if (ti != null) // This tile is already in the list
                        continue;

                    tileInfos.Add(tileInfo);
                }
            }

            if (options.MaxAge.HasValue)
            {
                using (var db = new SQLite.SQLiteConnection(options.DBFilename))
                {
                    // Create the tables, so at least they will exist
                    db.CreateTable<MBTiles.Domain.metadata>();
                    db.CreateTable<MBTiles.Domain.tiles>();

                    for (int i = 0; i < tileInfos.Count; i++)
                    {
                        TileInfo tileInfo = tileInfos[i];

                        int tileLevel = int.Parse(tileInfo.Index.Level);
                        int tileRow = OSMtoTMS(tileLevel, tileInfo.Index.Row);
                        MBTiles.Domain.tiles oldTile = db.Table<MBTiles.Domain.tiles>().Where(x => x.zoom_level == tileLevel && x.tile_column == tileInfo.Index.Col && x.tile_row == tileRow).FirstOrDefault();

                        if (oldTile != null && (DateTime.Now - oldTile.createDate) <= options.MaxAge.Value)
                        {
                            // This tile hasn't expired yet, so don't download it.
                            tileInfos.Remove(tileInfo);
                            i--;
                        }
                    }
                }
            }

            // Make sure we have any tiles left to download. If not, we are done!
            if (tileInfos.Count == 0)
                return;

            ConcurrentBag<LoadedTile> bag = new ConcurrentBag<LoadedTile>();

            using (System.Net.Http.HttpClient client = new System.Net.Http.HttpClient())
            {
                List<Task> fetchTasks = new List<Task>();
                foreach (var ti in tileInfos)
                {
                    fetchTasks.Add(Task.Run(async () => await FetchTile(ti, client, bag, options.UriFormat)));

                    if (fetchTasks.Count > 1) // Limit 2 to concurrent download threads
                    {
                        Task.WaitAll(fetchTasks.ToArray());
                        fetchTasks.Clear();
                    }
                }

                Task.WaitAll(fetchTasks.ToArray());
            }

            using (var db = new SQLite.SQLiteConnection(options.DBFilename))
            {
                db.CreateTable<MBTiles.Domain.metadata>();
                db.CreateTable<MBTiles.Domain.tiles>();

                var metaList = new List<MBTiles.Domain.metadata>();

                metaList.Add(new MBTiles.Domain.metadata { name = "name", value = options.DBName });
                metaList.Add(new MBTiles.Domain.metadata { name = "type", value = "baselayer" });
                metaList.Add(new MBTiles.Domain.metadata { name = "version", value = "1" });
                metaList.Add(new MBTiles.Domain.metadata { name = "description", value = options.DBDescription });
                metaList.Add(new MBTiles.Domain.metadata { name = "format", value = bag.First().mimeType.Contains("/png") ? "png" : "jpg" });

                foreach (var meta in metaList)
                    db.InsertOrReplace(meta);

                // Expand the bounds
                var existingBounds = db.Table<MBTiles.Domain.metadata>().Where(x => x.name == "bounds").FirstOrDefault();
                if (existingBounds != null)
                {
                    var components = existingBounds.value.Split(',');
                    var existingExtent = new double[4] {
                        double.Parse(components[0], NumberFormatInfo.InvariantInfo),
                        double.Parse(components[1], NumberFormatInfo.InvariantInfo),
                        double.Parse(components[2], NumberFormatInfo.InvariantInfo),
                        double.Parse(components[3], NumberFormatInfo.InvariantInfo)
                        };

                    if (originalBounds[0] < existingExtent[0])
                        existingExtent[0] = originalBounds[0];
                    if (originalBounds[1] < existingExtent[1])
                        existingExtent[1] = originalBounds[1];
                    if (originalBounds[2] > existingExtent[2])
                        existingExtent[2] = originalBounds[2];
                    if (originalBounds[3] > existingExtent[3])
                        existingExtent[3] = originalBounds[3];

                    existingExtent.CopyTo(originalBounds, 0);
                }

                db.InsertOrReplace(new MBTiles.Domain.metadata { name = "bounds", value = String.Join(",", originalBounds) });

                foreach (var lt in bag)
                {
                    MBTiles.Domain.tiles tile = new MBTiles.Domain.tiles();
                    tile.zoom_level = int.Parse(lt.ti.Index.Level);
                    tile.tile_column = lt.ti.Index.Col;
                    tile.tile_row = lt.ti.Index.Row;
                    tile.tile_data = lt.data;
                    tile.createDate = DateTime.Now;

                    tile.tile_row = OSMtoTMS(tile.zoom_level, tile.tile_row);

                    MBTiles.Domain.tiles oldTile = db.Table<MBTiles.Domain.tiles>().Where(x => x.zoom_level == tile.zoom_level && x.tile_column == tile.tile_column && x.tile_row == tile.tile_row).FirstOrDefault();

                    if (oldTile != null)
                    {
                        tile.id = oldTile.id;
                        db.Update(tile);
                    }
                    else
                        db.Insert(tile);
                }
            }
        }

        public static void Cache(TileDownloadOptions options, double[] bounds, BruTile.Web.HttpTileSource tileSource)
        {
            List<double[]> list = new List<double[]>();
            list.Add(bounds);
            Cache(options, list, tileSource);
        }
    }
}
