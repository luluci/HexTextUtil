using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Text.Json;
using System.Text.Json.Serialization;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System.Reactive.Disposables;
using System.Collections.ObjectModel;

namespace HexTextUtil
{

    public class Config : IDisposable
    {
        private CompositeDisposable disposables = new CompositeDisposable();
        private string configFilePath;
        public JsonItem? json;

        private ObservableCollection<CheckSumSetting> checksumSettings;
        public ObservableCollection<CheckSumSetting> ChecksumSettings { get { return checksumSettings; } }

        public Config()
        {
            // パス設定
            configFilePath = Environment.ProcessPath + @".json";
            // 設定初期化
            // hex/motからの読み込み値を反映する設定をデフォルトでセット
            checksumSettings = new ObservableCollection<CheckSumSetting>()
            {
                new CheckSumSetting()
                {
                    Name = "<Default>",
                    AddressRangeFix = false,
                    AddressRangeBeginText = "<auto>",
                    AddressRangeEndText = "<auto>",
                }
            };
        }

        public void Load()
        {
            LoadImpl(false).Wait();
        }

        public async Task LoadAsync()
        {
            await LoadImpl(true);
        }

        /** 初回起動用に同期的に動作する
         * 
         */
        private async Task LoadImpl(bool configAwait = true)
        {
            // 設定ロード
            if (File.Exists(configFilePath))
            {
                // ファイルが存在する
                //
                var options = new JsonSerializerOptions
                {
                    Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
                    //Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                    //NumberHandling = JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.AllowNamedFloatingPointLiterals,
                };
                //
                using (var stream = new FileStream(configFilePath, FileMode.Open, FileAccess.Read))
                {
                    // 呼び出し元でWait()している。ConfigureAwait(false)無しにawaitするとデッドロックで死ぬ。
                    json = await JsonSerializer.DeserializeAsync<JsonItem>(stream, options).ConfigureAwait(configAwait);
                }
            }
            else
            {
                // ファイルが存在しない
                json = null;
            }
            // JSONデータを制御値に反映
            LoadCheckSumSettings();
        }

        private void LoadCheckSumSettings()
        {
            if (json is not null && json.CheckSumSettings is not null)
            {
                foreach (var item in json.CheckSumSettings)
                {
                    var setting = new CheckSumSetting();
                    //
                    try
                    {
                        setting.Name = item.Name;
                        setting.AddressRangeBegin = Convert.ToUInt32(item.AddressRange.Begin, 16);
                        setting.AddressRangeEnd = Convert.ToUInt32(item.AddressRange.End, 16);
                        setting.Blank = Convert.ToByte(item.Blank, 16);
                        setting.AddressRangeBeginText = $"{item.AddressRange.Begin:8}";
                        setting.AddressRangeEndText = $"{item.AddressRange.End:8}";
                        setting.BlankText = item.Blank;
                        setting.Length = LoadCheckSumSettingsLength(item.Length);
                        setting.LengthValue = (uint)setting.Length;
                        setting.CalcTotal = item.CalcTotal;
                        setting.CalcTwosComp = item.CalcTwosCompl;
                        //
                        checksumSettings.Add(setting);
                    }
                    catch (Exception)
                    {

                    }
                }
            }
        }
        private CheckSumLength LoadCheckSumSettingsLength(uint inp)
        {
            switch (inp)
            {
                case 1: return CheckSumLength.Len1Byte;
                case 2: return CheckSumLength.Len2Byte;
                case 4: return CheckSumLength.Len4Byte;
                case 8: return CheckSumLength.Len8Byte;
                default: throw new Exception("invalid CheckSum Length, accept 1/2/4/8 byte");
            }
        }

        public void Save()
        {
            SaveImpl(false).Wait();
        }

        public async Task SaveAsync()
        {
            await SaveImpl(true);
        }

        public async Task SaveImpl(bool configAwait = true)
        {
            var options = new JsonSerializerOptions
            {
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                WriteIndented = true,
            };
            //
            string jsonStr = JsonSerializer.Serialize(json, options);
            //
            using (var stream = new FileStream(configFilePath, FileMode.Create, FileAccess.Write))
            {
                // 呼び出し元でWait()している。ConfigureAwait(false)無しにawaitするとデッドロックで死ぬ。
                await JsonSerializer.SerializeAsync(stream, json, options).ConfigureAwait(configAwait);
            }
        }

        #region IDisposable Support
        private bool disposedValue = false; // 重複する呼び出しを検出するには

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    disposables.Dispose();
                }

                disposedValue = true;
            }
        }

        void IDisposable.Dispose()
        {
            Dispose(true);
        }
        #endregion
    }

    public enum CheckSumLength : uint
    {
        Len1Byte = 0,
        Len2Byte = 1,
        Len4Byte = 2,
        Len8Byte = 3,
    }
    public class CheckSumSetting
    {
        // Setting名称
        public string Name { get; set; } = string.Empty;
        // チェックサム計算アドレス範囲設定
        public bool AddressRangeFix { get; set; } = false;
        public UInt32? AddressRangeBegin { get; set; } = null;
        public UInt32? AddressRangeEnd { get; set; } = null;
        public string AddressRangeBeginText { get; set; } = string.Empty;
        public string AddressRangeEndText { get; set; } = string.Empty;
        // Blank
        public uint Blank { get; set; } = 255;
        public string BlankText { get; set;} = "FF";
        // チェックサム長
        // 0:1byte, 1:2byte, 2:4byte, 3:8byte
        public CheckSumLength Length { get; set; } = CheckSumLength.Len2Byte;
        public uint LengthValue { get; set; } = (uint)CheckSumLength.Len2Byte;
        // チェックサム計算方法
        // 補数なし
        public bool CalcTotal { get; set; } = false;
        // 2の補数
        public bool CalcTwosComp { get; set; } = true;
    }

    public class JsonItem
    {

        [JsonPropertyName("checksum_settings")]
        public IList<JsonCheckSumSetting>? CheckSumSettings { get; set; }
    }

    public class JsonCheckSumSetting
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("address_range")]
        public JsonAddressRange AddressRange { get; set; } = new JsonAddressRange();

        [JsonPropertyName("blank")]
        public string Blank { get; set; } = "FF";

        [JsonPropertyName("length")]
        public uint Length { get; set; } = 1;

        [JsonPropertyName("calc_total")]
        public bool CalcTotal { get; set; } = false;
        [JsonPropertyName("calc_twos_compl")]
        public bool CalcTwosCompl { get; set; } = true;
    }

    public class JsonAddressRange
    {
        [JsonPropertyName("begin")]
        public string Begin { get; set; } = string.Empty;

        [JsonPropertyName("end")]
        public string End { get; set; } = string.Empty;
    }
}
