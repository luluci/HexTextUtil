﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using MaterialDesignThemes.Wpf;
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
        public AsyncReactiveCommand HexFileRead { get; } = new AsyncReactiveCommand();
        // HexFile Info 設定GUI
        public ReactivePropertySlim<string> HexTextAddressBegin { get; } = new ReactivePropertySlim<string>("-");
        public ReactivePropertySlim<string> HexTextAddressEnd { get; } = new ReactivePropertySlim<string>("-");
        public ReactivePropertySlim<bool> HexTextFormatIntel { get; } = new ReactivePropertySlim<bool>(false);
        public ReactivePropertySlim<bool> HexTextFormatMot { get; } = new ReactivePropertySlim<bool>(false);
        public ReactivePropertySlim<string> HexTextLoadStatus { get; } = new ReactivePropertySlim<string>("");
        // CheckSum Info 設定GUI
        public ObservableCollection<CheckSumSetting> CheckSumSettings { get { return Config.ChecksumSettings; } }
        public ReactivePropertySlim<int> SelectIndexCheckSumSettings { get; set; } = new ReactivePropertySlim<int>(0);
        public ReactivePropertySlim<bool> IsReadOnlyCheckSumSettings { get; } = new ReactivePropertySlim<bool>(false);
        public ReactivePropertySlim<bool> IsEnableCheckSumSettings { get; } = new ReactivePropertySlim<bool>(true);
        // CheckSum計算GUI
        public AsyncReactiveCommand CalcCheckSum { get; } = new AsyncReactiveCommand();
        public ReactivePropertySlim<string> CalcCheckSumResult { get; } = new ReactivePropertySlim<string>("");
        // ダイアログ
        public ReactivePropertySlim<string> DialogMessage { get; set; } = new ReactivePropertySlim<string>("");

        // hex情報
        HexText.HexInfo? hex;
        // Config
        private Config Config;

        private StackPanel dialog;

        public MainWindowViewModel(StackPanel dialog)
        {
            this.dialog = dialog;

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
                        HexFileRead.Execute();
                    }
                })
                .AddTo(disposables);
            HexFilePath
                .AddTo(disposables);
            HexFilePreviewDragOver
                .Subscribe((e) => { if (e is not null) HexTextFilePreviewDragOver((DragEventArgs)e); })
                .AddTo(disposables);
            HexFileDrop
                .Subscribe((e) => { if (e is not null) HexTextFileDrop((DragEventArgs)e); })
                .AddTo(disposables);
            HexTextLoadStatus
                .AddTo(disposables);
            HexFileRead
                .Subscribe(async (_) =>
                {
                    DialogMessage.Value = "Reading HexText File ...";
                    var result = await DialogHost.Show(this.dialog, async delegate (object sender, DialogOpenedEventArgs args)
                    {
                        //await Task.Delay(5000);
                        await OnClickHexFileRead(_);
                        // CheckSum計算
                        DialogMessage.Value = "Calculating Checksum ...";
                        await OnClickCalcCheckSum(_);
                        args.Session.Close(false);
                    });
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
                .Subscribe(async _ =>
                {
                    DialogMessage.Value = "Calculating Checksum ...";
                    var result = await DialogHost.Show(this.dialog, async delegate (object sender, DialogOpenedEventArgs args)
                    {
                        //await Task.Delay(5000);
                        // CheckSum計算
                        await OnClickCalcCheckSum(_);
                        args.Session.Close(false);
                    });
                })
                .AddTo(disposables);
            CalcCheckSumResult
                .AddTo(disposables);
            DialogMessage
                .AddTo(disposables);
        }

        private async Task OnClickHexFileRead(object? sender)
        {
            // hexファイルロード
            //hex = new HexText.HexInfo();
            //var result = hex.Load(HexFilePath.Value);
            var result = await Task.Run(() =>
            {
                hex = new HexText.HexInfo();
                return hex.Load(HexFilePath.Value);
            });
            HexTextLoadStatus.Value = result switch
            {
                HexText.HexTextLoader.LoadStatus.Success => "File Read OK",
                HexText.HexTextLoader.LoadStatus.NotFoundEndRecord => "Err: FileFormat NG",
                HexText.HexTextLoader.LoadStatus.DetectInvalidFormatLine => "Err: FileFormat NG",
                HexText.HexTextLoader.LoadStatus.ReadFileError => "Err: File is locked",
                HexText.HexTextLoader.LoadStatus.DetectCheckSumError => "Err: CheckSum NG",
                _ => "",
            };
            // 成功したらGUIに展開
            if (hex is not null && result == HexText.HexTextLoader.LoadStatus.Success)
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
            else
            {
                hex = null;
            }
        }

        private async Task OnClickCalcCheckSum(object? sender)
        {
            if (hex is not null)
            {
                var config = Config.ChecksumSettings[SelectIndexCheckSumSettings.Value];
                var checksum = await Task.Run(() =>
                {
                    return hex.CalcCheckSum(config.AddressRangeBegin.Value, config.AddressRangeEnd.Value, config.Blank.Value);
                });
                var sb = new StringBuilder();
                if (config.CalcTotal.Value)
                {
                    if (Object.ReferenceEquals(config.FormatTotal, string.Empty))
                    {
                        sb.AppendLine($"{FormatCheckSum(checksum, config.Length.Value)} (補数なし)");
                    }
                    else
                    {
                        sb.AppendLine(FormatCheckSum(config.FormatTotal, config, checksum));
                    }
                }
                if (config.CalcTwosComp.Value)
                {
                    var temp = (checksum ^ 0xFFFFFFFFFFFFFFFF) + 1;
                    if (Object.ReferenceEquals(config.FormatTotal, string.Empty))
                    {
                        sb.AppendLine($"{FormatCheckSum(temp, config.Length.Value)} (2の補数)");
                    }
                    else
                    {
                        sb.AppendLine(FormatCheckSum(config.FormatTwosComp, config, temp));
                    }
                }
                CalcCheckSumResult.Value = sb.ToString();
            }
            else
            {
                CalcCheckSumResult.Value = "HexFile is not read.";
            }
        }

        private string FormatCheckSum(string format, CheckSumSetting config, UInt64 checksum)
        {
            var chksum = FormatCheckSum(checksum, config.Length.Value);
            var output = format.Replace("%checksum%", chksum);
            output = output.Replace("%addr_begin%", config.AddressRangeBeginText.Value);
            output = output.Replace("%addr_end%", config.AddressRangeEndText.Value);
            output = output.Replace("%blank%", config.BlankText.Value);
            return output;
        }

        private string FormatCheckSum(UInt64 checksum, CheckSumLength len)
        {
            int pos = len switch
            {
                CheckSumLength.Len1Byte => 16 - 2,
                CheckSumLength.Len2Byte => 16 - 4,
                CheckSumLength.Len4Byte => 16 - 8,
                CheckSumLength.Len8Byte => 16 - 16,
                _ => 16 - 2,
            };
            return $"{checksum:X16}".Substring(pos);
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
                HexFileRead.Execute();
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
