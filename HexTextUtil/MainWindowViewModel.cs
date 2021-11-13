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
        // CheckSum設定GUI
        public ObservableCollection<CheckSumSetting> CheckSumSettings { get { return Config.ChecksumSettings; } }
        public ReactivePropertySlim<int> SelectIndexCheckSumSettings { get; set; } = new ReactivePropertySlim<int>(0);
        // CheckSum計算GUI

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
                    hex = new HexText.HexInfo();
                    var result = hex.Load(HexFilePath.Value);
                })
                .AddTo(disposables);
            // CheckSum設定GUI
            SelectIndexCheckSumSettings.Value = 0;
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

        private CompositeDisposable disposables = new CompositeDisposable();
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
