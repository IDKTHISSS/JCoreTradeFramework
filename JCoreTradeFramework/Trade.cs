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

        public static async Task<bool> SendTrade(JCSteamAccountInstance Account, string TradeLink, List<CSGOItem> items)
        {
            if (Account.AccountInfo.MaFile == null) return false;
            await Account.AccountInfo.CheckSession();
            ChromeDriverService service = ChromeDriverService.CreateDefaultService(AppDomain.CurrentDomain.BaseDirectory + @"\Dependents\chromedriver.exe");

            service.HideCommandPromptWindow = true;
            ChromeOptions options = new ChromeOptions();
            options.AddArgument("--window-size=1920,1080");
            options.AddArgument("--disable-gpu");
            options.AddArgument("--disable-extensions");
            options.AddArgument("--proxy-server='direct://'");
            options.AddArgument("--proxy-bypass-list=*");
            options.AddArgument("--start-maximized");
            options.AddArgument("--headless");
            options.AddArgument("no-sandbox");
            IWebDriver driver = new ChromeDriver(service, options);
            CookieContainer cookieContainer = Utils.GetCookies(Account.AccountInfo.MaFile.Session);
            driver.Navigate().GoToUrl(TradeLink);
            CookieCollection Cookies = cookieContainer.GetCookies(new Uri("https://steamcommunity.com/"));
            foreach (System.Net.Cookie cookie in Cookies)
            {
                try
                {
                    if (cookie.Name != "mobileClient" && cookie.Name != "mobileClientVersion")
                        driver.Manage().Cookies.AddCookie(new OpenQA.Selenium.Cookie(cookie.Name, cookie.Value));

                }
                catch (Exception ex)
                {
                    return false;
                }

            }

            driver.Navigate().Refresh();

            try
            {
                if (driver.FindElement(By.XPath("//*[@id=\"headline\"]")).Displayed)
                {
                    //Utils.ShowDialog("Steam dont work.");
                }
            }
            catch (Exception ex)
            {

            }
            Thread.Sleep(2000);
            try
            {

                IJavaScriptExecutor js = (IJavaScriptExecutor)driver;
                js.ExecuteScript("arguments[0].click();", driver.FindElement(By.XPath("//*[@id=\"appselect_activeapp\"]")));
                js.ExecuteScript("arguments[0].click();", driver.FindElement(By.XPath("//*[@id=\"appselect_option_you_730_2\"]")));
                Thread.Sleep(500);
                Console.WriteLine(items.Count);
                Actions action = new Actions(driver);
                foreach (var item in items)
                {
                    IJavaScriptExecutor jsExecutor = (IJavaScriptExecutor)driver;
                    string script = $@"var element = document.evaluate('//*[@id=""item730_2_{item.ID.ToString()}""]', document, null, XPathResult.FIRST_ORDERED_NODE_TYPE, null).singleNodeValue;
                          var event = new MouseEvent('dblclick', {{ bubbles: true, cancelable: true, view: window }});
                          element.dispatchEvent(event);";
                    jsExecutor.ExecuteScript(script);
                    //Thread.Sleep(500);
                }
                Thread.Sleep(500);
                js.ExecuteScript("arguments[0].click();", driver.FindElement(By.XPath("//*[@id=\"you_notready\"]")));
                Thread.Sleep(500);
                js.ExecuteScript("arguments[0].click();", driver.FindElement(By.XPath("/html/body/div[3]/div[3]/div/div[2]/div[1]/span")));
                Thread.Sleep(500);
                js.ExecuteScript("arguments[0].click();", driver.FindElement(By.XPath("//*[@id=\"trade_confirmbtn\"]")));
                Thread.Sleep(2000);
                driver.Quit();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
                /*                Utils.ShowDialog(ex.Message);*/
            }
            Confirmation[] confirmations = await Account.AccountInfo.MaFile.FetchConfirmationsAsync();
            await Account.AccountInfo.MaFile.AcceptConfirmation(confirmations.Last());
            return true;
        }
    }
}
