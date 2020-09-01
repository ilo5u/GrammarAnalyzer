using GrammarAnalyzer.Kernel;
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

        private readonly ObservableCollection<TokenViewer> Statement = new ObservableCollection<TokenViewer>();
        private ObservableCollection<TokenViewer> Tokens = new ObservableCollection<TokenViewer>();

        private LRBaseGrammar LRBaseAnalyzer = null;

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            Collision = false;
            CollisionInfo.Visibility = Visibility.Collapsed;

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

            Tokens = new ObservableCollection<TokenViewer>();
            foreach (var item in ProductionPage.Current.Tokens)
            {
                if (item.Type == TokenType.Terminal) Tokens.Add(item);
            }

            LRBaseAnalyzer = (LRBaseGrammar)e.Parameter;
            new Task(() => DrawAnalysisSheet()).Start();
        }

        async private void DrawAnalysisSheet()
        {

            var sheet = LRBaseAnalyzer.BuildAnalysisSheet();

            List<string> header = new List<string> { "" };
            sheet.Item2.ToList().ForEach(t =>
            {
                header.Add(t.Value._attr);
            });
            Rows = new List<List<string>> { header };
            for (int state = 0; state < sheet.Item1; ++state)
            {
                List<string> r = new List<string> { state.ToString() };
                for (int col = 0; col < sheet.Item2.Count; ++col)
                {
                    if (sheet.Item3.TryGetValue((state, col), out List<LRBaseGrammar.Action> acs))
                    {
                        if (acs.Count > 1) Collision = true;
                        // use the first as default
                        LRBaseGrammar.Action action = acs.First();
                        switch (action._type)
                        {
                            case LRBaseGrammar.Action.Type.SHIFT:
                                r.Add("Shift " + action._nextState.ToString());
                                break;
                            case LRBaseGrammar.Action.Type.REDUC:
                                {
                                    string desc = action._prodc._left._attr + "→";
                                    action._prodc._right.ForEach(t => desc += t._attr);
                                    r.Add(desc);
                                }
                                break;
                            case LRBaseGrammar.Action.Type.ACC:
                                r.Add("ACC");
                                break;
                            default:
                                break;
                        }
                    }
                    else
                    {
                        r.Add("");
                    }
                }
                Rows.Add(r);
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

            var res = LRBaseAnalyzer.Analyze(words);
            res.ForEach(s =>
            {
                // Step
                List<string> r = new List<string> { s.Item1.ToString() };
                // State
                string unit = "";
                s.Item2.ForEach(e => unit += e.ToString() + " ");
                unit.Remove(unit.Length - 1);
                r.Add(unit);
                // Token Stack
                unit = "";
                s.Item3.ForEach(e => unit += e._attr + " ");
                unit.Remove(unit.Length - 1);
                r.Add(unit);
                // Input
                unit = "";
                s.Item4.ForEach(e => unit += e._attr + " ");
                unit.Remove(unit.Length - 1);
                r.Add(unit);
                // Output
                if (s.Item5)
                {
                    switch (s.Item6._type)
                    {
                        case LRBaseGrammar.Action.Type.SHIFT:
                            r.Add("Shift " + s.Item6._nextState.ToString());
                            break;
                        case LRBaseGrammar.Action.Type.REDUC:
                            if (s.Item6._prodc._right is null)
                            {
                                r.Add("");
                            }
                            else
                            {
                                unit = "Reduced by " + s.Item6._prodc._left._attr + "→";
                                s.Item6._prodc._right.ForEach(t => unit += t._attr);
                                r.Add(unit);
                            }
                            break;
                        case LRBaseGrammar.Action.Type.ACC:
                            r.Add("ACC");
                            break;
                        default:
                            r.Add("");
                            break;
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
