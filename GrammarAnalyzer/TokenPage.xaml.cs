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
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI.Popups;
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
    public sealed partial class TokenPage : Page
    {
        public TokenPage()
        {
            this.InitializeComponent();
            Current = this;
        }
        public static TokenPage Current;

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            MainPage.Current.SelectHome();
            MainPage.Current.HideBackButton();
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            MainPage.Current.ShowBackButton();
        }

        public ObservableCollection<ProductionViewer> ProductionsLoadByLocal = null;
        public ObservableCollection<TokenViewer> Terminals = new ObservableCollection<TokenViewer>();
        public ObservableCollection<TokenViewer> Nonterminals = new ObservableCollection<TokenViewer>();
        public TokenViewer StartNonterminal = null;

        private void NonterminalInputer_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            TextBox nonterminal = (TextBox)sender;
            if (e.Key == Windows.System.VirtualKey.Enter
                && !string.IsNullOrEmpty(nonterminal.Text))
            {
                if (nonterminal.Text.Contains("@")
                    || nonterminal.Text.Contains("#"))
                {
                    nonterminal.Text = "";
                    if (sender is FrameworkElement illegalInput)
                    {
                        FlyoutBase.ShowAttachedFlyout(illegalInput);
                    }
                }
                else
                {
                    try
                    {
                        Terminals.First(elem => elem.Token == nonterminal.Text);
                        if (sender is FrameworkElement illegalInput)
                        {
                            FlyoutBase.ShowAttachedFlyout(illegalInput);
                        }
                    }
                    catch (Exception)
                    {
                        try
                        {
                            Nonterminals.First(elem => elem.Token == nonterminal.Text);
                        }
                        catch (Exception)
                        {
                            Nonterminals.Add(new TokenViewer
                            {
                                Token = nonterminal.Text,
                                IsStart = false,
                                Type = TokenType.Nonterminal
                            });
                        }
                    }
                    finally
                    {
                        nonterminal.Text = "";
                    }
                }
            }
            else if (nonterminal.Text.Length == 4)
            {
                e.Handled = true;
            }
            else if (e.Key == Windows.System.VirtualKey.Space)
            {
                e.Handled = true;
            }
            else if (e.Key == Windows.System.VirtualKey.Tab)
            {
                e.Handled = true;
            }
        }

        private void TerminalInputer_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            TextBox terminal = (TextBox)sender;
            if (e.Key == Windows.System.VirtualKey.Enter
                && !string.IsNullOrEmpty(terminal.Text))
            {
                if (terminal.Text.Contains("@")
                    || terminal.Text.Contains("#"))
                {
                    terminal.Text = "";
                    if (sender is FrameworkElement illegalInput)
                    {
                        FlyoutBase.ShowAttachedFlyout(illegalInput);
                    }
                }
                else
                {
                    try
                    {
                        Nonterminals.First(elem => elem.Token == terminal.Text);
                        if (sender is FrameworkElement illegalInput)
                        {
                            FlyoutBase.ShowAttachedFlyout(illegalInput);
                        }
                    }
                    catch (Exception)
                    {
                        try
                        {
                            Terminals.First(elem => elem.Token == terminal.Text);
                        }
                        catch (Exception)
                        {
                            Terminals.Add(new TokenViewer
                            {
                                Token = terminal.Text,
                                IsStart = false,
                                Type = TokenType.Terminal
                            });
                        }
                    }
                    finally
                    {
                        terminal.Text = "";
                    }
                }
            }
            else if (terminal.Text.Length == 4)
            {
                e.Handled = true;
            }
            else if (e.Key == Windows.System.VirtualKey.Space)
            {
                e.Handled = true;
            }
            else if (e.Key == Windows.System.VirtualKey.Tab)
            {
                e.Handled = true;
            }
        }

        private void DeleteNonterminal_Click(object sender, RoutedEventArgs e)
        {
            string nonterminal = (string)(((MenuFlyoutItem)sender).DataContext);
            Nonterminals.Remove(Nonterminals.First(elem => elem.Token == nonterminal));
        }

        private void SetStart_Click(object sender, RoutedEventArgs e)
        {
            string nonterminal = (string)(((MenuFlyoutItem)sender).DataContext);
            if (StartNonterminal != null)
            {
                Nonterminals.Remove(Nonterminals.First(elem => elem.Token == StartNonterminal.Token));
                Nonterminals.Add(new TokenViewer
                {
                    Token = StartNonterminal.Token,
                    IsStart = false,
                    Type = TokenType.Nonterminal
                });
            }
            Nonterminals.Remove(Nonterminals.First(elem => elem.Token == nonterminal));
            StartNonterminal = new TokenViewer
            {
                Token = nonterminal,
                IsStart = true,
                Type = TokenType.Nonterminal
            };
            Nonterminals.Add(StartNonterminal);
        }

        private void DeleteTerminal_Click(object sender, RoutedEventArgs e)
        {
            string terminal = (string)(((MenuFlyoutItem)sender).DataContext);
            Terminals.Remove(Terminals.First(elem => elem.Token == terminal));
        }

        private void ToProduction_Click(object sender, RoutedEventArgs e)
        {
            if (StartNonterminal == null
                || Nonterminals.Count == 0
                || Terminals.Count == 0)
            {
                if (sender is FrameworkElement noStartNonterminal)
                {
                    FlyoutBase.ShowAttachedFlyout(noStartNonterminal);
                }
            }
            else
            {
                Terminals.Add(new TokenViewer
                {
                    Token = "ε",
                    IsStart = false,
                    Type = TokenType.Epsilon
                });
                this.Frame.Navigate(typeof(ProductionPage), Nonterminals.Concat(Terminals));
            }
        }

        async private void ToLoad_Click(object sender, RoutedEventArgs e)
        {
            FileOpenPicker openPicker = new FileOpenPicker
            {
                ViewMode = PickerViewMode.Thumbnail,
                SuggestedStartLocation = PickerLocationId.PicturesLibrary
            };
            openPicker.FileTypeFilter.Add(".gra");

            StorageFile file = await openPicker.PickSingleFileAsync();
            if (file != null)
            {
                // Application now has read/write access to the picked file
                IBuffer localBuffer
                    = await FileIO.ReadBufferAsync(file);
                using (DataReader data = DataReader.FromBuffer(localBuffer))
                {
                    string records =
                        data.ReadString(localBuffer.Length);
                    new Task(() => LoadGrammar(records)).Start();
                }
            }
        }

        async private void LoadGrammar(string records)
        {
            if (!string.IsNullOrEmpty(records))
            {
                List<string> lines = records.Split('\n').ToList();
                foreach (var item in lines)
                {
                    if (string.IsNullOrEmpty(item))
                    {
                        lines.Remove(item);
                    }
                }

                if (lines.Count < 2)
                {
                    return;
                }

                List<string> nonterminals = lines[0].Split(' ').ToList();
                for (int index = 0; index < nonterminals.Count; ++index)
                {
                    if (string.IsNullOrWhiteSpace(nonterminals[index]))
                    {
                        nonterminals.Remove(nonterminals[index]);
                    }
                    else
                    {
                        nonterminals[index].Replace(" ", "");
                    }
                }

                List<string> terminals = lines[1].Split(' ').ToList();
                for (int index = 0; index < terminals.Count; ++index)
                {
                    if (string.IsNullOrWhiteSpace(terminals[index]))
                    {
                        terminals.Remove(terminals[index]);
                    }
                    else
                    {
                        terminals[index].Replace(" ", "");
                    }
                }

                List<string> productions = new List<string>();
                int lineIndex = 2;
                while (lineIndex < lines.Count)
                {
                    if (!string.IsNullOrWhiteSpace(lines[lineIndex]))
                    {
                        lines[lineIndex].Replace(" ", "");
                        if (lines[lineIndex].StartsWith('#'))
                        {
                            lineIndex++;
                            break;
                        }
                        productions.Add(lines[lineIndex]);
                    }
                    lineIndex++;
                }

                if (lineIndex == lines.Count)
                {
                    return;
                }

                while (lineIndex < lines.Count
                    && string.IsNullOrWhiteSpace(lines[lineIndex]))
                {
                    lineIndex++;
                }

                if (lineIndex == lines.Count)
                {
                    return;
                }

                string start = lines[lineIndex].Replace(" ", "");

                await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
                    () => OnLoadGrammarCallBack(nonterminals, terminals, productions, start));
            }
        }

        async private void OnLoadGrammarCallBack(List<string> nonterminals, List<string> terminals, List<string> productions, string start)
        {
            Nonterminals.Clear();
            Terminals.Clear();
            StartNonterminal = null;
            foreach (var item in nonterminals)
            {
                Nonterminals.Add(new TokenViewer
                {
                    Token = item,
                    IsStart = false,
                    Type = TokenType.Nonterminal
                });
            }
            foreach (var item in terminals)
            {
                Terminals.Add(new TokenViewer
                {
                    Token = item,
                    IsStart = false,
                    Type = TokenType.Terminal
                });
            }
            Terminals.Add(new TokenViewer
            {
                Token = "ε",
                IsStart = false,
                Type = TokenType.Epsilon
            });
            foreach (var item in Nonterminals)
            {
                if (item.Token.Equals(start))
                {
                    StartNonterminal = item;
                    StartNonterminal.IsStart = true;
                }
            }

            ProductionsLoadByLocal = new ObservableCollection<ProductionViewer>();
            foreach (var item in productions)
            {
                string[] nonterminalAndCandidate = item.Split('#');
                if (nonterminalAndCandidate.Length != 2)
                {
                    ProductionsLoadByLocal = null;
                    break;
                }
                else
                {
                    try
                    {
                        ProductionViewer newProduction = new ProductionViewer
                        {
                            Nonterminal = Nonterminals.First(elem => elem.Token.Equals(nonterminalAndCandidate[0])),
                            Candidates = new List<TokenViewer>()
                        };
                        string[] candidates = nonterminalAndCandidate[1].Split('.');
                        foreach (var elem in candidates)
                        {
                            if (!string.IsNullOrWhiteSpace(elem))
                            {
                                newProduction.Candidates.Add(Nonterminals.Concat(Terminals).First(e => e.Token.Equals(elem)));
                            }
                        }
                        ProductionsLoadByLocal.Add(newProduction);
                    }
                    catch (Exception)
                    {
                        ProductionsLoadByLocal = null;
                        break;
                    }
                }
            }

            if (Nonterminals.Count == 0
                || Terminals.Count == 0
                || StartNonterminal == null
                || ProductionsLoadByLocal == null)
            {
                Nonterminals.Clear();
                Terminals.Clear();
                StartNonterminal = null;

                MessageDialog error = new MessageDialog("文件读取失败") { Title = "错误" };
                error.Commands.Add(new UICommand("确定"));
                await error.ShowAsync();
            }
            else
            {
                this.Frame.Navigate(typeof(ProductionPage), Nonterminals.Concat(Terminals));
            }
        }
    }
}
