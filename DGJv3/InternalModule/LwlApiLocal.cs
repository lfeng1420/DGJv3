using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DGJv3.InternalModule
{
    class LwlApiLocal : LwlApiBaseModule
    {
        DirectoryInfo mDirInfo;

        internal LwlApiLocal()
        {
            SetServiceName("Local");
            SetInfo("本地音乐", INFO_AUTHOR, INFO_EMAIL, INFO_VERSION, "搜索本地歌曲");
        }

        protected override SongInfo Search(string keyword)
        {
            if (mDirInfo == null)
            {
                return null;
            }

            Log("关键词: " + keyword);
            string[] arrSongInfo = keyword.Split('#');
            string singerNameOri = (arrSongInfo.Length > 1) ? arrSongInfo[1] : "";
            string songNameOri = arrSongInfo[0];
            SongInfo song = new SongInfo(this, "", songNameOri, new string[] { singerNameOri });
            string songName = Regex.Replace(songNameOri, @"[\s\.\-\(\)（）]", "").ToLowerInvariant();
            string singerName = Regex.Replace(singerNameOri, @"[\s\.\-\(\)（）]", "").ToLowerInvariant();

            // 搜索
            searchFiles(mDirInfo, songName, singerName, ref song);
            if (string.IsNullOrEmpty(song.Id))
            {
                return null;
            }

            return song;
        }

        protected override string GetLyricById(string Id)
        {
            return "";
        }

        protected override string GetDownloadUrl(SongItem songInfo)
        {
            return $"file:///{songInfo.SongId}";
        }

        protected override ModuleType GetModuleType()
        {
            return ModuleType.Local;
        }

        public override bool DecodeFile(SongItem song)
        {
            string strNew = song.FilePath + song.FileFormat;
            try
            {
                File.Copy(song.FilePath, strNew);
                MediaFoundationEncoder.EncodeToMp3(new MediaFoundationReader(strNew), song.FilePath);
                File.Delete(strNew);
                return true;
            }
            catch (Exception e)
            {
                Log("解码出错！" + e.Message);
                return false;
            }
        }

        public void UpdatePath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                mDirInfo = null;
                return;
            }

            mDirInfo = new DirectoryInfo(path);
        }

        private void searchFiles(DirectoryInfo dirInfo, string songName, string singerName, ref SongInfo song)
        {
            FileInfo[] arrFiles = dirInfo.GetFiles();
            foreach (FileInfo file in arrFiles)
            {
                // 先匹配歌手名
                string fileName = Regex.Replace(file.Name, @"[\s\.\-\(\)（）]", "").ToLowerInvariant();
                if (!string.IsNullOrEmpty(singerName)
                    && fileName.IndexOf(singerName) == -1)
                {

                    continue;
                }

                // 匹配歌曲名
                decimal rate = 0;
                if (!CheckInfoMatch(songName, fileName, ref rate))
                {
                    continue;
                }

                Log($"{file.Name} {singerName}: {(rate * 100).ToString("#0.00")}%");
                if (rate > song.Rate)
                {
                    song.Rate = rate;
                    song.Id = file.FullName;
                    song.Name = file.Name.Substring(0, file.Name.LastIndexOf('.'));
                    song.FileFormat = file.Name.Substring(file.Name.LastIndexOf('.'));
                }
            }

            DirectoryInfo[] arrDirs = dirInfo.GetDirectories();
            foreach (DirectoryInfo dir in arrDirs)
            {
                searchFiles(dir, songName, singerName, ref song);
            }
        }
    }
}
