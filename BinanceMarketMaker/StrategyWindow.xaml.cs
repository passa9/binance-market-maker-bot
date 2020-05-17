using BinanceMarketMaker.BusinessLogic;
using BinanceMarketMaker.Models;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace BinanceMarketMaker.WPF
{
    /// <summary>
    /// Logica di interazione per Window1.xaml
    /// </summary>
    public partial class StrategyWindow : MetroWindow
    {
        private BinanceManager binanceManager;

        public Strategy Strategy { get; set; }
        public bool IsNewOrder { get; set; }
        private Order order { get; set; }
        public StrategyWindow(BinanceManager binanceManager, string pair)
        {
            InitializeComponent();
            this.DataContext = this;
            this.binanceManager = binanceManager;
            this.Strategy = new Strategy()
            {
                Pair = pair
            };
            IsNewOrder = true;
        }

        public StrategyWindow(BinanceManager binanceManager,Order order)
        {
            InitializeComponent();
            this.DataContext = this;
            this.binanceManager = binanceManager;
            this.Strategy = new Strategy()
            {
                Pair = order.Pair,
                MinGapBuy = order.BBeta,
                MinGapSell = order.SBeta,
                QuantityUSDT = order.AmountUSDT,
                TickUp = order.TickUp,
                WallBuyUSDT = order.BAlfaUSDT,
                WallSellUSDT = order.SAlfaUSDT
            };
            IsNewOrder = false;
            btnStart.Content = "Edit";
            this.order = order;
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
                this.DialogResult = false;
                this.Close();  
        }

        private async void btnStart_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                btnStart.IsEnabled = false;
                Cursor = Cursors.Wait;
                if (!Validate())
                    return;
                await ConvertUSDT();

                if (!IsNewOrder) {
                    this.order.BBeta = Strategy.MinGapBuy;
                    this.order.SBeta = Strategy.MinGapSell;
                    this.order.TickUp = Strategy.TickUp;
                    this.order.BAlfa = Strategy.WallBuy;
                    this.order.BAlfaUSDT = Strategy.WallBuyUSDT;
                    this.order.SAlfa = Strategy.WallSell;
                    this.order.SAlfaUSDT = Strategy.WallSellUSDT;
                }

                this.DialogResult = true;
            }
            finally {
                Cursor = Cursors.Arrow;
                btnStart.IsEnabled = true;
            }
        }

        private async Task ConvertUSDT()
        {
            Strategy.Quantity = await binanceManager.ConvertUSDTAsync(Strategy.QuantityUSDT, Strategy.Pair);
            Strategy.WallBuy = await binanceManager.ConvertUSDTAsync(Strategy.WallBuyUSDT, Strategy.Pair);
            Strategy.WallSell = await binanceManager.ConvertUSDTAsync(Strategy.WallSellUSDT, Strategy.Pair);
        }

        private bool Validate()
        {

            if (Strategy.QuantityUSDT <= 0)
            {
                MessageBox.Show("Invalid amount",
                                             "Error",
                                             MessageBoxButton.OK,
                                             MessageBoxImage.Question);
                return false;
            }

            if (Strategy.TickUp < 0)
            {
             MessageBox.Show("Invalid tick up/down",
                                             "Error",
                                             MessageBoxButton.OK,
                                             MessageBoxImage.Question);
                return false;
            }

            return true;


        }
    }
}
