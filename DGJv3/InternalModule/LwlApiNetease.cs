using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;

namespace DGJv3.InternalModule
{
    sealed class LwlApiNetease : LwlApiBaseModule
    {
        private const string API_PROTOCOL = "http://";
        private const string API_HOST = "music.163.com";
        private const string API_SEARCH = "/api/search/get?";
        private const string API_GET_LYRIC = "/api/song/lyric?";
        private const string API_PLAYLIST = "/api/playlist/detail?";


        internal LwlApiNetease()
        {
            SetServiceName("netease");
            SetInfo("网易云音乐", INFO_AUTHOR, INFO_EMAIL, INFO_VERSION, "搜索网易云音乐的歌曲");
        }

        private static string FetchRspHeader(string url, int nTimeout = 5000)
        {
            HttpWebRequest request = null;
            try
            {
                request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "GET";
                request.Timeout = nTimeout;
                request.KeepAlive = false;
                request.UseDefaultCredentials = true;
                using (var response = (HttpWebResponse)request.GetResponse())
                {
                    return response.ResponseUri.OriginalString;
                }
            }
            catch (Exception)
            {
                return "404";
            }
            finally
            {
                if (request != null)
                {
                    request.Abort();
                }
            }
        }

        protected override string GetDownloadUrl(SongItem songInfo)
        {
            return $"https://music.163.com/song/media/outer/url?id={songInfo.SongId}";
        }

        protected override string GetLyricById(string Id)
        {
            try
            {
                string strContent = Fetch(API_PROTOCOL, API_HOST, API_GET_LYRIC + $"id={Id}&lv=-1&kv=-1&tv=-1");
                JObject obj = JObject.Parse(strContent);
                if (obj["code"] == null
                    || obj["code"].ToString() != "200"
                    || obj["lrc"] == null
                    || obj["lrc"]["lyric"] == null)
                {
                    return null;
                }

                return obj["lrc"]["lyric"].ToString();
            }
            catch (Exception ex)
            {
                Log("歌词获取错误(ex:" + ex.ToString() + ",id:" + Id + ")");
                return null;
            }
        }

        protected override List<SongInfo> GetPlaylist(string keyword)
        {
            List<SongInfo> songInfos = new List<SongInfo>();

            try
            {
                string strContent = Fetch(API_PROTOCOL, API_HOST, API_PLAYLIST + $"id={HttpUtility.UrlEncode(keyword)}");
                JObject playlist = JObject.Parse(strContent);

                if (playlist["code"] == null
                    || playlist["code"].ToString() != "200"
                    || playlist["result"] == null
                    || playlist["result"]["tracks"] == null)
                {
                    return null;
                }

                JArray songArr = playlist["result"]["tracks"] as JArray;
                foreach (JObject song in songArr)
                {
                    try
                    {
                        SongInfo songInfo = new SongInfo(
                            this,
                            song["id"].ToString(),
                            song["name"].ToString(),
                            (song["artists"] as JArray).Select(x => x["name"].ToString()).ToArray()
                        );

                        songInfo.Lyric = null;//在之后再获取Lyric
                        songInfos.Add(songInfo);
                    }
                    catch (Exception)
                    {
                    }
                };

                return songInfos;
            }
            catch (Exception ex)
            {
                Log("获取歌单信息时出错 " + ex.Message);
                return null;
            }
        }

        protected override SongInfo Search(string keyword)
        {
            string result_str;
            string format_keyword = keyword.Replace('#', ' ');
            try
            {
                result_str = Fetch(API_PROTOCOL, API_HOST, API_SEARCH + $"s={format_keyword}&limit=5&sub=false&type=1");
            }
            catch (Exception ex)
            {
                Log("搜索歌曲时网络错误：" + ex.Message);
                return null;
            }

            try
            {
                JObject info = JObject.Parse(result_str);
                if (info["code"].ToString() != "200"
                    || info["result"] == null
                    || info["result"]["songs"] == null)
                {
                    return null;
                }

                JArray songArr = info["result"]["songs"] as JArray;
                return getValidSong(songArr, keyword);
            }
            catch (Exception ex)
            {
                /*
                 TODO: 点歌 hentai 搜索歌曲解析数据错误：未将对象引用设置到对象的实例。
                 */
                Log("搜索歌曲解析数据错误：" + ex.Message);
                return null;
            }
        }

        protected override ModuleType GetModuleType()
        {
            return ModuleType.Netease;
        }

        private SongInfo getValidSong(JArray songArr, string keyword)
        {
            if (songArr == null)
            {
                return null;
            }

            Log("关键词: " + keyword);
            decimal matchRate = 0m;
            foreach (JObject song in songArr)
            {
                SongInfo songInfo = new SongInfo(
                    this,
                    song["id"].ToString(),
                    song["name"].ToString(),
                    (song["artists"] as JArray).Select(x => x["name"].ToString()).ToArray());

                // 检查歌曲信息匹配度
                if (!CheckSingerMatch(keyword, songInfo, true, ref matchRate)
                    || !CheckSongNameMatch(keyword, songInfo, ref matchRate))
                {
                    continue;
                }

                // 检查下载链接是否有效
                SongItem songitem = new SongItem(songInfo, "");
                string strUrl = GetDownloadUrl(songitem);
                string strLoc = FetchRspHeader(strUrl);
                if (strLoc.EndsWith("404"))
                {
                    Log(songInfo.Name + " " + songInfo.SingersText + ": " + (matchRate * 100).ToString("#0.00") + "%，404，没版权了");
                    continue;
                }

                Log($"{songInfo.Name} {songInfo.SingersText}: {(matchRate * 100).ToString("#0.00")}%");
                songInfo.Rate = matchRate;
                songInfo.Lyric = GetLyricById(songInfo.Id);
                return songInfo;
            }

            return null;
        }
    }
}
