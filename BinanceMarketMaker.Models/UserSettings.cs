using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BinanceMarketMaker.Models
{
   public class UserSettings
    {
        public string ApiKey { get; set; }
        public string SecretKey { get; set; }
        public decimal BTC { get; set; }
        public decimal ETH { get; set; }
        public decimal LTC { get; set; }
        public decimal USDT { get; set; }
    }
}
