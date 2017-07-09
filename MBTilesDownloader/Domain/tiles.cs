﻿/*
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
using System.Linq;
using System.Text;
using SQLite;

namespace MBTiles.Domain
{
    public class tiles
    {
        [PrimaryKey, AutoIncrement]
        public int id { get; set; }

        public int zoom_level { get; set; }
        public int tile_column { get; set; }
        public int tile_row { get; set; }

        public DateTime createDate { get; set; }

        public byte[] tile_data { get; set; }
    }
}
