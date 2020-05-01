using System;
using System.ComponentModel;
using System.Data;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.ComTypes;
using Binance.Net.Objects;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace BinanceMarketMaker.Models
{
    public class Order : INotifyPropertyChanged
    {

        #region "privatefields"
        private decimal? bid;
        private decimal? ask;
        private decimal? profitPercentage;
        private decimal? profitBTC;
        private decimal? profitUSD;
        private decimal amount;
        private decimal? buyPrice;
        private decimal? sellPrice;
        private DateTime? buyOrderCompletedDatetime;
        private DateTime? sellOrderCompletedDatetime;
        private Status status;
        private DateTime? endDate;
        private string errorMessage;
        #endregion
        public bool Processing { get; set; }
        public string Guid { get; set; }
        public string Pair { get; set; }
      
        public decimal AmountBTC { get; set; }
        public decimal AmountUSDT { get; set; }
        public decimal Amount
        {
            get
            {
                return this.amount;
            }
            set
            {
                this.amount = value;
                NotifyPropertyChanged(nameof(Amount));
            }
        }
        public decimal? Bid
        {
            get
            {
                return this.bid;
            }
            set
            {
                this.bid = value;
                NotifyPropertyChanged(nameof(Bid));
            }
        }
        public decimal? Ask
        {
            get
            {
                return this.ask;
            }
            set
            {
                this.ask = value;
                NotifyPropertyChanged(nameof(Ask));
            }
        }
        public decimal? BuyPrice
        {
            get
            {
                return this.buyPrice;
            }
            set
            {
                this.buyPrice = value;
                NotifyPropertyChanged(nameof(BuyPrice));
            }
        }
        public DateTime? BuyOrderCompletedDatetime
        {
            get
            {
                return this.buyOrderCompletedDatetime;
            }
            set
            {
                this.buyOrderCompletedDatetime = value;
                NotifyPropertyChanged(nameof(BuyOrderCompletedDatetime));
            }
        }
        public decimal? SellPrice
        {
            get
            {
                return this.sellPrice;
            }
            set
            {
                this.sellPrice = value;
                NotifyPropertyChanged(nameof(SellPrice));
            }
        }
        public DateTime? SellOrderCompletedDatetime
        {
            get
            {
                return this.sellOrderCompletedDatetime;
            }
            set
            {
                this.sellOrderCompletedDatetime = value;
                NotifyPropertyChanged(nameof(SellOrderCompletedDatetime));
            }
        }
        public BinancePlacedOrder BuyOrder { get; set; }
        public BinancePlacedOrder SellOrder { get; set; }
        public decimal BAlfaUSDT { get; set; }
        public decimal BAlfa { get; set; }
        public decimal BBeta { get; set; }
        public decimal SAlfaUSDT { get; set; }
        public decimal SAlfa { get; set; }
        public decimal SBeta { get; set; }
        public int TickUp { get; set; }
        public Status Status
        {
            get
            {
                return this.status;
            }
            set
            {
                this.status = value;
                NotifyPropertyChanged(nameof(Status));
            }
        }
        public string ErrorMessage
        {
            get
            {
                return this.errorMessage;
            }
            set
            {
                this.errorMessage = value;
                NotifyPropertyChanged(nameof(ErrorMessage));
            }
        }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate
        {
            get
            {
                return this.endDate;
            }
            set
            {
                this.endDate = value;
                NotifyPropertyChanged(nameof(EndDate));
            }
        }

        public decimal? ProfitPercentage
        {
            get
            {
                return this.profitPercentage;
            }
            set
            {
                this.profitPercentage = value;
                NotifyPropertyChanged(nameof(ProfitPercentage));
            }
        }
        public decimal? ProfitBTC
        {
            get
            {
                return this.profitBTC;
            }
            set
            {
                this.profitBTC = value;
                NotifyPropertyChanged(nameof(ProfitBTC));
            }
        }
        public decimal? ProfitUSD
        {
            get
            {
                return this.profitUSD;
            }
            set
            {
                this.profitUSD = value;
                NotifyPropertyChanged(nameof(ProfitUSD));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}