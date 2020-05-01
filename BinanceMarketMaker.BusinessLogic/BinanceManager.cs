using BinanceMarketMaker.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BinanceMarketMaker.BusinessLogic
{
    public class BinanceManager
    {
        Binance.Net.BinanceClient binanceClient;
        IList<Binance.Net.Objects.BinanceSymbol> symbols;

        public BinanceManager(Binance.Net.BinanceClient binanceClient)
        {
            this.binanceClient = binanceClient;
        }

        public void Init()
        {
            var result = binanceClient.GetExchangeInfo();
            if (!result.Success)
                throw new Exception(result.Error.Message);

            symbols = result.Data.Symbols.ToList();
        }

        public Order CreateOrder(Strategy strategy)
        {
            var order = new Order()
            {
                AmountUSDT = strategy.QuantityUSDT,
                Amount = strategy.Quantity,
                StartDate = DateTime.Now,
                BAlfa = strategy.WallBuy,
                BAlfaUSDT = strategy.WallBuyUSDT,
                BBeta = strategy.MinGapBuy,
                Pair = strategy.Pair,
                Processing = false,
                SAlfa = strategy.WallSell,
                SAlfaUSDT = strategy.WallSellUSDT,
                SBeta = strategy.MinGapSell,
                Status = Status.WaitBuy,
                TickUp = strategy.TickUp,
                Guid = Guid.NewGuid().ToString(),
            };

            return order;
        }


        public async Task<IList<Ticker>> GetTickersAsync(string quoteAsset)
        {
            IList<Ticker> tickers = new List<Ticker>();
            var result = await binanceClient.Get24HPricesListAsync();

            if (!result.Success)
                throw new Exception(result.Error.Message);

            var h24PricesList = result.Data.Where(x => x.Symbol.EndsWith(quoteAsset));

            foreach (var h24Price in h24PricesList)
            {
                var symbol = symbols.Single(x => x.Name == h24Price.Symbol);
                var spread = h24Price.AskPrice == 0 ? 0 : ((h24Price.AskPrice - h24Price.BidPrice) / h24Price.AskPrice) * 100;
                spread = Math.Round(spread, 3);
                var tickSpread = (h24Price.AskPrice - h24Price.BidPrice) / symbol.PriceFilter.TickSize;

                var ticker = new Ticker()
                {
                    Ask = h24Price.AskPrice,
                    Bid = h24Price.BidPrice,
                    ChangeH24 = h24Price.PriceChangePercent,
                    High = h24Price.HighPrice,
                    Last = h24Price.LastPrice,
                    Low = h24Price.LowPrice,
                    Pair = h24Price.Symbol,
                    QuoteVolume = h24Price.QuoteVolume,
                    Spread = spread,
                    TickSpread = tickSpread,
                    Trades = h24Price.Trades
                };

                tickers.Add(ticker);
            }

            return tickers;
        }

        public async Task<decimal> ConvertUSDTAsync(decimal quantityUSDT, string pair)
        {

            var result = await binanceClient.Get24HPriceAsync("BTCUSDT");

            if (!result.Success)
                throw new Exception(result.Error.Message);

            var btcUSDTPrice = result.Data.LastPrice;
            var btcQuantity = quantityUSDT / btcUSDTPrice;

             result = await binanceClient.Get24HPriceAsync(pair);

            if (!result.Success)
                throw new Exception(result.Error.Message);

            var price = result.Data.LastPrice;
            var quantity = btcQuantity / price;

            var symbol = symbols.Single(x => x.Name == pair);
            quantity = quantity - (quantity % symbol.LotSizeFilter.StepSize);
            quantity = Math.Round(quantity, 8);

            return quantity;
        }


    }
}
