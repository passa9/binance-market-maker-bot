using BinanceMarketMaker.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace BinanceMarketMaker.Web.Controllers
{
    public class LicenseKeyController : ApiController
    {
        // GET api/values
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        // GET api/values/5
        public bool Get(string licenseKey)
        {
            using (var context = new BinanceMarketMaker.Web.Models.BMMContext())
            {
                var result = context.LicenseKeys.Any(x => x.Key == licenseKey);
                return result;
            }
        }

        // POST api/values
        public void Post([FromBody]OrderCompleted value)
        {

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
