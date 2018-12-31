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

        private ObservableCollection<DeductionViewer> SLRDeductions = new ObservableCollection<DeductionViewer>();
        private Kernel.Analyzer SLRAnalyzer = new Kernel.Analyzer();
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            IEnumerable<TokenViewer> tokens = (IEnumerable<TokenViewer>)e.Parameter;
            foreach (var item in tokens)
            {
                switch (item.Type)
                {
                    case TokenType.Terminal:
                        SLRAnalyzer.InsertTerminal(item.Token);
                        break;
                    case TokenType.Nonterminal:
                        SLRAnalyzer.InsertNonterminal(item.Token);
                        break;
                    default:
                        break;
                }
            }

            foreach (var item in ProductionPage.Current.Productions)
            {
                string production = item.Nonterminal.Token + "#";
                foreach (var token in item.Candidates)
                {
                    switch (token.Type)
                    {
                        case TokenType.Epsilon:
                            production += "@";
                            break;
                        case TokenType.Terminal:
                            production += token.Token;
                            break;
                        case TokenType.Nonterminal:
                            production += token.Token;
                            break;
                        default:
                            break;
                    }
                    production += '.';
                }
                production.Remove(production.LastIndexOf('.'));
                SLRAnalyzer.InsertProduction(production);
            }

            foreach (var item in tokens)
            {
                if (item.IsStart == true)
                {
                    SLRAnalyzer.SetStartNonterminal(item.Token);
                }
            }

            WaitForSLRDeductions.Visibility = Visibility.Visible;
            ToSheet.Visibility = Visibility.Collapsed;

            new Task(BuildLRAnalysisSheet).Start();
        }

        async private void BuildLRAnalysisSheet()
        {
            string slrDeductions = SLRAnalyzer.GetSLRDeductions();
            await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                WaitForSLRDeductions.Visibility = Visibility.Collapsed;
                ToSheet.Visibility = Visibility.Visible;

                string[] anys = slrDeductions.Split('\t');
                int idCounter = 0;
                foreach (var item in anys)
                {
                    if (!string.IsNullOrEmpty(item))
                    {
                        SLRDeductions.Add(new DeductionViewer
                        {
                            Id = idCounter,
                            Description = item
                        });
                        idCounter++;
                    }
                }
            });

        }

        private void ToSheet_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(LRSheetPage), SLRAnalyzer);
        }
    }
}
