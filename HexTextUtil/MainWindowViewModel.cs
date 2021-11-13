using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

using WinAPI = Microsoft.WindowsAPICodePack;

namespace HexTextUtil
{
    internal class MainWindowViewModel : IDisposable, INotifyPropertyChanged
    {
        private bool disposedValue;
        private CompositeDisposable disposables = new CompositeDisposable();

        #region NotifyPropertyChanged
#pragma warning disable CS0067
        public event PropertyChangedEventHandler? PropertyChanged;
        public void SetProperty<T>(ref T target, T value, [System.Runtime.CompilerServices.CallerMemberName] string caller = "")
        {
            target = value;

            if (PropertyChanged == null)
                return;
            PropertyChangedEventArgs arg = new PropertyChangedEventArgs(caller);
            PropertyChanged.Invoke(this, arg);
        }
#pragma warning restore CS0067
        #endregion

        // HexFile指定
        public ReactiveCommand HexFilePathSelect { get; } = new ReactiveCommand();
        public ReactivePropertySlim<string> HexFilePath { get; set; } = new ReactivePropertySlim<string>("");
        public ReactiveCommand HexFilePreviewDragOver { get; } = new ReactiveCommand();
        public ReactiveCommand HexFileDrop { get; } = new ReactiveCommand();
        public ReactiveCommand HexFileRead { get; } = new ReactiveCommand();
        // HexFile Info 設定GUI
        public ReactivePropertySlim<string> HexTextAddressBegin { get; } = new ReactivePropertySlim<string>("-");
        public ReactivePropertySlim<string> HexTextAddressEnd { get; } = new ReactivePropertySlim<string>("-");
        public ReactivePropertySlim<bool> HexTextFormatIntel { get; } = new ReactivePropertySlim<bool>(false);
        public ReactivePropertySlim<bool> HexTextFormatMot { get; } = new ReactivePropertySlim<bool>(false);
        // CheckSum Info 設定GUI
        public ObservableCollection<CheckSumSetting> CheckSumSettings { get { return Config.ChecksumSettings; } }
        public ReactivePropertySlim<int> SelectIndexCheckSumSettings { get; set; } = new ReactivePropertySlim<int>(0);
        public ReactivePropertySlim<bool> IsReadOnlyCheckSumSettings { get; } = new ReactivePropertySlim<bool>(false);
        public ReactivePropertySlim<bool> IsEnableCheckSumSettings { get; } = new ReactivePropertySlim<bool>(true);
        // CheckSum計算GUI
        public ReactiveCommand CalcCheckSum { get; } = new ReactiveCommand();
        public ReactivePropertySlim<string> CalcCheckSumResult { get; } = new ReactivePropertySlim<string>("");

        // hex情報
        HexText.HexInfo? hex;
        // Config
        private Config Config;

        public MainWindowViewModel()
        {
            // Configロード
            Config = new Config();
            Config.Load();
            // hex/motファイル指定GUI
            HexFilePathSelect
                .Subscribe(_ =>
                {
                    var result = FileSelectDialog(HexFilePath.Value);
                    if (result is not null)
                    {
                        HexFilePath.Value = result;
                    }
                })
                .AddTo(disposables);
            HexFilePath
                .AddTo(disposables);
            HexFilePreviewDragOver
                .Subscribe(e => HexTextFilePreviewDragOver((DragEventArgs)e))
                .AddTo(disposables);
            HexFileDrop
                .Subscribe(e => HexTextFileDrop((DragEventArgs)e))
                .AddTo(disposables);
            HexFileRead
                .Subscribe(_ =>
                {
                    // hexファイルロード
                    hex = new HexText.HexInfo();
                    var result = hex.Load(HexFilePath.Value);
                    // 成功したらGUIに展開
                    if (result)
                    {
                        // HexTextFile Info
                        HexTextAddressBegin.Value = $"{hex.AddressBegin:X8}";
                        HexTextAddressEnd.Value = $"{hex.AddressEnd:X8}";
                        var ishex = hex.FileFormat == HexText.HexTextLoader.HexTextFileFormat.IntelHex;
                        HexTextFormatIntel.Value = ishex;
                        HexTextFormatMot.Value = !ishex;
                        // CheckSum Info
                        var config = Config.ChecksumSettings[0];
                        if (!config.AddressRangeFix)
                        {
                            config.AddressRangeBegin.Value = hex.AddressBegin;
                            config.AddressRangeEnd.Value = hex.AddressEnd;
                            config.AddressRangeBeginText.Value = $"{config.AddressRangeBegin.Value:X8}";
                            config.AddressRangeEndText.Value = $"{config.AddressRangeEnd.Value:X8}";
                        }
                    }
                })
                .AddTo(disposables);
            // HexFile Info 設定GUI
            HexTextAddressBegin
                .AddTo(disposables);
            HexTextAddressEnd
                .AddTo(disposables);
            HexTextFormatIntel
                .AddTo(disposables);
            HexTextFormatMot
                .AddTo(disposables);
            // CheckSum設定GUI
            IsReadOnlyCheckSumSettings
                .AddTo(disposables);
            IsEnableCheckSumSettings
                .AddTo(disposables);
            SelectIndexCheckSumSettings
                .Subscribe(_ =>
                {
                    var config = Config.ChecksumSettings[SelectIndexCheckSumSettings.Value];
                    IsReadOnlyCheckSumSettings.Value = config.AddressRangeFix;
                    IsEnableCheckSumSettings.Value = !config.AddressRangeFix;
                })
                .AddTo(disposables);
            SelectIndexCheckSumSettings.Value = 0;
            //
            CalcCheckSum
                .Subscribe(_ =>
                {
                    if (hex is not null)
                    {
                        var config = Config.ChecksumSettings[SelectIndexCheckSumSettings.Value];
                        var checksum = hex.CalcCheckSum(config.AddressRangeBegin.Value, config.AddressRangeEnd.Value, config.Blank.Value);
                        switch (config.Length.Value)
                        {
                            case CheckSumLength.Len1Byte: checksum &= 0xFF; break;
                            case CheckSumLength.Len2Byte: checksum &= 0xFFFF; break;
                            case CheckSumLength.Len4Byte: checksum &= 0xFFFFFFFF; break;
                            default: break;
                        }
                        var sb = new StringBuilder();
                        if (config.CalcTotal.Value)
                        {
                            sb.AppendLine($"{checksum:X16} (補数なし)");
                        }
                        if (config.CalcTwosComp.Value)
                        {
                            var temp = (checksum ^ 0xFFFFFFFFFFFFFFFF) + 1;
                            sb.AppendLine($"{temp:X16} (2の補数)");
                        }
                        CalcCheckSumResult.Value = sb.ToString();
                    }
                })
                .AddTo(disposables);
            CalcCheckSumResult
                .AddTo(disposables);
        }

        private void HexTextFilePreviewDragOver(DragEventArgs e)
        {
            // マウスポインタを変更する。
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.All;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = e.Data.GetDataPresent(DataFormats.FileDrop);
        }

        private void HexTextFileDrop(DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                // ドロップしたファイル名を全部取得する。
                string[] filenames = (string[])e.Data.GetData(DataFormats.FileDrop);
                HexFilePath.Value = filenames[0];
            }
        }


        private static string? FileSelectDialog(string initDir)
        {
            string? result = null;
            var dlg = new WinAPI::Dialogs.CommonOpenFileDialog
            {
                // フォルダ選択ダイアログ（falseにするとファイル選択ダイアログ）
                IsFolderPicker = false,
                // タイトル
                Title = "ファイルを選択してください",
                // 初期ディレクトリ
                InitialDirectory = initDir
            };

            if (dlg.ShowDialog() == WinAPI::Dialogs.CommonFileDialogResult.Ok)
            {
                result = dlg.FileName;
            }

            return result;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: マネージド状態を破棄します (マネージド オブジェクト)
                    disposables.Dispose();
                }

                // TODO: アンマネージド リソース (アンマネージド オブジェクト) を解放し、ファイナライザーをオーバーライドします
                // TODO: 大きなフィールドを null に設定します
                disposedValue = true;
            }
        }

        // // TODO: 'Dispose(bool disposing)' にアンマネージド リソースを解放するコードが含まれる場合にのみ、ファイナライザーをオーバーライドします
        // ~MainWindowViewModel()
        // {
        //     // このコードを変更しないでください。クリーンアップ コードを 'Dispose(bool disposing)' メソッドに記述します
        //     Dispose(disposing: false);
        // }

        void IDisposable.Dispose()
        {
            // このコードを変更しないでください。クリーンアップ コードを 'Dispose(bool disposing)' メソッドに記述します
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
