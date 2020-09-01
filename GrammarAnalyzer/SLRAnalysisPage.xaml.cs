using GrammarAnalyzer.Kernel;
using GrammarAnalyzer.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板

namespace GrammarAnalyzer
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class SLRAnalysisPage : Page
    {
        public SLRAnalysisPage()
        {
            this.InitializeComponent();
        }

        private readonly ObservableCollection<DerivViewer> SLRDerivs = new ObservableCollection<DerivViewer>();
        private SLR SLRAnalyzer = null;
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            SLRAnalyzer = new SLR((Grammar)e.Parameter);

            WaitForSLRDerivs.Visibility = Visibility.Visible;
            ToSheet.Visibility = Visibility.Collapsed;

            new Task(BuildLRAnalysisSheet).Start();
        }

        async private void BuildLRAnalysisSheet()
        {
            if (!(SLRAnalyzer.RunFIS() is null) && !(SLRAnalyzer.RunFOS() is null))
            {
                var derivs = SLRAnalyzer.BuildDerivs();
                await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    WaitForSLRDerivs.Visibility = Visibility.Collapsed;
                    ToSheet.Visibility = Visibility.Visible;

                    derivs.ToList().ForEach(d =>
                    {
                        string desc = "";
                        d.Value.ForEach(s => desc += s + "\n");
                        SLRDerivs.Add(new DerivViewer
                        {
                            Id = d.Key,
                            Description = desc
                        });
                    });
                });
            }
        }

        private void ToSheet_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(LRSheetPage), SLRAnalyzer);
        }
    }
}
