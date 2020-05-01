using BinanceMarketMaker.BusinessLogic;
using BinanceMarketMaker.Models;
using BinanceMarketMaker.WPF;
using BinanceMarketMaker.WPF.Dialogs;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace BinanceMarketMaker
{
    /// <summary>
    /// Logica di interazione per MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow, INotifyPropertyChanged
    {
        private const string QuoteAsset = "BTC";

        private Binance.Net.BinanceClient binanceClient;
        private BinanceManager binanceManager;
        private BinanceBot binanceBot;

        public event PropertyChangedEventHandler PropertyChanged;

        public ObservableCollection<Order> Orders
        {
            get
            {
                return binanceBot.Orders;
            }
            set
            {
                binanceBot.Orders = value;
                NotifyPropertyChanged(nameof(Orders));
            }
        }
        private ObservableCollection<Ticker> tickers;
        public ObservableCollection<Ticker> Tickers
        {
            get
            {
                return this.tickers;
            }
            set
            {
                this.tickers = value;
                NotifyPropertyChanged(nameof(Tickers));
            }
        }
        public string ApiKey { get; set; }
        public string SecretKey { get; set; }
        public decimal Fees { get; set; }

        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public MainWindow()
        {
            InitializeComponent();

            this.DataContext = this;

            binanceClient = new Binance.Net.BinanceClient();
            var uiContext = SynchronizationContext.Current;
            binanceBot = new BinanceBot(binanceClient, uiContext);
            binanceManager = new BinanceManager(binanceClient);
            binanceManager.Init();

            binanceBot.SetSettings("WymHu98iDz4GNJPg7FBJgEqukV4RljRubWKm0ZBT4Rv7wOOLZT3eceVsbRATSe3g", "Tf4TFKTTeISO3bUFmJhgZDQXX8F6tPynSS7k2SfGXE4a13RdYAtYI810EjpWYF7n", 1000, 0.06m);
            binanceBot.Start();
        }


        private async Task LoadTickersAsync()
        {
            var tickers = await binanceManager.GetTickersAsync(QuoteAsset);
            Tickers = new ObservableCollection<Ticker>(tickers);

        }
        private async Task DoTaskAsync(Task task)
        {
            var loadingDialog = new LoadingDialog();
            await this.ShowMetroDialogAsync(loadingDialog);
            await task;
            await this.HideMetroDialogAsync(loadingDialog);
        }
        private async void btnRefreshTickers_Click(object sender, RoutedEventArgs e)
        {
            btnRefreshTickers.IsEnabled = false;
            await DoTaskAsync(LoadTickersAsync());

            await Task.Run(async () =>
            {
                int i = 30;
                while (i > 0)
                {
                    await Dispatcher.BeginInvoke((Action)(() =>
                    {
                        btnRefreshTickers.Content = $"Wait {i} seconds...";
                    }));
                    await Task.Delay(1000);
                    i--;
                }
                await Dispatcher.BeginInvoke((Action)(() =>
                {
                    btnRefreshTickers.Content = "REFRESH";
                    btnRefreshTickers.IsEnabled = true;
                }));

            });
        }

        private async void MetroWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await DoTaskAsync(LoadTickersAsync());
        }

        private async void btnCreateStrategy_Click(object sender, RoutedEventArgs e)
        {
            Ticker ticker = ((FrameworkElement)sender).DataContext as Ticker;
            var strategyWindow = new StrategyWindow(binanceManager, ticker.Pair);

            if (strategyWindow.ShowDialog().Value)
            {
                Strategy strategy = strategyWindow.Strategy;
                var order = binanceManager.CreateOrder(strategy);

                binanceBot.Add(order);
            }
        }

        private async void btnEditOrder_Click(object sender, RoutedEventArgs e)
        {
            Order order = ((FrameworkElement)sender).DataContext as Order;
       
            var strategyWindow = new StrategyWindow(binanceManager, order);
        
            strategyWindow.ShowDialog();
        }

        private async void btnDeleteOrder_Click(object sender, RoutedEventArgs e)
        {
            Order order = ((FrameworkElement)sender).DataContext as Order;

           await binanceBot.DeleteOrder(order.Guid);
        }

    }
}
