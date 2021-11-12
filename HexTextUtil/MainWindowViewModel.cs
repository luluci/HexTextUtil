using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Text;
using System.Threading.Tasks;

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
        public ReactivePropertySlim<string> HexFilePath { get; set; } = new ReactivePropertySlim<string>();
        public ReactiveCommand HexFileRead { get; } = new ReactiveCommand();
        // CheckSum設定GUI
        public ObservableCollection<CheckSumSetting> CheckSumSettings { get { return Config.ChecksumSettings; } }
        public ReactivePropertySlim<int> SelectIndexCheckSumSettings { get; set; } = new ReactivePropertySlim<int>();
        // CheckSum計算GUI

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
            HexFileRead
                .Subscribe(_ =>
                {
                })
                .AddTo(disposables);
            // CheckSum設定GUI
            SelectIndexCheckSumSettings.Value = 0;
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
