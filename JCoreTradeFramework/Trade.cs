using JCorePanelBase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SteamKit2;
using SteamKit2.Authentication;
using SteamKit2.Internal;
using SteamAuth;
using System.Threading.Tasks;
using System.Xml.Linq;
using Newtonsoft.Json.Linq;
using System.IO;
using Newtonsoft.Json;
using static SteamKit2.Internal.StoreItem;
using System.Collections.Specialized;
using System.Net;
using System.Runtime.CompilerServices;
using static JCoreTradeFramework.TradeUtils;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium;
using System.Threading;
using static SteamKit2.Internal.CMsgRemoteClientBroadcastStatus;
using System.Net.Mime;
using RestSharp;
using System.IO.Compression;

namespace JCoreTradeFramework
{
    public static class Trade
    {
        public static async Task<List<CSGOItem>> GetCSGOInventoryAsync(JCSteamAccount Account)
        {
            if (Account.MaFile == null) return new List<CSGOItem>();
            List<CSGOItem> AccountInventory = new List<CSGOItem>();
            await Account.CheckSession();

            string text = await SteamWeb.GETRequest("https://steamcommunity.com/my/inventory/json/730/2", Account.MaFile.Session.GetCookies());
            JArray JsonItems = new JArray(JObject.Parse(text)["rgInventory"].Values());
            JArray JsonItemsDescriptions = new JArray(JObject.Parse(text)["rgDescriptions"].Values());
            foreach (var item in JsonItems)
            {
                CSGOItem NewItem = new CSGOItem();
                NewItem.ID = item.Value<Int64>("id");
                NewItem.ClassID = item.Value<Int64>("classid");
                NewItem.InstanceId = item.Value<Int64>("instanceid");
               
                foreach (var Desc in JsonItemsDescriptions)
                {
                    if (Desc.Value<Int64>("classid") == NewItem.ClassID)
                    {
                        NewItem.Name = Desc.Value<string>("name");
                        NewItem.IsTradeble = Desc.Value<bool>("tradable");
                        foreach (var tag in Desc.Value<JArray>("tags"))
                        {
                           
                            if (tag.Value<string>("category") == "Rarity")
                            {
                                NewItem.Rarity = tag.Value<string>("internal_name");

                            }
                            if (tag.Value<string>("category") == "Type")
                            {
                                NewItem.Type = tag.Value<string>("internal_name");
                            }
                        }
                    }
                }
                AccountInventory.Add(NewItem);
            }
            return AccountInventory;
        }
        private static DateTime? _inventoryTime;
        private static long GetTimeStampForInventoryRequest()
        {
            if (_inventoryTime == null)
                _inventoryTime = DateTime.UtcNow;

            if (_inventoryTime < DateTime.UtcNow - TimeSpan.FromHours(1))
                _inventoryTime = DateTime.UtcNow;

            return _inventoryTime.Value.ToTimeStamp();
        }
        public static async Task<bool> SendTrade(JCSteamAccountInstance Account, string TradeLink, List<CSGOItem> items)
        {
            if (Account.AccountInfo.MaFile == null) return false;
            await Account.AccountInfo.CheckSession();
            var tradeObjects = new List<object>();
            foreach (var item in items)
            {
                tradeObjects.Add(new
                {
                    appid = 730,
                    contextid = 2,
                    amount = 1,
                    assetid = item.ID.ToString(),
                });
            }
            JsonConvert.SerializeObject(tradeObjects).ToString();
            Uri uri = new Uri(TradeLink);
            string partnerId = uri.QueryParameters("partner");
            string token = uri.QueryParameters("token");
            var data = new List<(string key, string value)>
        {
            ("sessionid", Account.AccountInfo.MaFile.Session.SessionID),
            ("serverid", "1"),
            ("partner", $"{76561197960265728 + Int64.Parse(partnerId)}"),
            ("tradeoffermessage", "test"),
            ("json_tradeoffer", "{\"newversion\":true,\"version\":2,\"me\":{\"assets\":" + JsonConvert.SerializeObject(tradeObjects) + ",\"currency\":[],\"ready\":false},\"them\":{\"assets\":[],\"currency\":[],\"ready\":false}}"),
            ("captcha", ""),
            ("trade_offer_create_params", $"{{\"trade_offer_access_token\":\"{token}\"}}")
        };
            var referer = $"https://steamcommunity.com/tradeoffer/new/?partner={partnerId}";

            Console.WriteLine(GetTimeStampForInventoryRequest());
            var cookies = CreateCookies(Account);
            cookies.Add(new System.Net.Cookie("webTradeEligibility",
                    $"%7B%22allowed%22%3A1%2C%22allowed_at_time%22%3A0%2C%22steamguard_required_days%22%3A15%2C%22new_device_cooldown_days%22%3A0%2C%22time_checked%22%3A{GetTimeStampForInventoryRequest()}%7D")
            { Domain = "steamcommunity.com" });
           
            CancellationToken cancellationToken = default;
            try
            {
                RestResponse response = await ExecutePostRequestAsync(
                 "https://steamcommunity.com/tradeoffer/new/send",
                 cookies,
                 null,
                 referer,
                  string.Join("&", data.Select(t => $"{WebUtility.UrlEncode(t.key)}={WebUtility.UrlEncode(t.value)}")),
                 cancellationToken);
                Console.WriteLine(response.StatusCode);
                Console.WriteLine(response.Content);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            Thread.Sleep(200);
              Confirmation[] confirmations = await Account.AccountInfo.MaFile.FetchConfirmationsAsync();
              await Account.AccountInfo.MaFile.AcceptConfirmation(confirmations.Last());
            return true;
        }
        public static CookieContainer CreateCookies(JCSteamAccountInstance accountInstance)
        {
            var container = new CookieContainer();

            container.Add(new System.Net.Cookie("steamid", accountInstance.AccountInfo.MaFile.Session.SteamID.ToString(), "/", ".steamcommunity.com"));
            container.Add(new System.Net.Cookie("sessionid", accountInstance.AccountInfo.MaFile.Session.SessionID, "/", ".steamcommunity.com"));

            container.Add(new System.Net.Cookie("steamLoginSecure", accountInstance.AccountInfo.MaFile.Session.SteamID.ToString() + "%7C%7C" + accountInstance.AccountInfo.MaFile.Session.AccessToken, "/", ".steamcommunity.com")
            {
                HttpOnly = true,
                Secure = true
            });
            container.Add(new System.Net.Cookie("bCompletedTradeOfferTutorial", "true", "/", ".steamcommunity.com"));
            container.Add(new System.Net.Cookie("Steam_Language", "english", "/", ".steamcommunity.com"));
            container.Add(new System.Net.Cookie("dob", "", "/", ".steamcommunity.com"));

            return container;
        }
        private static void AddHeadersToRequest(RestRequest request, string referer = "https://steamcommunity.com")
        {
            request.AddHeader("Accept", "application/json, text/javascript;q=0.9, */*;q=0.5");
            request.AddHeader("UserAgent",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/108.0.0.0 Safari/537.36");
            request.AddHeader("Accept-Encoding", "gzip, deflate");
            request.AddHeader("Content-Type", "application/x-www-form-urlencoded; charset=UTF-8");

            if (referer != null)
                request.AddHeader("Referer", referer);
        }
        public static async Task<RestResponse> ExecutePostRequestAsync(string url, CookieContainer cookies,
       IEnumerable<(string name, string value)> headers, string referer,
       string body,
       CancellationToken cancellationToken = default)
        {
            RestClient RestClient = new RestClient(
            new RestClientOptions
            {
                FollowRedirects = true,
                AutomaticDecompression = DecompressionMethods.GZip,
            }); ;

            var request = new RestRequest(url, Method.Post)
            {
                CookieContainer = cookies,
            };

            AddHeadersToRequest(request, referer);

            if (headers != null)
                foreach (var (name, value) in headers)
                    request.AddHeader(name, value);

            request.AddBody(body, RestSharp.ContentType.FormUrlEncoded);

            var response = await RestClient.ExecuteAsync(request, cancellationToken);

            return response;
        }
    }
   
    public static class UriExtensions
    {
        public static string QueryParameters(this Uri uri, string paramName)
        {
            var queryDictionary = System.Web.HttpUtility.ParseQueryString(uri.Query);
            return queryDictionary[paramName];
        }
    }
    public static class TimeHelpers
    {
        public static long ToTimeStamp(this DateTime dateTime) =>
            (long)dateTime.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;

        public static DateTime FromTimeStamp(long timestamp) =>
            new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddSeconds(timestamp);
    }

}
