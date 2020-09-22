﻿using NAudio.Wave;
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
        private Dictionary<string, string> mDictFiles = new Dictionary<string, string>();
        private readonly List<string> mSupportFileExts = new List<string> {
            ".mp3",
            ".wav",
            ".flac",
            ".aac",
            ".m4a",
            ".wma",
            ".ogg",
            ".amr",
            ".ape",
        };

        internal LwlApiLocal()
        {
            SetServiceName("Local");
            SetInfo("本地音乐", INFO_AUTHOR, INFO_EMAIL, INFO_VERSION, "搜索本地歌曲");
        }

        protected override SongInfo Search(string keyword)
        {
            if (mDictFiles.Count == 0)
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
            searchFiles(songNameOri, singerNameOri, ref song);
            return !string.IsNullOrEmpty(song.Id) ? song : null;
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
            if (string.IsNullOrEmpty(path)
                || mDictFiles.Count > 0)
            {
                return;
            }

            cacheFileName(path);
        }

        private void cacheFileName(string path)
        {
            string[] arrFileName = Directory.GetFiles(path);
            foreach (string fileName in arrFileName)
            {
                if (!mSupportFileExts.Exists(ext => fileName.EndsWith(ext)))
                {
                    continue;
                }

                mDictFiles[fileName] = fileName.Substring(fileName.LastIndexOf('\\') + 1);
            }

            string[] arrDirs = Directory.GetDirectories(path);
            foreach (string dir in arrDirs)
            {
                cacheFileName(dir);
            }
        }

        private void searchFiles(string songName, string singerName, ref SongInfo song)
        {
            foreach (var file in mDictFiles)
            {
                // 先匹配歌手名
                string fileName = Regex.Replace(file.Value, @"[\s\.\-\(\)（）]", "").ToLowerInvariant();
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

                Log($"{file.Value} {singerName}: {(rate * 100).ToString("#0.00")}%");
                if (rate > song.Rate)
                {
                    song.Rate = rate;
                    song.Id = file.Key;
                    song.Name = file.Value.Substring(0, file.Value.LastIndexOf('.'));
                    song.FileFormat = file.Value.Substring(file.Value.LastIndexOf('.'));
                }
            }
        }
    }
}
