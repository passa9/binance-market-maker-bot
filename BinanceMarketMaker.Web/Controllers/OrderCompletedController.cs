using BinanceMarketMaker.Models;
using System;
using System.Collections.Generic;
using System.Web.Http;

namespace BinanceMarketMaker.Web.Controllers
{
    public class OrderCompletedController : ApiController
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
        public bool Post([FromBody]OrderCompleted value)
        {
            using (var context = new Models.BMMContext())
            {
                var order = new Models.OrderCompleted()
                {
                    Amount = value.Amount,
                    AmountUSDT = value.AmountUSDT,
                    BuyPrice = value.BuyPrice,
                    MinTickBuy = value.MinTickBuy,
                    MinTickSell = value.MinTickSell,
                    Pair = value.Pair,
                    ProfitPercentage = value.ProfitPercentage,
                    ProfitUSDT = value.ProfitUSDT,
                    SellPrice = value.SellPrice,
                    TickUp = value.TickUp,
                    Timestamp = DateTime.Now,
                    Username = value.Username,
                    WallBuy = value.WallBuy,
                    WallSell = value.WallSell
                };

                context.OrderCompleteds.Add(order);
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
