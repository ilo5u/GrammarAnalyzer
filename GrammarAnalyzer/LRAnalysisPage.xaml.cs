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
    public sealed partial class LRAnalysisPage : Page
    {
        public LRAnalysisPage()
        {
            this.InitializeComponent();
        }

        private readonly ObservableCollection<DerivViewer> LRDerivs = new ObservableCollection<DerivViewer>();
        private LR LRAnalyzer = null;
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            LRAnalyzer = new LR((Grammar)e.Parameter);

            WaitForLRDerivs.Visibility = Visibility.Visible;
            ToSheet.Visibility = Visibility.Collapsed;

            new Task(BuildLRAnalysisSheet).Start();
        }

        async private void BuildLRAnalysisSheet()
        {
            if (!(LRAnalyzer.RunFIS() is null) && !(LRAnalyzer.RunFOS() is null))
            {
                var derivs = LRAnalyzer.BuildDerivs();
                await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    WaitForLRDerivs.Visibility = Visibility.Collapsed;
                    ToSheet.Visibility = Visibility.Visible;

                    derivs.ToList().ForEach(d =>
                    {
                        string desc = "";
                        d.Value.ForEach(s => desc += s.Item1 + " " + s.Item2 + "\n");
                        LRDerivs.Add(new DerivViewer
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
            this.Frame.Navigate(typeof(LRSheetPage), LRAnalyzer);
        }
    }
}
