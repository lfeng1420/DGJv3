using DGJv3.InternalModule;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DGJv3
{
    class SearchModules : INotifyPropertyChanged
    {
        public SearchModule NullModule { get; private set; }
        public ObservableCollection<SearchModule> Modules { get; set; }
        public SearchModule PrimaryModule { get => primaryModule; set => SetField(ref primaryModule, value); }
        public SearchModule SecondaryModule { get => secondaryModule; set => SetField(ref secondaryModule, value); }
        public SearchModule BilibiliModule { get => bilibiliModule; set => SetField(ref bilibiliModule, value); }


        private string _localMusicFilePath;
        public string LocalMusicFilePath {
            get { return _localMusicFilePath; }
            set
            {
                _localMusicFilePath = value;
                updateLocalPath();
            }
        }

        private SearchModule primaryModule;
        private SearchModule secondaryModule;
        private SearchModule bilibiliModule;
        private SearchModule[] mSearchOrder = new SearchModule[2];

        internal SearchModules()
        {
            Modules = new ObservableCollection<SearchModule>();

            NullModule = new NullSearchModule();
            Modules.Add(NullModule);
            Modules.Add(new LwlApiNetease());
            Modules.Add(new LwlApiTencent());
            //Modules.Add(new LwlApiKugou());
            //Modules.Add(new LwlApiBaidu());
            //Modules.Add(new LwlApiXiami());
            Modules.Add(new LwlApiLocal());
            Modules.Add(new LwlApiBilibili());
            updateLocalPath();

            // TODO: 加载外置拓展

            void logaction(string log)
            {
                Log(log);
            }

            foreach (var m in Modules)
            {
                m._log = logaction;
            }

            PrimaryModule = Modules[1];
            SecondaryModule = Modules[2];
            BilibiliModule = Modules[4];
        }

        public SongInfo SafeSearch(string keyword)
        {
            SongInfo songInfo = null;
            for (int nIndex = 0; nIndex < mSearchOrder.Length; ++nIndex)
            {
                SearchModule module = mSearchOrder[nIndex];
                if (module != NullModule)
                {
                    SongInfo song = module.SafeSearch(keyword);
                    if (song == null
                        || (songInfo != null && decimal.Compare(song.Rate, songInfo.Rate) <= 0))
                    {
                        continue;
                    }

                    songInfo = song;
                }
            }

            return songInfo;
        }

        public SongItem SafeSearchBV(string keyword)
        {
            return BilibiliModule?.SafeSearchBV(keyword);
        }


        public event PropertyChangedEventHandler PropertyChanged;
        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = "")
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            updateSearchOrder();
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            return true;
        }

        public event LogEvent LogEvent;
        private void Log(string message, Exception exception = null) => LogEvent?.Invoke(this, new LogEventArgs() { Message = message, Exception = exception });

        private void updateLocalPath()
        {
            LwlApiLocal module = Modules[Modules.Count - 1] as LwlApiLocal;
            module?.UpdatePath(_localMusicFilePath);
        }

        private void updateSearchOrder()
        {
            mSearchOrder[0] = primaryModule;
            mSearchOrder[1] = secondaryModule;
        }
    }
}
