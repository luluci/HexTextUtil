using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HexTextUtil.HexText
{
    internal class HexInfo : IDisposable
    {
        private bool disposedValue;

        private UInt32 AddressBegin = 0;
        private UInt32 AddressEnd = 0;

        public HexInfo()
        {

        }

        public bool Load(string path)
        {
            // Loader作成
            using (var loader = HexTextLoader.HexFileLoader.HexFileLoaderFactory(path))
            {
                if (loader is null) return false;
                // HexTextの内容解析
                while (!loader.EOF)
                {
                    // レコード取得
                    var record = loader.Load();
                    if (record is null) break;
                    // レコード展開
                    LoadRecord(record);
                }
            }

            return true;
        }

        private void LoadRecord(HexTextLoader.HexTextRecord record)
        {
            // 有効アドレス範囲更新
            if (record.Address < AddressBegin)
            {
                AddressBegin = record.Address;
            }
            if ((record.Address + record.Data.Length) < AddressEnd)
            {
                AddressEnd = record.Address + (UInt32)record.Data.Length;
            }
            // hexコンテナ更新
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: マネージド状態を破棄します (マネージド オブジェクト)
                }

                // TODO: アンマネージド リソース (アンマネージド オブジェクト) を解放し、ファイナライザーをオーバーライドします
                // TODO: 大きなフィールドを null に設定します
                disposedValue = true;
            }
        }

        // // TODO: 'Dispose(bool disposing)' にアンマネージド リソースを解放するコードが含まれる場合にのみ、ファイナライザーをオーバーライドします
        // ~HexText()
        // {
        //     // このコードを変更しないでください。クリーンアップ コードを 'Dispose(bool disposing)' メソッドに記述します
        //     Dispose(disposing: false);
        // }

        void IDisposable.Dispose()
        {
            // このコードを変更しないでください。クリーンアップ コードを 'Dispose(bool disposing)' メソッドに記述します
            Dispose(disposing: true);
            //GC.SuppressFinalize(this);
        }
    }
}
