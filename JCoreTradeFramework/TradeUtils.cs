using SteamAuth;
using SteamKit2;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace JCoreTradeFramework
{
    public static class TradeUtils
    {
        public static List<asset> GenerateAssetList(List<CSGOItem> items)
        {
            var list = new List<asset>();
            foreach (var item in items)
            {
                list.Add(new asset
                {
                    appid = 730,
                    contextid = "2",
                    amount = "1",
                    assetid = item.ID.ToString()
                });
            }
            return list;
        }
        public static CookieContainer GetCookies(SessionData session)
        {

            CookieContainer cookieContainer_0 = new CookieContainer();
            cookieContainer_0.Add(new Cookie("mobileClientVersion", "777777 3.1.0", "/", ".steamcommunity.com"));
            cookieContainer_0.Add(new Cookie("mobileClient", "android", "/", ".steamcommunity.com"));
            cookieContainer_0.Add(new Cookie("steamid", session.SteamID.ToString(), "/", ".steamcommunity.com"));
            cookieContainer_0.Add(new Cookie("steamLogin", session.SteamID.ToString() + "%7C%7C" + session.AccessToken, "/", ".steamcommunity.com")
            {
                HttpOnly = true
            });
            cookieContainer_0.Add(new Cookie("steamLoginSecure", session.SteamID.ToString() + "%7C%7C" + session.AccessToken, "/", ".steamcommunity.com")
            {
                HttpOnly = true,
                Secure = true
            });
            cookieContainer_0.Add(new Cookie("Steam_Language", "english", "/", ".steamcommunity.com"));
            cookieContainer_0.Add(new Cookie("dob", "", "/", ".steamcommunity.com"));
            cookieContainer_0.Add(new Cookie("sessionid", GetRandomHexNumber(32), "/", ".steamcommunity.com"));
            cookieContainer_0.Add(new Cookie("steamCurrencyId", "1", "/", ".steamcommunity.com"));
            return cookieContainer_0;
        }
        private static string GetRandomHexNumber(int digits)
        {
            Random random = new Random();
            byte[] array = new byte[digits / 2];
            random.NextBytes(array);
            string text = string.Concat(array.Select((byte x) => x.ToString("X2")).ToArray());
            if (digits % 2 == 0)
            {
                return text;
            }

            return text + random.Next(16).ToString("X");
        }
        public static string Request(string string_0, string string_1, NameValueCollection nameValueCollection_0 = null, CookieContainer cookieContainer_0 = null, NameValueCollection nameValueCollection_1 = null, string string_2 = "https://steamcommunity.com")
        {
            string text = ((nameValueCollection_0 != null) ? string.Join("&", Array.ConvertAll(nameValueCollection_0.AllKeys, (string key) => $"{WebUtility.UrlEncode(key)}={WebUtility.UrlEncode(nameValueCollection_0[key])}")) : string.Empty);
            if (string_1 == "GET")
            {
                string_0 = string_0 + (string_0.Contains("?") ? "&" : "?") + text;
            }
            return Request(string_0, string_1, text, cookieContainer_0, nameValueCollection_1, string_2);
        }

        public static string Request(string string_0, string string_1, string string_2 = null, CookieContainer cookieContainer_0 = null, NameValueCollection nameValueCollection_0 = null, string string_3 = "https://steamcommunity.com")
        {
            try
            {
                HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(string_0);
                httpWebRequest.Method = string_1;
                httpWebRequest.Accept = "text/javascript, text/html, application/xml, text/xml, */*";
                if (string_0.Contains("steamcommunity.com"))
                {
                    httpWebRequest.Host = "steamcommunity.com";
                }
                httpWebRequest.UserAgent = "Dalvik/2.1.0 (Linux; U; Android 6.0; Android SDK built for x86 Build/MASTER; Valve Steam App Version/3)";
                httpWebRequest.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
                httpWebRequest.Referer = string_3;
                if (nameValueCollection_0 != null)
                {
                    httpWebRequest.Headers.Add(nameValueCollection_0);
                }
                if (cookieContainer_0 != null)
                {
                    httpWebRequest.CookieContainer = cookieContainer_0;
                }
                if (string_1 == "POST")
                {
                    httpWebRequest.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
                    httpWebRequest.ContentLength = string_2.Length;
                    StreamWriter streamWriter = new StreamWriter(httpWebRequest.GetRequestStream());
                    streamWriter.Write(string_2);
                    streamWriter.Close();
                }
                HttpWebResponse httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                if (httpWebResponse.StatusCode == HttpStatusCode.OK)
                {
                    StreamReader streamReader = new StreamReader(httpWebResponse.GetResponseStream());
                    return streamReader.ReadToEnd();
                }
                Console.WriteLine(httpWebResponse.StatusCode.ToString());
                return null;
            }
            catch (WebException ex)
            {
                HttpWebResponse httpWebResponse2 = ex.Response as HttpWebResponse;
                using (StreamReader streamReader2 = new StreamReader(httpWebResponse2.GetResponseStream()))
                {
                    string text = streamReader2.ReadToEnd();
                    if (httpWebResponse2.StatusCode == HttpStatusCode.Unauthorized && text.Contains("Access is denied. Retrying will not help. Please verify your"))
                    {
                        return "OAuthTokenExpired";
                    }
                }
                Console.WriteLine(ex);
                return null;
            }
        }

    }
    public class asset
    {
        [CompilerGenerated]
        private int int_0;

        [CompilerGenerated]
        private string string_0;

        [CompilerGenerated]
        private string string_1;

        [CompilerGenerated]
        private string string_2;

        public int appid
        {
            [CompilerGenerated]
            get
            {
                return int_0;
            }
            [CompilerGenerated]
            set
            {
                int_0 = value;
            }
        }

        public string contextid
        {
            [CompilerGenerated]
            get
            {
                return string_0;
            }
            [CompilerGenerated]
            set
            {
                string_0 = value;
            }
        }

        public string amount
        {
            [CompilerGenerated]
            get
            {
                return string_1;
            }
            [CompilerGenerated]
            set
            {
                string_1 = value;
            }
        }

        public string assetid
        {
            [CompilerGenerated]
            get
            {
                return string_2;
            }
            [CompilerGenerated]
            set
            {
                string_2 = value;
            }
        }
    }
}
