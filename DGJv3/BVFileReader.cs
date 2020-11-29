using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DGJv3
{
    class BVFileReader
    {
        public TimeSpan CurrentTime { get; set; }

        public TimeSpan TotalTime { get; private set; }

        public PlayerStatus Status { get; private set; }

        public delegate void StoppedDelegate();
        public StoppedDelegate OnStopped;

        //private string mFileFullPath;
        private string mBV;
        private string mPage;
        private DateTime mStartTime = DateTime.Now;

        public void Load(SongItem item)
        {
            TotalTime = new TimeSpan(0,0,item.Duration + 8);
            CurrentTime = new TimeSpan(0);
            //item.FilePath = genHtmlFile(item.SongId);
            //mFileFullPath = item.FilePath;
            mBV = item.SongId;
            mPage = item.Extra;
        }

        public void Play()
        {
            mStartTime = DateTime.Now;
            try
            {
                System.Diagnostics.Process.Start($"https://www.bilibili.com/video/{mBV}?as_wide=1&p={mPage}&high_quality=1&danmaku=1&t=1");
                //mProcess = System.Diagnostics.Process.Start($"file:///{mFileFullPath}");
            }catch (Exception)
            {
            }
        }

        public void Pause()
        {
            Status = PlayerStatus.Paused;
        }

        public void Stop()
        {
            Process[] processes = Process.GetProcessesByName("msedge");
            foreach (Process p in processes)
            {
                try
                {
                    p.CloseMainWindow();
                    Thread.Sleep(100);
                    if (!p.HasExited)
                    {
                        p.Kill();
                    }
                }
                catch (Exception)
                {
                }
            }

            Status = PlayerStatus.Stopped;
            OnStopped?.Invoke();
        }

        public void Dispose()
        {
            Status = PlayerStatus.Stopped;
        }

        public void OnTick()
        {
            CurrentTime = DateTime.Now - mStartTime;
            if (CurrentTime >= TotalTime)
            {
                Status = PlayerStatus.Stopped;
                OnStopped?.Invoke();
            }
        }

        private string genHtmlFile(string bv)
        {
            //string fileFullPath = Path.Combine(Utilities.SongsCacheDirectoryPath, $"{bv}.html");
            //using (StreamWriter sw = File.CreateText(fileFullPath))
            //{
            //    sw.Write("<html><div style=\\\"position: relative; padding: 30% 45%;\\\"><iframe style=\\\"position: absolute; width: 100%; height: 100%; left: 0; top: 0;\\\" src=\\\"https://player.bilibili.com/player.html?bvid={bv}&page=1&as_wide=1&high_quality=1&autoplay=1\" scrolling=\\\"no\\\" frameborder=\\\"no\\\" framespacing=\\\"0\\\"> </iframe></div></html>");
            //}

            //return fileFullPath;
            return "";
        }
    }
}
