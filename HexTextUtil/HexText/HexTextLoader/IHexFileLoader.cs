using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HexTextUtil.HexText.HexTextLoader
{
    internal class HexTextRecord
    {
        public UInt32 Address { get; set; } = 0;
        public string DataStr { get; set; } = string.Empty;
        public byte[] Data { get; set; } = Array.Empty<byte>();
        public byte[] Record { get; set; } = Array.Empty<byte>();
    }

    internal enum LoadStatus {
        Loading = 0,
        Success,
        NotFoundEndRecord,          // エンドレコード検出前にEOF到達
        DetectInvalidFormatLine,
        ReadFileError,
        DetectCheckSumError,
    }

    internal interface IHexFileLoader : IDisposable
    {
        public LoadStatus Status { get; set; }
        public bool EOF { get; }

        public HexTextRecord? Load();
    }
}
