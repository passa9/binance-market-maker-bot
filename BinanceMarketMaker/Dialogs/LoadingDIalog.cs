using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace BinanceMarketMaker.WPF.Dialogs
{
    public class LoadingDialog : CustomDialog
    {
        public LoadingDialog()
        {
            this.Background = Brushes.Transparent;

            var progressRing = new ProgressRing()
            {
                Width = 100,
                Height = 100
            };
           
            this.AddChild(progressRing);
            this.OnClose();
        }
    }
}
