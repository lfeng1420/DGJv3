using BilibiliDM_PluginFramework;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Threading;

namespace DGJv3
{
    class DanmuHandler : INotifyPropertyChanged
    {
        private ObservableCollection<SongItem> Songs;

        private ObservableCollection<BlackListItem> Blacklist;

        private Player Player;

        private Downloader Downloader;

        private SearchModules SearchModules;

        private Dispatcher dispatcher;

        /// <summary>
        /// 最多点歌数量
        /// </summary>
        public uint MaxTotalSongNum { get => _maxTotalSongCount; set => SetField(ref _maxTotalSongCount, value); }
        private uint _maxTotalSongCount;

        /// <summary>
        /// 每个人最多点歌数量
        /// </summary>
        public uint MaxPersonSongNum { get => _maxPersonSongNum; set => SetField(ref _maxPersonSongNum, value); }
        private uint _maxPersonSongNum;

        internal DanmuHandler(ObservableCollection<SongItem> songs, Player player, Downloader downloader, SearchModules searchModules, ObservableCollection<BlackListItem> blacklist)
        {
            dispatcher = Dispatcher.CurrentDispatcher;
            Songs = songs;
            Player = player;
            Downloader = downloader;
            SearchModules = searchModules;
            Blacklist = blacklist;
        }


        /// <summary>
        /// 处理弹幕消息
        /// <para>
        /// 注：调用侧可能会在任意线程
        /// </para>
        /// </summary>
        /// <param name="danmakuModel"></param>
        internal void ProcessDanmu(DanmakuModel danmakuModel)
        {
            if (danmakuModel.MsgType != MsgTypeEnum.Comment || string.IsNullOrWhiteSpace(danmakuModel.CommentText))
                return;

            string[] commands = danmakuModel.CommentText.Split(SPLIT_CHAR, StringSplitOptions.RemoveEmptyEntries);
            string rest = string.Join(" ", commands.Skip(1));

            if (danmakuModel.isAdmin)
            {
                switch (commands[0])
                {
                    case "切歌":
                        {
                            // Player.Next();

                            dispatcher.Invoke(() =>
                            {
                                if (Songs.Count > 0)
                                {
                                    Songs[0].Remove(Songs, Downloader, Player);
                                    Log($"切歌成功:UP已切歌");
                                }
                            });

                            /*
                            if (commands.Length >= 2)
                            {
                                // TODO: 切指定序号的歌曲
                            }
                            */
                        }
                        return;
                    case "暂停":
                    case "暫停":
                        {
                            Player.Pause();
                        }
                        return;
                    case "播放":
                        {
                            Player.Play();
                        }
                        return;
                    case "音量":
                        {
                            if (commands.Length > 1
                                && int.TryParse(commands[1], out int volume100)
                                && volume100 >= 0
                                && volume100 <= 100)
                            {
                                Player.Volume = volume100 / 100f;
                            }
                        }
                        return;
                    default:
                        break;
                }
            }

            switch (commands[0])
            {
                case "点歌":
                case "點歌":
                    {
                        DanmuAddSong(danmakuModel, rest);
                    }
                    return;

                case "点播":
                case "點播":
                    {
                        DanmuAddBV(danmakuModel, rest);
                    }
                    return;

                case "取消點歌":
                case "取消点歌":
                    {
                        dispatcher.Invoke(() =>
                        {
                            SongItem songItem = Songs.LastOrDefault(x => x.UserName == danmakuModel.UserName && x.Status != SongStatus.Playing);
                            if (songItem != null)
                            {
                                songItem.Remove(Songs, Downloader, Player);
                                Log($"取消点歌成功:{danmakuModel.UserName}已取消点歌: {songItem.SongName}");
                            }
                        });
                    }
                    return;
                case "投票切歌":
                    {
                        // TODO: 投票切歌
                    }
                    return;
                case "切歌":
                    {
                        dispatcher.Invoke(() =>
                        {
                            if (Songs.Count > 0)
                            {
                                SongItem item = Songs[0];
                                if (item.UserName == danmakuModel.UserName)
                                {
                                    if (item.FileFormat == "mp4")
                                    {
                                        Log($"切歌失败:暂时不能切视频");
                                        return;
                                    }

                                    item.Remove(Songs, Downloader, Player);
                                    Log($"切歌成功:{item.UserName}已切歌");
                                }
                            }
                        });
                    }
                    return;
                default:
                    break;
            }
        }

        private void DanmuAddSong(DanmakuModel danmakuModel, string keyword)
        {
            if (dispatcher.Invoke(callback: () => CanAddSong(username: danmakuModel.UserName)))
            {
                SongInfo songInfo = SearchModules.SafeSearch(keyword);
                if (songInfo == null)
                {
                    Log($"点歌失败:歌曲{keyword}可能没有版权或需要VIP");
                    return;
                }

                if (songInfo.IsInBlacklist(Blacklist))
                {
                    Log($"点歌失败:歌曲{songInfo.Name}在黑名单中");
                    return;
                }
                Log($"点歌成功:{danmakuModel.UserName}点歌: {songInfo.Name}");
                dispatcher.Invoke(callback: () =>
                {
                    if (CanAddSong(danmakuModel.UserName) &&
                        !Songs.Any(x =>
                            x.SongId == songInfo.Id &&
                            x.Module.UniqueId == songInfo.Module.UniqueId)
                    )
                    {
                        SongItem item = new SongItem(songInfo, danmakuModel.UserName);
                        for (int i = Songs.Count - 1; i >= 0; --i)
                        {
                            if (Songs[i].UserName != Utilities.SparePlaylistUser)
                            {
                                Songs.Insert(i + 1, item);
                                return;
                            }
                        }

                        Player.Next();
                        Songs.RemoveAt(0);
                        Songs.Insert(0, item);
                    }
                });
            }
        }

        private void DanmuAddBV(DanmakuModel danmakuModel, string keyword)
        {
            if (dispatcher.Invoke(callback: () => CanAddSong(username: danmakuModel.UserName)))
            {
                SongItem songItem = SearchModules.SafeSearchBV(keyword);
                if (songItem == null)
                {
                    Log($"点播失败:视频{keyword}可能不存在");
                    return;
                }

                if (songItem.IsInBlacklist(Blacklist))
                {
                    Log($"点播失败:视频{songItem.SongName}在黑名单中");
                    return;
                }

                songItem.UserName = danmakuModel.UserName;
                songItem.Status = SongStatus.WaitingPlay;
                Log($"点播成功:{danmakuModel.UserName}点播: {songItem.SongName}");
                dispatcher.Invoke(callback: () =>
                {
                    if (CanAddSong(danmakuModel.UserName) &&
                        !Songs.Any(x =>
                            x.SongId == songItem.SongName &&
                            x.Module.UniqueId == songItem.Module.UniqueId)
                    )
                    {
                        for (int i = Songs.Count - 1; i >= 0; --i)
                        {
                            if (Songs[i].UserName != Utilities.SparePlaylistUser)
                            {
                                Songs.Insert(i + 1, songItem);
                                return;
                            }
                        }

                        Player.Next();
                        Songs.RemoveAt(0);
                        Songs.Insert(0, songItem);
                    }
                });
            }
        }

        /// <summary>
        /// 能否点歌
        /// <para>
        /// 注：调用侧需要在主线程上运行
        /// </para>
        /// </summary>
        /// <param name="username">点歌用户名</param>
        /// <returns></returns>
        private bool CanAddSong(string username)
        {
            return Songs.Count < MaxTotalSongNum ? (Songs.Where(x => x.UserName == username).Count() < MaxPersonSongNum) : false;
        }

        private readonly static char[] SPLIT_CHAR = { ' ' };

        public event PropertyChangedEventHandler PropertyChanged;
        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = "")
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            return true;
        }

        public event LogEvent LogEvent;
        private void Log(string message, Exception exception = null) => LogEvent?.Invoke(this, new LogEventArgs() { Message = message, Exception = exception });
    }
}
