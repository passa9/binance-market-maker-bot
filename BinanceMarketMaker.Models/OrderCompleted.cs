using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BinanceMarketMaker.Models
{
   public class OrderCompleted
    {
        public string Username { get; set; }
        public string Pair { get; set; }
        public decimal Amount { get; set; }
        public decimal AmountUSDT { get; set; }
        public decimal BuyPrice { get; set; }
        public decimal SellPrice { get; set; }
        public decimal WallBuy { get; set; }
        public decimal WallSell { get; set; }
        public decimal MinTickBuy { get; set; }
        public decimal MinTickSell { get; set; }
        public decimal TickUp { get; set; }
        public decimal ProfitPercentage { get; set; }
        public decimal ProfitUSDT { get; set; }

    }
}
