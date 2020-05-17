using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Binance.Net;
using Binance.Net.Objects;
using BinanceMarketMaker.Models;
using CryptoExchange.Net.Authentication;

namespace BinanceMarketMaker.BusinessLogic
{
    public class BinanceBot
    {
        public ObservableCollection<Order> Orders { get; set; }

        private int interval = 1000;
        private BinanceClient binanceClient;
        private List<BinanceSymbol> symbols;
        private double fees;
        private SynchronizationContext context;
        public BinanceBot(BinanceClient binanceClient, SynchronizationContext context)
        {
            this.context = context;
            Orders = new ObservableCollection<Order>();

            this.binanceClient = binanceClient;
            var webRequest = this.binanceClient.GetExchangeInfo();
            symbols = webRequest.Data.Symbols.ToList();
        }

        public void SetSettings(string apiKey, string secretKey, int interval, double fees)
        {
            binanceClient.SetApiCredentials(apiKey, secretKey);
            this.fees = fees;
            this.interval = interval;
        }

        public Task Start()
        {
            return Task.Run(async () =>
            {
                while (true)
                {
                    try
                    {
                        Task task = Task.Run(async () => await Task.Delay(interval));
                        var orders = Orders.Where(x => x.Processing == false && x.Status != Status.Completed && x.Status != Status.Error).ToList();
                        foreach (var order in orders)
                        {
                            await Process(order);
                        }
                        task.Wait();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                }
            });
        }

        private decimal GetPriceToPlace(string pair, OrderSide side, BinanceOrderBook orderBook, decimal alfa,
            int tickUp)
        {
            var symbol = symbols.Single(x => x.Name == pair);
            var orderbookSide = side == OrderSide.Buy ? orderBook.Bids.ToList() : orderBook.Asks.ToList();

            decimal sum = 0;
            var i = 0;

            while (sum < alfa)
            {
                sum += (orderbookSide[i].Quantity);
                i++;
            }
            i = i == 0 ? 0 : i - 1;
            var price = orderbookSide[i].Price;
            var ticksize = symbol.PriceFilter.TickSize;

            var tickToIncrease = ticksize * tickUp;
            decimal priceToPlace;
            if (side == OrderSide.Buy)
            {
                priceToPlace = price + tickToIncrease;
            }
            else
            {
                priceToPlace = price - tickToIncrease;
            }
            var round = Math.Round(priceToPlace, symbol.BaseAssetPrecision);
            return round;
        }

        private bool CheckBetaTick(string pair, BinanceOrderBook orderbook, decimal price, decimal beta, OrderSide side)
        {
            var availableTick = GetAvailableTick(pair, price, orderbook, side);

            if (availableTick >= beta)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private int GetAvailableTick(string pair, decimal price, BinanceOrderBook orderbook, OrderSide side)
        {
            var orderbookSide = side == OrderSide.Buy ? orderbook.Asks.ToList() : orderbook.Bids.ToList();
            var symbol = symbols.Single(x => x.Name == pair);
            var availableTick = int.Parse((Math.Abs(orderbookSide[0].Price - price) / symbol.PriceFilter.TickSize).ToString("F0"));

            return availableTick;
        }

        private decimal GetAmountToPlace(string pair, decimal amount)
        {
            var symbol = symbols.Single(x => x.Name == pair);
            var module = amount % symbol.LotSizeFilter.StepSize;
            var amountToPlace = amount - module;
            amountToPlace = Math.Round(amountToPlace, 8);
            return amountToPlace;
        }

        private async Task<BinancePlacedOrder> PlaceOrder(string pair, decimal amount, OrderSide side, decimal price)
        {
            var amountToPlace = GetAmountToPlace(pair, amount);
            var orderRequest = (await binanceClient.PlaceOrderAsync(pair, side, OrderType.Limit, amountToPlace, null, null, price, TimeInForce.GoodTillCancel));

            if (!orderRequest.Success)
            {
                throw new Exception(orderRequest.Error.Message);
            }
            var orderPlaced = orderRequest.Data;

            orderPlaced.Price = price;
            orderPlaced.OriginalQuantity = amountToPlace;
            orderPlaced.Side = side;

            return orderPlaced;
        }

        private Task Process(Order order)
        {
            return Task.Run(async () =>
             {
                 try
                 {
                     order.Processing = true;
                     var orderBookRequest = (await binanceClient.GetOrderBookAsync(order.Pair, 50));
                     if (!orderBookRequest.Success)
                     {
                         order.Status = Status.Error;
                         order.ErrorMessage = orderBookRequest.Error.Message;
                         return;
                     }

                     BinanceOrderBook orderbook = orderBookRequest.Data;

                     order.Bid = orderbook.Bids.First().Price;
                     order.Ask = orderbook.Asks.First().Price;

                     if (order.Status == Status.WaitBuy)
                     {
                         var priceToPlace = GetPriceToPlace(order.Pair,
                             OrderSide.Buy,
                             orderbook,
                             order.BAlfa,
                             order.TickUp);

                         var checkBetaTick = CheckBetaTick(order.Pair, orderbook, priceToPlace, order.BBeta,
                             OrderSide.Buy);

                         if (checkBetaTick)
                         {
                             order.BuyOrder = await PlaceOrder(order.Pair, order.Amount, OrderSide.Buy, priceToPlace);
                             order.Status = Status.Buy;
                             order.BuyPrice = priceToPlace;
                         }
                     }
                     else if (order.Status == Status.WaitSell)
                     {
                         var priceToPlace = GetPriceToPlace(order.Pair,
                             OrderSide.Sell,
                             orderbook,
                             order.SAlfa,
                             order.TickUp);

                         var checkBetaTick = CheckBetaTick(order.Pair, orderbook, priceToPlace, order.SBeta,
                             OrderSide.Sell);

                         if (checkBetaTick)
                         {
                             order.SellOrder = await PlaceOrder(order.Pair, order.Amount, OrderSide.Sell, priceToPlace);
                             order.Status = Status.Sell;
                             order.SellPrice = priceToPlace;
                         }
                     }
                     else if (order.Status == Status.Buy)
                     {
                         var statusOrderRequest = await binanceClient.GetOrderAsync(order.Pair, order.BuyOrder.OrderId);

                         if (!statusOrderRequest.Success)
                         {
                             if (statusOrderRequest.Error.Code == -2013)
                             {
                                 return;
                             }
                             order.Status = Status.Error;
                             order.ErrorMessage = statusOrderRequest.Error.Message;

                         }

                         var statusOrder = statusOrderRequest.Data;

                         if (statusOrder.Status == OrderStatus.Filled)
                         {
                             order.BuyOrderCompletedDatetime = DateTime.Now;
                             var priceToPlace = GetPriceToPlace(order.Pair, OrderSide.Sell, orderbook, order.SAlfa,
                                 order.TickUp);

                             var checkBeta = CheckBetaTick(order.Pair, orderbook, priceToPlace, order.SBeta,
                                 OrderSide.Sell);

                             if (checkBeta)
                             {
                                 order.SellOrder = await PlaceOrder(order.Pair, order.BuyOrder.OriginalQuantity,
                                     OrderSide.Sell, priceToPlace);
                                 order.Status = Status.Sell;
                                 order.SellPrice = priceToPlace;
                             }
                             else
                             {
                                 order.Status = Status.WaitSell;
                             }

                         }
                         else if (statusOrder.Status == OrderStatus.New)
                         {
                             var orderBookClear = RemoveMyOrderFromOrderbook(order.BuyOrder.Price,
                                 order.BuyOrder.OriginalQuantity, OrderSide.Buy, orderbook);

                             var priceToPlace = GetPriceToPlace(order.Pair, OrderSide.Buy, orderBookClear, order.BAlfa,
                                 order.TickUp);
                             var checkBeta = CheckBetaTick(order.Pair, orderBookClear, priceToPlace, order.BBeta,
                                 OrderSide.Buy);

                             if (checkBeta)
                             {
                                 if (priceToPlace != order.BuyOrder.Price)
                                 {
                                     BinanceCanceledOrder orderCancelled;
                                     try
                                     {
                                         orderCancelled = await CancelBinanceOrder(order.Pair, order.BuyOrder.OrderId);
                                     }
                                     catch (UnknowOrderException)
                                     {
                                         return;
                                     }
                                     if (orderCancelled.ExecutedQuantity > 0)
                                     {
                                         ManagePartiallyFilledBuy(order, orderCancelled);
                                     }
                                     // Controllare
                                     order.BuyOrder = await PlaceOrder(order.Pair, order.BuyOrder.OriginalQuantity,
                                         OrderSide.Buy, priceToPlace);
                                     order.BuyPrice = priceToPlace;
                                 }
                             }
                             else
                             {

                                 BinanceCanceledOrder orderCancelled;
                                 try
                                 {
                                     orderCancelled = await CancelBinanceOrder(order.Pair, order.BuyOrder.OrderId);
                                 }
                                 catch (UnknowOrderException)
                                 {
                                     return;
                                 }
                                 if (orderCancelled.ExecutedQuantity > 0)
                                 {
                                     ManagePartiallyFilledBuy(order, orderCancelled);
                                 }

                                 order.Status = Status.WaitBuy;
                             }
                         }
                         else if (statusOrder.Status == OrderStatus.Canceled)
                         {

                             var priceToPlace = GetPriceToPlace(order.Pair, OrderSide.Buy, orderbook, order.BAlfa,
                                 order.TickUp);
                             var checkBeta = CheckBetaTick(order.Pair, orderbook, priceToPlace, order.BBeta,
                                 OrderSide.Buy);

                             if (checkBeta)
                             {
                                 // Controllare
                                 order.BuyOrder = await PlaceOrder(order.Pair, order.BuyOrder.OriginalQuantity,
                                     OrderSide.Buy, priceToPlace);
                                 order.BuyPrice = priceToPlace;
                             }
                             else
                             {
                                 order.Status = Status.WaitBuy;
                             }
                         }
                         else if (statusOrder.Status == OrderStatus.PartiallyFilled)
                         {
                             BinanceCanceledOrder orderCancelled;
                             try
                             {
                                 orderCancelled = await CancelBinanceOrder(order.Pair, order.BuyOrder.OrderId);
                             }
                             catch (UnknowOrderException)
                             {
                                 return;
                             }
                             ManagePartiallyFilledBuy(order, orderCancelled);
                             context.Send(x => Orders.Remove(order), null);

                         }
                     }
                     else if (order.Status == Status.Sell)
                     {
                         var orderStatusRequest = await binanceClient.GetOrderAsync(order.Pair, order.SellOrder.OrderId);

                         if (!orderStatusRequest.Success)
                         {
                             order.Status = Status.Error;
                             order.ErrorMessage = orderStatusRequest.Error.Message;
                             return;
                         }

                         BinanceOrder statusOrder = orderStatusRequest.Data;

                         if (statusOrder.Status == OrderStatus.Filled)
                         {
                             order.SellOrderCompletedDatetime = DateTime.Now;
                             CompleteOrder(order);
                         }
                         else if (statusOrder.Status == OrderStatus.New)
                         {
                             var orderBookClear = RemoveMyOrderFromOrderbook(order.SellOrder.Price,
                                 order.BuyOrder.OriginalQuantity, OrderSide.Sell, orderbook);

                             var priceToPlace = GetPriceToPlace(order.Pair,
                                 OrderSide.Sell,
                                 orderBookClear,
                                 order.SAlfa,
                                 order.TickUp);

                             var checkBetaTick = CheckBetaTick(order.Pair, orderbook, priceToPlace, order.SBeta,
                                 OrderSide.Sell);
                             if (checkBetaTick)
                             {

                                 if (priceToPlace != order.SellOrder.Price)
                                 {
                                     BinanceCanceledOrder orderCancelled;
                                     try
                                     {
                                         orderCancelled = await CancelBinanceOrder(order.Pair, order.SellOrder.OrderId);
                                     }
                                     catch (UnknowOrderException)
                                     {
                                         return;
                                     }

                                     if (orderCancelled.ExecutedQuantity > 0)
                                     {
                                         ManagePartiallyFilledSell(order, orderCancelled);
                                         return;
                                     }

                                     order.SellOrder = await PlaceOrder(order.Pair, order.SellOrder.OriginalQuantity,
                                         OrderSide.Sell, priceToPlace);
                                     order.SellPrice = priceToPlace;
                                 }
                             }
                             else
                             {
                                 BinanceCanceledOrder orderCancelled;
                                 try
                                 {
                                     orderCancelled = await CancelBinanceOrder(order.Pair, order.SellOrder.OrderId);
                                 }
                                 catch (UnknowOrderException)
                                 {
                                     return;
                                 }
                                 if (orderCancelled.ExecutedQuantity > 0)
                                 {
                                     ManagePartiallyFilledSell(order, orderCancelled);
                                     return;
                                 }

                                 order.Status = Status.WaitSell;
                             }
                         }
                         else if (statusOrder.Status == OrderStatus.Canceled)
                         {
                             var orderBookClear = RemoveMyOrderFromOrderbook(order.SellOrder.Price,
                                    order.BuyOrder.OriginalQuantity, OrderSide.Sell, orderbook);

                             var priceToPlace = GetPriceToPlace(order.Pair,
                                 OrderSide.Sell,
                                 orderBookClear,
                                 order.SAlfa,
                                 order.TickUp);

                             var checkBetaTick = CheckBetaTick(order.Pair, orderbook, priceToPlace, order.SBeta,
                                 OrderSide.Sell);
                             if (checkBetaTick)
                             {
                                 order.SellOrder = await PlaceOrder(order.Pair, order.SellOrder.OriginalQuantity,
                                     OrderSide.Sell, priceToPlace);
                                 order.SellPrice = priceToPlace;
                             }
                             else
                             {
                                 order.Status = Status.WaitSell;
                             }
                         }

                         else if (statusOrder.Status == OrderStatus.PartiallyFilled)
                         {
                             BinanceCanceledOrder orderCancelled;
                             try
                             {
                                 orderCancelled = await CancelBinanceOrder(order.Pair, order.SellOrder.OrderId);
                             }
                             catch (UnknowOrderException)
                             {
                                 return;
                             }
                             ManagePartiallyFilledSell(order, orderCancelled);
                         }
                     }
                 }
                 catch (Exception ex)
                 {
                     order.Status = Status.Error;
                     order.ErrorMessage = ex.Message;
                 }
                 finally
                 {
                     order.Processing = false;
                 }
             });
        }

        private async Task<BinanceCanceledOrder> CancelBinanceOrder(string pair, long orderId)
        {

            var cancellOlderRequest = await binanceClient.CancelOrderAsync(pair, orderId);
            if (!cancellOlderRequest.Success)
            {
                if (cancellOlderRequest.Error.Code == -2011)
                {
                    throw new UnknowOrderException();
                }
                throw new Exception(cancellOlderRequest.Error.Message);
            }
            return cancellOlderRequest.Data;
        }

        private void CompleteOrder(Order order)
        {
            var sellRebate =(1 - ((decimal)fees / 100)) * order.SellOrder.Price;
            var buyRebate = (1 + ((decimal)fees / 100)) * order.BuyOrder.Price;

            var quantityBTC = (sellRebate - buyRebate) * order.BuyOrder.OriginalQuantity;
            var btcBuyQuantity = order.BuyOrder.OriginalQuantity * order.BuyOrder.Price;

            var percentage = Math.Round((((quantityBTC + btcBuyQuantity) / btcBuyQuantity) * 100) - 100, 2);
            var btcPrice = binanceClient.GetPrice("BTCUSDT").Data.Price;
            var quantityUSD = Math.Round(quantityBTC * btcPrice, 2);

            var tradeQUantityBTC = order.SellOrder.Price * order.SellOrder.OriginalQuantity;
            var tradeQUantityUSD = Math.Round(tradeQUantityBTC * btcPrice, 2);

            order.Status = Status.Completed;
            order.EndDate = DateTime.Now;
            order.AmountBTC = tradeQUantityBTC;
            order.AmountUSDT = tradeQUantityUSD;
            order.ProfitBTC = quantityBTC;
            order.ProfitUSD = quantityUSD;
            order.ProfitPercentage = percentage;
            Task.Run(() =>
            {
                try
                {
                    var orderCompleted = new OrderCompleted()
                    {
                        Amount = order.Amount,
                        AmountUSDT = order.AmountUSDT,
                        BuyPrice = order.BuyPrice.Value,
                        MinTickBuy = order.BBeta,
                        MinTickSell = order.SBeta,
                        Pair = order.Pair,
                        ProfitPercentage = order.ProfitPercentage.Value,
                        ProfitUSDT = order.ProfitUSD.Value,
                        SellPrice = order.SellPrice.Value,
                        TickUp = order.TickUp,
                        Username = Environment.UserName,
                        WallBuy = order.BAlfaUSDT,
                        WallSell = order.SAlfaUSDT
                    };
                }
                catch (Exception exception)
                {
                }
            });
        }

        private void SellPartiallyFilled(Order order, BinanceCanceledOrder orderStatus)
        {
            var buyOrder = CloneBinancePlaceOrder(order.BuyOrder);
            buyOrder.OriginalQuantity = orderStatus.ExecutedQuantity;
            buyOrder.ExecutedQuantity = orderStatus.ExecutedQuantity;

            var newOrder = new Order()
            {
                Amount = orderStatus.ExecutedQuantity,
                BAlfa = order.BAlfa,
                BBeta = order.BBeta,
                SAlfa = order.SBeta,
                SBeta = order.SBeta,
                TickUp = order.TickUp,
                Processing = false,
                Guid = Guid.NewGuid().ToString(),
                BuyOrder = buyOrder,
                BuyPrice = order.BuyPrice,
                BuyOrderCompletedDatetime = DateTime.Now,
                Pair = order.Pair,
                Status = Status.WaitSell,
                StartDate = DateTime.Now
            };
            Add(newOrder);
        }

        private void ManagePartiallyFilledBuy(Order order, BinanceCanceledOrder orderStatus)
        {
            BuyPartiallyFilled(order, orderStatus);
            SellPartiallyFilled(order, orderStatus);
        }

        private void BuyPartiallyFilled(Order order, BinanceCanceledOrder orderStatus)
        {
            var quantity = orderStatus.OriginalQuantity;
            var executedQuantity = orderStatus.ExecutedQuantity;

            var amountToPlace = quantity - executedQuantity;

            var newOrder = new Order()
            {
                Amount = amountToPlace,
                AmountUSDT = order.AmountUSDT,
                BAlfa = order.BAlfa,
                BAlfaUSDT = order.BAlfaUSDT,
                BBeta = order.BBeta,
                SAlfa = order.SAlfa,
                SAlfaUSDT = order.SAlfaUSDT,
                SBeta = order.SBeta,
                TickUp = order.TickUp,
                Processing = false,
                Guid = Guid.NewGuid().ToString(),
                Pair = order.Pair,
                Status = Status.WaitBuy,
                StartDate = DateTime.Now,

            };
            Add(newOrder);
        }

        private void ManagePartiallyFilledSell(Order order, BinanceCanceledOrder orderStatus)
        {
            var quantity = orderStatus.OriginalQuantity;
            var executedQuantity = orderStatus.ExecutedQuantity;

            var amountToPlace = quantity - executedQuantity;
            var buyOrder = CloneBinancePlaceOrder(order.BuyOrder);
            buyOrder.ExecutedQuantity = amountToPlace;
            buyOrder.OriginalQuantity = amountToPlace;

            var newOrder = new Order()
            {
                Amount = amountToPlace,
                AmountUSDT = order.AmountUSDT,
                BAlfa = order.BAlfa,
                BAlfaUSDT = order.BAlfaUSDT,
                BBeta = order.BBeta,
                SAlfa = order.SAlfa,
                SAlfaUSDT = order.SAlfaUSDT,
                SBeta = order.SBeta,
                Processing = false,
                TickUp = order.TickUp,
                Guid = Guid.NewGuid().ToString(),
                BuyOrder = buyOrder,
                BuyPrice = buyOrder.Price,
                BuyOrderCompletedDatetime = DateTime.Now,
                Pair = order.Pair,
                Status = Status.WaitSell,
                StartDate = order.StartDate
            };
            Add(newOrder);

            order.Amount = executedQuantity;
            order.BuyOrder.ExecutedQuantity = executedQuantity;
            order.BuyOrder.OriginalQuantity = executedQuantity;
            order.SellOrderCompletedDatetime = DateTime.Now;
            CompleteOrder(order);
        }

        private BinancePlacedOrder CloneBinancePlaceOrder(BinancePlacedOrder binancePlaced)
        {
            var clone = new BinancePlacedOrder()
            {
                ExecutedQuantity = binancePlaced.ExecutedQuantity,
                Price = binancePlaced.Price,
                Status = binancePlaced.Status,
                OriginalQuantity = binancePlaced.OriginalQuantity,
                TransactTime = binancePlaced.TransactTime,
                OrderId = binancePlaced.OrderId,
                ClientOrderId = binancePlaced.ClientOrderId,
                CummulativeQuoteQuantity = binancePlaced.CummulativeQuoteQuantity,
                Fills = binancePlaced.Fills,
                OrderListId = binancePlaced.OrderListId,
                OriginalClientOrderId = binancePlaced.OriginalClientOrderId,
                Side = binancePlaced.Side,
                StopPrice = binancePlaced.StopPrice,
                Symbol = binancePlaced.Symbol,
                TimeInForce = binancePlaced.TimeInForce,
                Type = binancePlaced.Type
            };
            return clone;
        }

        private BinanceOrderBook RemoveMyOrderFromOrderbook(decimal price, decimal quantity, OrderSide orderSide,
            BinanceOrderBook orderbook)
        {
            var orderbookSide = orderSide == OrderSide.Sell ? orderbook.Asks.ToList() : orderbook.Bids.ToList();

            var indexOfMyOrder = orderbookSide.FindIndex(x => x.Price == price);

            if (indexOfMyOrder == -1)
            {
                Console.WriteLine("My order not found in orderbook");
                return orderbook;
            }

            var amountDifference = orderbookSide[indexOfMyOrder].Quantity - quantity;

            if (amountDifference == 0)
            {
                orderbookSide.Splice(indexOfMyOrder, 1);
            }
            else if (amountDifference > 0)
            {
                orderbookSide[indexOfMyOrder].Quantity = amountDifference;
            }
            else
            {
                Console.WriteLine("amount less than my order????");
            }

            if (orderSide == OrderSide.Sell)
            {
                orderbook.Asks = orderbookSide;
            }
            else
            {
                orderbook.Bids = orderbookSide;
            }

            return orderbook;
        }

        public void EditOrder(string guid, decimal bAlfa, decimal bBeta, decimal sAlfa, decimal sBeta)
        {
            var order = Orders.SingleOrDefault(x => x.Guid == guid);

            if (order == null)
            {
                return;
            }

            order.BAlfa = bAlfa;
            order.BBeta = bBeta;
            order.SAlfa = sAlfa;
            order.SBeta = sBeta;
        }


        public void Add(Order order)
        {
            if (order == null) throw new ArgumentNullException(nameof(order));
            order.Processing = false;
            context.Send(x => Orders.Add(order), null);



        }

        public Order GetOrder(string guid)
        {
            return Orders.SingleOrDefault(x => x.Guid == guid);
        }

        public async Task DeleteOrder(string guid)
        {
            var order = Orders.SingleOrDefault(x => x.Guid == guid);

            if (order != null)
            {
                Orders.Remove(order);
                if (order.Status == Status.Buy)
                    await binanceClient.CancelOrderAsync(order.Pair, order.BuyOrder.OrderId);
                else if (order.Status == Status.Sell)
                    await binanceClient.CancelOrderAsync(order.Pair, order.SellOrder.OrderId);
            }
        }

        public void DeleteOrders()
        {
            Orders.Clear();
        }
    }
}