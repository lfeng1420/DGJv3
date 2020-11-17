using NAudio.MediaFoundation;
using NAudio.Wave;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;

namespace DGJv3.InternalModule
{
    sealed class LwlApiTencent : LwlApiBaseModule
    {
        private const string API_PROTOCOL = "https://";
        private const string API_HOST = "c.y.qq.com";
        private const string API_PATH = "/soso/fcgi-bin/client_search_cp?";
        private const string API_LYRIC_PATH = "/lyric/fcgi-bin/fcg_query_lyric_yqq.fcg?";
        private const string API_VKEY_HOST = "u.y.qq.com";
        private const string API_VKEY_PATH = "/cgi-bin/musicu.fcg?";
        private const string API_REFERER = API_PROTOCOL + "y.qq.com/";
        private const string FILE_FORMAT = ".m4a";
        

        internal LwlApiTencent()
        {
            SetServiceName("tencent");
            SetInfo("QQ音乐", INFO_AUTHOR, INFO_EMAIL, INFO_VERSION, "搜索QQ音乐的歌曲");
        }

        protected override string GetDownloadUrl(SongItem songInfo)
        {
            return $"http://ws.stream.qqmusic.qq.com/{songInfo.Extra}";
        }

        protected override string GetLyricById(string Id)
        {
            try
            {
                string strContent = Fetch(API_PROTOCOL, API_HOST, API_LYRIC_PATH + $"nobase64=1&musicid={Id}&-=jsonp1&g_tk=5381&loginUin=0&hostUin=0&format=json&inCharset=utf8&outCharset=utf-8&notice=0&platform=yqq.json&needNewCode=0", true, API_REFERER);
                JObject obj = JObject.Parse(strContent);
                if (obj["code"] == null
                    || obj["code"].ToString() != "0"
                    || obj["lyric"] == null)
                {
                    return null;
                }

                string lyric = HttpUtility.HtmlDecode(obj["lyric"].ToString());
                return lyric.Replace("&apos;", "'");
            }
            catch (Exception ex)
            {
                Log("歌词获取错误(ex:" + ex.ToString() + ",id:" + Id + ")");
                return null;
            }
        }

        protected override SongInfo Search(string keyword)
        {
            string result_str;
            string format_keyword = keyword.Replace('#', ' ');
            try
            {
                result_str = Fetch(API_PROTOCOL, API_HOST, API_PATH + $"ct=24&qqmusic_ver=1298&new_json=1&remoteplace=txt.yqq.song&searchid=68928555779163786&t=0&aggr=1&cr=1&lossless=0&flag_qc=0&p=1&n=5&w={HttpUtility.UrlEncode(format_keyword)}&g_tk=5381&loginUin=0&hostUin=0&format=json&inCharset=utf8&outCharset=utf-8&notice=0&platform=yqq.json&needNewCode=0");
            }
            catch (Exception ex)
            {
                Log("搜索歌曲时网络错误：" + ex.Message);
                return null;
            }

            try
            {
                JObject info = JObject.Parse(result_str);
                if (info["code"].ToString() != "0"
                    || info["data"] == null
                    || info["data"]["song"] == null
                    || info["data"]["song"]["list"] == null)
                {
                    return null;
                }

                return getValidSong(info["data"]["song"]["list"] as JArray, keyword);
            }
            catch (Exception ex)
            {
                Log("搜索歌曲解析数据错误：" + ex.Message);
                return null;
            }
        }

        private string fetchVKey(string strId)
        {
            string result_str;
            string strData = $"-=getplaysongvkey6523405588481026&g_tk=5381&loginUin=0&hostUin=0&format=json&inCharset=utf8&outCharset=utf-8&notice=0&platform=yqq.json&needNewCode=0&data=%7B%22req%22%3A%7B%22module%22%3A%22CDN.SrfCdnDispatchServer%22%2C%22method%22%3A%22GetCdnDispatch%22%2C%22param%22%3A%7B%22guid%22%3A%224049845496%22%2C%22calltype%22%3A0%2C%22userip%22%3A%22%22%7D%7D%2C%22req_0%22%3A%7B%22module%22%3A%22vkey.GetVkeyServer%22%2C%22method%22%3A%22CgiGetVkey%22%2C%22param%22%3A%7B%22guid%22%3A%224049845496%22%2C%22songmid%22%3A%5B%22{strId}%22%5D%2C%22songtype%22%3A%5B0%5D%2C%22uin%22%3A%220%22%2C%22loginflag%22%3A1%2C%22platform%22%3A%2220%22%7D%7D%2C%22comm%22%3A%7B%22uin%22%3A0%2C%22format%22%3A%22json%22%2C%22ct%22%3A24%2C%22cv%22%3A0%7D%7D";
            try
            {
                result_str = Fetch(API_PROTOCOL, API_VKEY_HOST, API_VKEY_PATH + strData);
            }
            catch (Exception ex)
            {
                Log("搜索歌曲时网络错误：" + ex.Message);
                return null;
            }

            JObject song = null;
            try
            {
                JObject info = JObject.Parse(result_str);
                if (info["code"].ToString() != "0"
                    || info["req_0"]["code"].ToString() != "0"
                    || info["req_0"]["data"] == null)
                {
                    return null;
                }

                JArray array = info["req_0"]["data"]["midurlinfo"] as JArray;
                if (array == null || array.Count == 0)
                {
                    return null;
                }

                song = array[0] as JObject;
                return song["purl"].ToString();
            }
            catch (Exception ex)
            {
                Log("搜索歌曲解析数据错误：" + ex.Message);
                return null;
            }
        }

        /// <summary>
        /// 解码
        /// </summary>
        /// <param name="song"></param>
        public override bool DecodeFile(SongItem song)
        {
            string strNew = song.FilePath + FILE_FORMAT;
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
                    song["mid"].ToString(),
                    song["name"].ToString(),
                    (song["singer"] as JArray).Select(x => x["name"].ToString()).ToArray()
                );
                songInfo.EId = song["id"].ToString();

                // 检查歌曲信息匹配度
                if (!CheckSingerMatch(keyword, songInfo, true, ref matchRate)
                    || !CheckSongNameMatch(keyword, songInfo, ref matchRate))
                {
                    continue;
                }

                // 获取vkey
                songInfo.Extra = fetchVKey(songInfo.Id);
                if (songInfo.Extra == null
                    || songInfo.Extra.Length == 0)
                {
                    Log(songInfo.Name + " " + songInfo.SingersText + ": " + (matchRate * 100).ToString("#0.00") + "%，VKey获取失败");
                    continue;
                }

                songInfo.FileFormat = FILE_FORMAT;
                songInfo.Rate = matchRate;
                songInfo.Lyric = GetLyricById(songInfo.EId);
                Log($"{songInfo.Name} {songInfo.SingersText}: {(matchRate * 100).ToString("#0.00")}%");
                return songInfo;
            }

            return null;
        }
    }
}
