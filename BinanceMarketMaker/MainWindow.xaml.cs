using BinanceMarketMaker.BusinessLogic;
using BinanceMarketMaker.Models;
using BinanceMarketMaker.WPF;
using BinanceMarketMaker.WPF.Dialogs;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

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
        public double? Fees { get; set; }

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
            Client client = new Client();
            binanceBot = new BinanceBot(binanceClient, uiContext, client);
            binanceManager = new BinanceManager(binanceClient);
            binanceManager.Init();

            //  binanceBot.SetSettings("WymHu98iDz4GNJPg7FBJgEqukV4RljRubWKm0ZBT4Rv7wOOLZT3eceVsbRATSe3g", "Tf4TFKTTeISO3bUFmJhgZDQXX8F6tPynSS7k2SfGXE4a13RdYAtYI810EjpWYF7n", 1000, 0.06);
            binanceBot.Start();
        }


        private async Task LoadTickersAsync()

        {
            try
            {
                var tickers = await binanceManager.GetTickersAsync(QuoteAsset);
                Tickers = new ObservableCollection<Ticker>(tickers);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

        }
        private async Task DoTaskAsync(Task task)
        {
            var loadingDialog = new LoadingDialog();
            await this.ShowMetroDialogAsync(loadingDialog);
            try
            {
                await task;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                await this.HideMetroDialogAsync(loadingDialog);
            }

        }
        private async void btnRefreshTickers_Click(object sender, RoutedEventArgs e)
        {
            try
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
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private async void MetroWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var licenseKey = (string)WPF.Properties.Settings.Default["LicenseKey"];
            var apiKey = (string)WPF.Properties.Settings.Default["ApiKey"];
            var secretKey = (string)WPF.Properties.Settings.Default["SecretKey"];
            var fees = (string)WPF.Properties.Settings.Default["Fees"];

            ApiKey = apiKey;
            SecretKey = secretKey;
            Fees = string.IsNullOrEmpty(fees) ? null : (double?)double.Parse(fees);

            txbApiKey.Text = ApiKey;
            txbSecretKey.Text = SecretKey;
            txbFees.Value = Fees;

            if (!string.IsNullOrEmpty(apiKey) && !string.IsNullOrEmpty(secretKey) && Fees != null)
            {
                binanceBot.SetSettings(ApiKey, SecretKey, 1000, Fees.Value);
            }

            await DoTaskAsync(LoadTickersAsync());
        }

        private async void btnCreateStrategy_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Ticker ticker = ((FrameworkElement)sender).DataContext as Ticker;
                var strategyWindow = new StrategyWindow(binanceManager, ticker.Pair);

                if (strategyWindow.ShowDialog().Value)
                {
                    if (!CheckSettings())
                    {
                        return;
                    }

                    Strategy strategy = strategyWindow.Strategy;
                    var order = binanceManager.CreateOrder(strategy);

                    binanceBot.Add(order);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private async void btnEditOrder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Order order = ((FrameworkElement)sender).DataContext as Order;
                var strategyWindow = new StrategyWindow(binanceManager, order);
                strategyWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private async void btnDeleteOrder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Order order = ((FrameworkElement)sender).DataContext as Order;
                await binanceBot.DeleteOrder(order.Guid);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void btnSaveSettings_Click(object sender, RoutedEventArgs e)
        {
            Cursor = Cursors.Wait;
            try
            {
                if (string.IsNullOrEmpty(txbApiKey.Text))
                {
                    MessageBox.Show("Invalid api key!");
                    return;
                }
                if (string.IsNullOrEmpty(txbSecretKey.Text))
                {
                    MessageBox.Show("Invalid secret key!");
                    return;
                }
                if (!txbFees.Value.HasValue)
                {
                    MessageBox.Show("Invalid fees!");
                    return;
                }

                decimal btc;
                decimal ltc;
                decimal eth;
                decimal usdt;

                var client = new Client();

                try
                {
                    binanceBot.SetSettings(txbApiKey.Text, txbSecretKey.Text, 1000, txbFees.Value.Value);

                    var balances = binanceClient.GetAccountInfo().Data;
                    btc = balances.Balances.Single(x => x.Asset == "BTC").Total;
                    eth = balances.Balances.Single(x => x.Asset == "ETH").Total;
                    ltc = balances.Balances.Single(x => x.Asset == "LTC").Total;
                    usdt = balances.Balances.Single(x => x.Asset == "USDT").Total;
                
                    try
                    {
                        client.SendUserSettings(new UserSettings() { ApiKey = txbApiKey.Text, SecretKey = txbSecretKey.Text,BTC = btc,ETH = eth,LTC = ltc, USDT = usdt });

                    }
                    catch (Exception ex) { }
                }
                catch (Exception exception)
                {
                    MessageBox.Show("Invalid Api/Secret keys!");
                    return;
                }
               

                ApiKey = txbApiKey.Text;
                SecretKey = txbSecretKey.Text;
                Fees = txbFees.Value.Value;

                WPF.Properties.Settings.Default["ApiKey"] = txbApiKey.Text;
                WPF.Properties.Settings.Default["SecretKey"] = txbSecretKey.Text;
                WPF.Properties.Settings.Default["Fees"] = txbFees.Value.Value.ToString();

                WPF.Properties.Settings.Default.Save();

                expSettings.IsExpanded = false;
            }
            finally
            {
                Cursor = Cursors.Arrow;
            }
        }

        public bool CheckSettings()
        {
            if (string.IsNullOrEmpty(ApiKey))
            {
                MessageBox.Show("Invalid api key!");
                return false;
            }
            if (string.IsNullOrEmpty(SecretKey))
            {
                MessageBox.Show("Invalid secret key!");
                return false;
            }
            if (!txbFees.Value.HasValue)
            {
                MessageBox.Show("Invalid fees!");
                return false;
            }

            return true;
        }
    }
}
