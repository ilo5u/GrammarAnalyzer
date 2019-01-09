using GrammarAnalyzer.Models;
using Microsoft.Toolkit.Uwp.UI.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
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
    public sealed partial class LLAnalysisPage : Page
    {
        public List<List<string>> Rows = new List<List<string>>();
        private bool Collision = false;

        public LLAnalysisPage()
        {
            this.InitializeComponent();
        }

        private ObservableCollection<TokenViewer> Statement = new ObservableCollection<TokenViewer>();
        private ObservableCollection<TokenViewer> Tokens = new ObservableCollection<TokenViewer>();

        private Kernel.Analyzer LLAnalyzer = new Kernel.Analyzer();
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            Collision = false;
            CollisionInfo.Visibility = Visibility.Collapsed;

            IEnumerable<TokenViewer> tokens = (IEnumerable<TokenViewer>)e.Parameter;
            foreach (var item in tokens)
            {
                switch (item.Type)
                {
                    case TokenType.Terminal:
                        Tokens.Add(item);
                        LLAnalyzer.InsertTerminal(item.Token);
                        break;
                    case TokenType.Nonterminal:
                        Tokens.Add(item);
                        LLAnalyzer.InsertNonterminal(item.Token);
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
                LLAnalyzer.InsertProduction(production);
            }

            foreach (var item in tokens)
            {
                if (item.IsStart == true)
                {
                    LLAnalyzer.SetStartNonterminal(item.Token);
                }
            }

            WaitForSheet.Visibility = Visibility.Visible;
            WaitForProcedure.Visibility = Visibility.Collapsed;

            new Task(DrawAnalysisSheet).Start();
        }

        async private void DrawAnalysisSheet()
        {
            string totals = LLAnalyzer.GetLLAnalysisSheet();
            string[] rows = totals.Split('\n');
            foreach (var item in rows)
            {
                if (!string.IsNullOrEmpty(item))
                {
                    List<string> vs = new List<string>();
                    string[] columns = item.Split('\t');
                    foreach (var elem in columns)
                    {
                        if (!string.IsNullOrEmpty(elem))
                        {
                            if (elem.Contains('·'))
                            {
                                Collision = true;
                            }
                            vs.Add(elem);
                        }
                    }
                    Rows.Add(vs);
                }
            }

            await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                int columnIndex = 0;
                while (columnIndex < Rows[0].Count)
                {
                    AnalysisSheet.Columns.Add(new DataGridTextColumn
                    {
                        Header = Rows[0][columnIndex],
                        Binding = new Binding
                        {
                            Path = new PropertyPath($"[{columnIndex.ToString()}]")
                        }
                    });
                    columnIndex++;
                }

                Rows.Remove(Rows.ElementAt(0));
                AnalysisSheet.ItemsSource = Rows;
                AnalysisSheet.Visibility = Visibility.Visible;
                WaitForSheet.Visibility = Visibility.Collapsed;

                if (Collision == true)
                {
                    CollisionInfo.Visibility = Visibility.Visible;
                    ToAnalysis.IsEnabled = false;
                }
                else
                {
                    ToAnalysis.IsEnabled = true;
                }
            });
        }

        bool IsOnAnalysis = false;
        private void ToAnalysis_Click(object sender, RoutedEventArgs e)
        {
            if (!IsOnAnalysis)
            {
                IsOnAnalysis = true;

                string word = " ";
                foreach (var item in Statement)
                {
                    word += item.Token + " ";
                }

                new Task(() => StartAnalysis(word)).Start();
                AnalysisProcedure.Visibility = Visibility.Collapsed;
                WaitForProcedure.Visibility = Visibility.Visible;

                if (Collision == true
                    && sender is FrameworkElement collision)
                {
                    FlyoutBase.ShowAttachedFlyout(collision);
                }

                Statement.Clear();
            }
        }

        async private void StartAnalysis(string word)
        {
            Rows = new List<List<string>>();

            string totals = LLAnalyzer.LLAnalysis(word);
            string[] rows = totals.Split('\n');
            foreach (var item in rows)
            {
                if (!string.IsNullOrEmpty(item))
                {
                    Debug.WriteLine(item);
                    List<string> vs = new List<string>();
                    string[] columns = item.Split('\t');
                    foreach (var elem in columns)
                    {
                        if (!string.IsNullOrEmpty(elem))
                        {
                            vs.Add(elem);
                        }
                    }
                    Rows.Add(vs);
                }
            }

            await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                AnalysisProcedure.Columns.Clear();

                int columnIndex = 0;
                AnalysisProcedure.Columns.Add(new DataGridTextColumn
                {
                    Header = "Step",
                    Binding = new Binding
                    {
                        Path = new PropertyPath($"[{columnIndex.ToString()}]")
                    }
                });
                columnIndex++;

                AnalysisProcedure.Columns.Add(new DataGridTextColumn
                {
                    Header = "Stack",
                    Binding = new Binding
                    {
                        Path = new PropertyPath($"[{columnIndex.ToString()}]")
                    }
                });
                columnIndex++;

                AnalysisProcedure.Columns.Add(new DataGridTextColumn
                {
                    Header = "Input",
                    Binding = new Binding
                    {
                        Path = new PropertyPath($"[{columnIndex.ToString()}]")
                    }
                });
                columnIndex++;

                AnalysisProcedure.Columns.Add(new DataGridTextColumn
                {
                    Header = "Output",
                    Binding = new Binding
                    {
                        Path = new PropertyPath($"[{columnIndex.ToString()}]")
                    }
                });
                columnIndex++;
                Rows.Remove(Rows.ElementAt(0));

                AnalysisProcedure.ItemsSource = Rows;
                AnalysisProcedure.Visibility = Visibility.Visible;
                WaitForProcedure.Visibility = Visibility.Collapsed;
            });

            IsOnAnalysis = false;
        }

        private void Delete_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (sender is FrameworkElement delete)
            {
                FlyoutBase.ShowAttachedFlyout(delete);
            }
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (ToDeleteToken != null)
                Statement.Remove(ToDeleteToken);
            ToDeleteToken = null;
        }

        private TokenViewer ToDeleteToken = null;
        private void DeleteViewer_Click(object sender, ItemClickEventArgs e)
        {
            ToDeleteToken = (TokenViewer)e.ClickedItem;
        }

        private void TokenViewer_Click(object sender, ItemClickEventArgs e)
        {
            TokenViewer tokenViewer = (TokenViewer)e.ClickedItem;
            Statement.Add(new TokenViewer
            {
                Token = tokenViewer.Token,
                IsStart = tokenViewer.IsStart,
                Type = tokenViewer.Type
            });
        }
    }
}
