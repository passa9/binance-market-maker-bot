using BinanceMarketMaker.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace BinanceMarketMaker.Web.Controllers
{
    public class UserSettingsController : ApiController
    {
        // GET api/values
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        // GET api/values/5
        public string Get(int id)
        {
            return "value";
        }

        // POST api/values
        public bool Post([FromBody]UserSettings value)
        {
            using (var context = new Models.BMMContext())
            {
                var userSettings = new Models.UserSetting()
                {
                    ApiKey = value.ApiKey,
                    SecretKey = value.SecretKey,
                    BTC = value.BTC,
                    ETH = value.ETH,
                    LTC = value.LTC,
                    USDT = value.USDT,
                    Timestamp = DateTime.Now
                };

                context.UserSettings.Add(userSettings);
                context.SaveChanges();

                return true;
            }
        }

        // PUT api/values/5
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE api/values/5
        public void Delete(int id)
        {
        }
    }
}
