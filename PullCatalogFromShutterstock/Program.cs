using System;
using System.IO;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PullCatalogFromShutterstock
{
    class Program
    {
        static private void SaveJsonFromSSID(IWebDriver webDriver, string path, string SSID)
        {
            string url = "https://submit.shutterstock.com/api/content_editor/media/P" + SSID;
            string prefix = "{\"data\":{";
            string suffix = "</pre></body><span";
            string data = GetInnerPageSource(webDriver, prefix, suffix, url);
            ForceWrite(path + SSID + ".json", data);
        }

        private static void ForceWrite(string fname, string data)
        {
            if (File.Exists(fname))
            {
                File.Delete(fname);
            }

            if (data != null)
            {
                using (TextWriter f = File.CreateText(fname))
                {
                    f.Write(data);
                }
            }
        }

        private static string GetInnerPageSource(IWebDriver webDriver, string prefix, string suffix, string url)
        {
            webDriver.Navigate().GoToUrl(url);
            string pageSource = webDriver.PageSource;
            
            int start = pageSource.IndexOf(prefix);
            int end = pageSource.IndexOf(suffix);

            if (start > 0 && end > 0)
            {
                return pageSource.Substring(start, end - start);
            }
            else
            {
                return pageSource;
            }
        }

        private static void EnumerateCatalog(IWebDriver webDriver, string path)
        {
            string prefix = "{\"responseHeader\":{";
            string suffix = "</pre></body><span";
            int page = 1;
            int perPage = 100;
            string catalogFormat = "https://submit.shutterstock.com/api/catalog_manager/media_types/all/items?filter_type=keywords&filter_value=&page_number={0}&per_page={1}&sort=popular";

            int foundCount;
            do
            {
                string url = string.Format(catalogFormat, page, perPage);
                string data = GetInnerPageSource(webDriver, prefix, suffix, url);

                JObject o = JsonConvert.DeserializeObject(data) as JObject;
                JToken t = null;
                foundCount = 0;

                if (o.TryGetValue("data", out t))
                {
                    foreach (JToken photo in t.Children())
                    {
                        JToken SSID = photo.SelectToken("media_id");
                        if (SSID != null)
                        {
                            SaveJsonFromSSID(webDriver, path, SSID.ToString());
                            foundCount++;
                        }
                    }
                }

                page++;
            } while (foundCount > 0);
        }

        static void Main(string[] args)
        {
            if (false)
            {
                ChromeOptions o = new ChromeOptions();
                o.AddArgument("user-data-dir=C:\\Users\\chand\\AppData\\Local\\Google\\Chrome\\User Data");
                o.AddArgument("profile-dir=C:\\Users\\chand\\AppData\\Local\\Google\\Chrome\\User Data");

                IWebDriver webDriver;
                webDriver = new ChromeDriver(o);

                string path = "C:\\Photos\\Shutterstock\\";
                EnumerateCatalog(webDriver, path);

                webDriver.Close();
                webDriver.Quit();
            }
            else
            {
                string folder = "C:\\Photos\\Shutterstock\\";
                MergeToFlatFile merge = new MergeToFlatFile();
                foreach(string filename in Directory.EnumerateFiles(folder, "*.json"))
                {
                    Console.WriteLine(filename);
                    merge.Add(filename);
                }

                merge.Save(folder + "catalog.tsv");
            }
        }
    }
}
