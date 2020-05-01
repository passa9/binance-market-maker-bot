using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BinanceMarketMaker.Models
{
    public class Ticker
    {
        public string Pair { get; set; }
        public decimal Bid { get; set; }
        public decimal Ask { get; set; }
        public decimal Last { get; set; }
        public decimal Spread { get; set; }
        public decimal TickSpread { get; set; }
        public decimal High { get; set; }
        public decimal Low { get; set; }
        public long Trades { get; set; }
        public decimal QuoteVolume { get; set; }
        public decimal ChangeH24 { get; set; }
    }
}
