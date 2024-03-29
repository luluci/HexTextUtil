﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HexTextUtil.HexText.HexTextLoader
{
    static internal class HexFileLoader
    {
        public static IHexFileLoader? HexFileLoaderFactory(string path)
        {
            // ファイル存在チェック
            if (!System.IO.File.Exists(path)) return null;
            // ファイル内容判定
            // とりあえず拡張子だけで判定
            return System.IO.Path.GetExtension(path) switch
            {
                ".hex" => new IntelHex(path),
                ".mot" => new MotS(path),
                _ => null,
            };
        }
    }
}
