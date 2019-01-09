using GrammarAnalyzer.Models;
using Microsoft.Toolkit.Uwp.UI.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
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
    public sealed partial class LRSheetPage : Page
    {
        public List<List<string>> Rows = new List<List<string>>();
        private bool Collision = false;

        public LRSheetPage()
        {
            this.InitializeComponent();
        }

        private ObservableCollection<TokenViewer> Statement = new ObservableCollection<TokenViewer>();
        private ObservableCollection<TokenViewer> Tokens = new ObservableCollection<TokenViewer>();

        private Kernel.Analyzer LRAnalyzer = null;

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            Collision = false;
            CollisionInfo.Visibility = Visibility.Collapsed;

            LRAnalyzer = (Kernel.Analyzer)e.Parameter;
            AnalysisSheet.Visibility = Visibility.Collapsed;
            WaitForSheet.Visibility = Visibility.Visible;
            WaitForProcedure.Visibility = Visibility.Collapsed;

            foreach (var item in ProductionPage.Current.Tokens)
            {
                if (item.Type != TokenType.Epsilon)
                {
                    Tokens.Add(item);
                }
            }

            new Task(() => DrawAnalysisSheet(LRAnalyzer)).Start();
        }

        async private void DrawAnalysisSheet(Kernel.Analyzer analyzer)
        {
            string totals = analyzer.GetLRAnalysisSheet();
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
                 AnalysisSheet.Columns.Add(new DataGridTextColumn
                 {
                     Header = "State",
                     Binding = new Binding
                     {
                         Path = new PropertyPath($"[{columnIndex.ToString()}]")
                     }
                 });
                 columnIndex++;

                 while (columnIndex <= TokenPage.Current.Terminals.Count)
                 {
                     AnalysisSheet.Columns.Add(new DataGridTextColumn
                     {
                         Header = "Action",
                         Binding = new Binding
                         {
                             Path = new PropertyPath($"[{columnIndex.ToString()}]")
                         }
                     });
                     columnIndex++;
                 }

                 while (columnIndex < Rows[0].Count)
                 {
                     AnalysisSheet.Columns.Add(new DataGridTextColumn
                     {
                         Header = "Goto",
                         Binding = new Binding
                         {
                             Path = new PropertyPath($"[{columnIndex.ToString()}]")
                         }
                     });
                     columnIndex++;
                 }
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
                Debug.WriteLine("Analyze: " + word);

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

            string totals = LRAnalyzer.LRAnalysis(word);
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
                    Header = "State",
                    Binding = new Binding
                    {
                        Path = new PropertyPath($"[{columnIndex.ToString()}]")
                    }
                });
                columnIndex++;

                AnalysisProcedure.Columns.Add(new DataGridTextColumn
                {
                    Header = "Token",
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
