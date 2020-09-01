using GrammarAnalyzer.Kernel;
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

        private readonly ObservableCollection<TokenViewer> Statement = new ObservableCollection<TokenViewer>();
        private ObservableCollection<TokenViewer> Tokens = new ObservableCollection<TokenViewer>();

        private LL LLAnalyzer = null;
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            Collision = false;
            CollisionInfo.Visibility = Visibility.Collapsed;

            LLAnalyzer = new LL((Grammar)e.Parameter);
            Tokens = new ObservableCollection<TokenViewer>();
            foreach (var item in ProductionPage.Current.Tokens)
            {
                if (item.Type == TokenType.Terminal) Tokens.Add(item);
            }

            WaitForSheet.Visibility = Visibility.Visible;
            WaitForProcedure.Visibility = Visibility.Collapsed;

            new Task(DrawAnalysisSheet).Start();
        }

        async private void DrawAnalysisSheet()
        {
            if (!(LLAnalyzer.RunFIS() is null) && !(LLAnalyzer.RunFOS() is null))
            {
                var lls = LLAnalyzer.BuildAnalysisSheet();
                // set header
                List<string> r = new List<string>() { "" };
                lls.Item2.ToList().ForEach(t => r.Add(t.Value._attr));
                Rows.Add(r);
                // set data
                lls.Item1.ToList().ForEach(t =>
                {
                    r = new List<string>() { t.Value._attr };
                    for (int col = 0; col < lls.Item2.Count; ++col)
                    {
                        if (lls.Item3.TryGetValue((t.Key, col), out HashSet<Grammar.Prodc> p))
                        {
                            Collision = p.Count != 1;
                            // use the first as default
                            Grammar.Prodc cur = p.First();
                            string str = cur._left._attr + "→";
                            cur._right.ForEach(e => str += e._attr);
                            r.Add(str);
                        }
                        else
                        {
                            r.Add("");
                        }
                    }
                    Rows.Add(r);
                });
                // show the sheet
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
        }

        bool IsOnAnalysis = false;
        private void ToAnalysis_Click(object sender, RoutedEventArgs e)
        {
            if (!IsOnAnalysis)
            {
                IsOnAnalysis = true;

                List<Grammar.Token> words = new List<Grammar.Token>();
                foreach (var item in Statement)
                {
                    words.Add(new Grammar.Token(
                        item.Type == TokenType.Nonterminal ? Grammar.Token.Type.NONTERMINAL : Grammar.Token.Type.TERMINAL,
                        item.Token
                        ));
                }

                new Task(() => StartAnalysis(words)).Start();
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

        async private void StartAnalysis(List<Grammar.Token> words)
        {
            Rows = new List<List<string>>();

            var steps = LLAnalyzer.Analyze(words);
            // set data
            steps.ForEach(s =>
            {
                List<string> r = new List<string>() { s.Item1.ToString() };
                string str = "";
                s.Item2.ForEach(t => str += t._attr);
                r.Add(str);

                str = "";
                s.Item3.ForEach(t => str += t._attr);
                r.Add(str);

                if (s.Item4)
                {
                    if (s.Item5._right is null)
                    {
                        r.Add("");
                    }
                    else
                    {
                        string ps = s.Item5._left._attr + "→";
                        s.Item5._right.ForEach(p => ps += p._attr);
                        r.Add(ps);
                    }
                }
                else
                {
                    r.Add("Failed");
                }
                Rows.Add(r);
            });

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
