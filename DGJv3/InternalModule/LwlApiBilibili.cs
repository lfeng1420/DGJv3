using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace DGJv3.InternalModule
{
    class LwlApiBilibili : LwlApiBaseModule
    {
        private const string API_PROTOCOL = "https://";
        private const string API_HOST = "api.bilibili.com";
        private const string API_FETCH_BVDETAIL = "/x/web-interface/view?bvid=";
        private const string FILE_FORMAT = "mp4";

        internal LwlApiBilibili()
        {
            SetServiceName("bilibili");
            SetInfo("B站视频", INFO_AUTHOR, INFO_EMAIL, INFO_VERSION, "搜索B站视频");
        }

        protected override SongItem SearchBV(string keyword)
        {
            int page = 1;
            int nIndex = keyword.LastIndexOf('/');
            if (nIndex != -1)
            {
                int.TryParse(keyword.Substring(nIndex + 1), out page);
                keyword = keyword.Substring(0, nIndex);
            }

            try
            {
                string result = Fetch(API_PROTOCOL, API_HOST, API_FETCH_BVDETAIL + $"{HttpUtility.UrlEncode(keyword)}");
                JObject obj = JObject.Parse(result);
                if (obj["code"] == null
                    || obj["code"].ToString() != "0")
                {
                    Log("搜索视频时出错：" + obj["message"].ToString());
                    return null;
                }

                JObject data = obj["data"] as JObject;
                if (data == null)
                {
                    return null;
                }

                JArray pages = data["pages"] as JArray;
                if (pages == null)
                {
                    return null;
                }

                SongInfo songInfo = new SongInfo(this, keyword, data["title"].ToString(), new string[] { data["owner"]["name"].ToString() });
                SongItem item = new SongItem(songInfo, "");
                int duration = 0;
                int.TryParse(pages[page - 1]["duration"].ToString(), out duration);
                item.Duration = duration;
                item.FileFormat = FILE_FORMAT;
                item.Extra = page.ToString();

                return item;
            }
            catch (Exception ex)
            {
                Log("搜索视频时出错：" + ex.Message);
                return null;
            }
        }
    }
}