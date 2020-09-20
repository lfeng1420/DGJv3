using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Threading;
using System.Threading.Tasks;

namespace DGJv3.InternalModule
{
    internal class LwlApiBaseModule : SearchModule
    {
        protected enum EnRequestType
        {
            GET,
            POST,
        };

        private string ServiceName = "undefined";
        protected void SetServiceName(string name) => ServiceName = name;

        protected const string INFO_PREFIX = "";
        protected const string INFO_AUTHOR = "Genteure & LWL12";
        protected const string INFO_EMAIL = "dgj@genteure.com";
        protected const string INFO_VERSION = "1.1";

        internal static int RoomId = 0;

        internal LwlApiBaseModule()
        {
            IsPlaylistSupported = true;
        }

        protected override DownloadStatus Download(SongItem item)
        {
            throw new NotImplementedException();
        }

        protected override string GetDownloadUrl(SongItem songInfo)
        {
            return $"https://music.163.com/song/media/outer/url?id={songInfo.SongId}";
        }

        protected override string GetLyricById(string Id)
        {
            return null;
        }

        protected override List<SongInfo> GetPlaylist(string keyword)
        {
            return null;
        }

        protected override SongInfo Search(string keyword)
        {
            return null;
        }

        protected override ModuleType GetModuleType()
        {
            return ModuleType.Invalid;
        }

        protected static string Fetch(string prot, string host, string path, bool getflag = true, string refer = "", string data = "")
        {
            for (int retryCount = 0; retryCount < 4; retryCount++)
            {
                try
                {
                    return Fetch_exec(prot, host, path, getflag, refer, data);
                }
                catch (WebException)
                {
                    if (retryCount >= 3)
                    {
                        throw;
                    }

                    continue;
                }
            }

            return null;
        }

        private static string Fetch_exec(string prot, string host, string path, bool getflag = true, string refer = "", string data = "")
        {
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(new Uri(prot + host + path));
                request.Method = getflag ? "GET" : "POST";
                request.ContentType = "application/x-www-form-urlencoded";
                if (refer != "")
                {
                    request.Referer = refer;
                }

                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                {
                    using (var reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8))
                    {
                        return reader.ReadToEnd();
                    }
                }
            }
            catch (Exception)
            {
                return null;
            }
        }
        private static string Fetch(string url)
        {
            var request = (HttpWebRequest)WebRequest.Create(url);
            request.Timeout = 10000;
            var response = (HttpWebResponse)request.GetResponse();
            var responseString = new StreamReader(response.GetResponseStream(), Encoding.UTF8).ReadToEnd();
            return responseString;
        }

        private static bool GetDNSResult(string domain, out string result)
        {
            if (DNSList.TryGetValue(domain, out DNSResult result_from_d))
            {
                if (result_from_d.TTLTime > DateTime.Now)
                {
                    result = result_from_d.IP;
                    return true;
                }
                else
                {
                    DNSList.Remove(domain);
                    if (RequestDNSResult(domain, out DNSResult? result_from_api, out Exception exception))
                    {
                        DNSList.Add(domain, result_from_api.Value);
                        result = result_from_api.Value.IP;
                        return true;
                    }
                    else
                    {
                        result = null;
                        return false;
                    }
                }
            }
            else
            {
                if (RequestDNSResult(domain, out DNSResult? result_from_api, out Exception exception))
                {
                    DNSList.Add(domain, result_from_api.Value);
                    result = result_from_api.Value.IP;
                    return true;
                }
                else
                {
                    result = null;
                    return false;
                }
            }
        }
        private static bool RequestDNSResult(string domain, out DNSResult? dnsResult, out Exception exception)
        {
            dnsResult = null;
            exception = null;

            try
            {
                var http_result = Fetch("http://119.29.29.29/d?ttl=1&dn=" + domain);
                if (http_result == string.Empty)
                {
                    return false;
                }

                var m = regex.Match(http_result);
                if (!m.Success)
                {
                    exception = new Exception("HTTPDNS 返回结果不正确");
                    return false;
                }

                dnsResult = new DNSResult()
                {
                    IP = m.Groups[1].Value,
                    TTLTime = DateTime.Now + TimeSpan.FromSeconds(double.Parse(m.Groups[2].Value))
                };
                return true;
            }
            catch (Exception ex)
            {
                exception = ex;
                return false;
            }
        }

        [Obsolete("Use GetLyricById instead", true)]
        protected override string GetLyric(SongItem songInfo)
        {
            throw new NotImplementedException();
        }

        private static readonly Dictionary<string, DNSResult> DNSList = new Dictionary<string, DNSResult>();
        private static readonly Regex regex = new Regex(@"((?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?))\,(\d+)", RegexOptions.Compiled);
        private struct DNSResult
        {
            internal string IP;
            internal DateTime TTLTime;
        }

        public override bool DecodeFile(SongItem song)
        {
            return true;
        }
    }
}
