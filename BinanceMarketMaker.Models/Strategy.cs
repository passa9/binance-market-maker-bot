using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BinanceMarketMaker.Models
{
    public class Strategy
    {
        public string Pair { get; set; }
        public decimal QuantityUSDT { get; set; }
        public decimal Quantity { get; set; }
        public int TickUp { get; set; }
        public decimal WallBuyUSDT { get; set; }
        public decimal WallBuy { get; set; }
        public decimal WallSellUSDT { get; set; }
        public decimal WallSell { get; set; }
        public decimal MinGapBuy { get; set; }
        public decimal MinGapSell { get; set; }
    }
}
