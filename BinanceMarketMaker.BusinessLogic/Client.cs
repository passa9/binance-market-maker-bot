using BinanceMarketMaker.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace BinanceMarketMaker.BusinessLogic
{
    public class Client
    {
        private const string address = "http://binancemarketmaker.azurewebsites.net/"; // "http://binancemarketmaker.somee.com"; //"http://localhost:50107/";//
        private CookieContainer cookies;

        public void SendUserSettings(UserSettings userSettings) {
            string path = "/api/usersettings";
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(userSettings);

            bool result = DoPostRequest<bool>(path, json);

        }

        public void SendOrder(OrderCompleted orderCompleted)
        {
            string path = "/api/ordercompleted";
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(orderCompleted);

            bool result = DoPostRequest<bool>(path, json);

        }

        private T DoGetRequest<T>(string path)
        {
            string url = address + path;
            HttpWebRequest request = WebRequest.CreateHttp(url);
            request.Method = "GET";
            request.CookieContainer = cookies;

            string textContent;
            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            {
                var stream = response.GetResponseStream();
                using (StreamReader reader = new StreamReader(stream))
                {
                    textContent = reader.ReadToEnd();
                }

                response.Close();
            }

            var data = Newtonsoft.Json.JsonConvert.DeserializeObject<T>(textContent);

            return data;
        }

        private T DoPostRequest<T>(string path, string json)
        {
            string url = address + path;

            HttpWebRequest req = WebRequest.CreateHttp(url);
            req.CookieContainer = cookies;
            req.UserAgent = "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.1; .NET CLR 1.0.3705;)";
            req.Method = "POST";
            req.KeepAlive = true;
            req.Headers.Add("Keep-Alive: 300");
            req.AllowAutoRedirect = false;

            req.ContentType = "application/json";

            StreamWriter sw = new StreamWriter(req.GetRequestStream());

            sw.Write(json);
            sw.Close();

            HttpWebResponse response = (HttpWebResponse)req.GetResponse();

            StreamReader reader = new StreamReader(response.GetResponseStream());
            string bodyContent = reader.ReadToEnd();

            var data = Newtonsoft.Json.JsonConvert.DeserializeObject<T>(bodyContent);

            return data;
        }
    }
}
