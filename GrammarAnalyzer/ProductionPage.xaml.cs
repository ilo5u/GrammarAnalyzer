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
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Provider;
using Windows.UI.Core;
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
    public sealed partial class ProductionPage : Page
    {
        public ProductionPage()
        {
            this.InitializeComponent();
            Current = this;
        }
        /// <summary>
        /// actually, this is not a good design to use the instance
        /// across different class objects
        /// </summary>
        public static ProductionPage Current;

        public ObservableCollection<TokenViewer> StartNonterminal = new ObservableCollection<TokenViewer>();
        public ObservableCollection<TokenViewer> Candidate = new ObservableCollection<TokenViewer>();
        public ObservableCollection<ProductionViewer> Productions = new ObservableCollection<ProductionViewer>();

        public ObservableCollection<TokenViewer> Tokens = new ObservableCollection<TokenViewer>();
        public ObservableCollection<TokenViewer> Nonterminals = new ObservableCollection<TokenViewer>();

        private Grammar Raw = new Grammar();
        /// <summary>
        /// reset page resources
        /// </summary>
        /// <param name="e"></param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            // reset all views
            StartNonterminal = new ObservableCollection<TokenViewer>();
            Candidate = new ObservableCollection<TokenViewer>();
            Productions = new ObservableCollection<ProductionViewer>();
            Tokens = new ObservableCollection<TokenViewer>();
            Nonterminals = new ObservableCollection<TokenViewer>();
            // reset grammar info
            Raw = new Grammar();
            // set views
            IEnumerable<TokenViewer> tokenViewers = (IEnumerable<TokenViewer>)e.Parameter;
            foreach (var item in tokenViewers)
            {
                switch (item.Type)
                {
                    case TokenType.Terminal:
                        Tokens.Add(item);
                        break;
                    case TokenType.Nonterminal:
                        Tokens.Add(item);
                        Nonterminals.Add(item);
                        if (item.IsStart == true)
                        {
                            StartNonterminal.Add(item);
                        }
                        break;
                    case TokenType.Epsilon:
                        Tokens.Add(item);
                        break;
                    default:
                        break;
                }
            }

            if (TokenPage.Current.ProductionsLoadByLocal != null)
            {
                Productions = TokenPage.Current.ProductionsLoadByLocal;
            }
        }
        /// <summary>
        /// build the raw grammar instance,
        /// and test the connectivity, or legality
        /// </summary>
        /// <returns></returns>
        private bool BuildAndTest()
        {
            // set grammar info
            Tokens.ToList().ForEach(t =>
            {
                if (t.Type == TokenType.Epsilon)
                {
                    // do not add epsilon causing this token
                    // is added default when generate productions
                }
                else
                {
                    Raw.InsertToken(new Grammar.Token(
                        t.Type == TokenType.Nonterminal ? Grammar.Token.Type.NONTERMINAL : Grammar.Token.Type.TERMINAL,
                        t.Token
                    ));
                }
            });
            bool epsilon = false;
            Productions.ToList().ForEach(p =>
            {
                // but if epsilon existed in productions,
                // the epsilon token is needed to add
                // (casue the difference in backend and frontend)
                if (!epsilon && p.Candidates.Exists(t => t.Type == TokenType.Epsilon))
                {
                    Raw.InsertToken(Grammar.Epsilon);
                    epsilon = true;
                }
                List<Grammar.Token> tokens = new List<Grammar.Token>();
                p.Candidates.ForEach(t => tokens.Add(new Grammar.Token(
                    t.Type == TokenType.Nonterminal ? Grammar.Token.Type.NONTERMINAL : Grammar.Token.Type.TERMINAL,
                    t.Token
                    )));
                Raw.InsertProduction(new Grammar.Prodc(new Grammar.Token(
                    Grammar.Token.Type.NONTERMINAL, p.Nonterminal.Token
                    ), tokens));
            });
            Raw.SetStart(StartNonterminal.First().Token);
            return Raw.ConnectivityTest();
        }
        /// <summary>
        /// delete the selected token in the right part
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (ToDeleteToken != null)
                Candidate.Remove(ToDeleteToken);
            ToDeleteToken = null;
        }
        /// <summary>
        /// select one token
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void NonterminalViewer_Click(object sender, ItemClickEventArgs e)
        {
            TokenViewer tokenViewer = (TokenViewer)e.ClickedItem;
            StartNonterminal.Clear();
            StartNonterminal.Add(tokenViewer);
        }
        /// <summary>
        /// select and add one token into the right part of current production
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TerminalViewer_Click(object sender, ItemClickEventArgs e)
        {
            TokenViewer tokenViewer = (TokenViewer)e.ClickedItem;
            if (tokenViewer.Type != TokenType.Epsilon)
            {
                try
                {
                    Candidate.First(elem => elem.Type == TokenType.Epsilon);
                    // if epsilon token already added, the other tokens including epsilon
                    // can not be chosen to add
                }
                catch (Exception)
                {
                    Candidate.Add(new TokenViewer
                    {
                        Token = tokenViewer.Token,
                        Type = TokenType.Terminal
                    });
                }
            }
            else
            {
                // the right part must be empty to add epsilon token
                Candidate.Clear();
                Candidate.Add(new TokenViewer
                {
                    Token = tokenViewer.Token,
                    Type = TokenType.Epsilon
                });
            }
        }
        /// <summary>
        /// show the delete choic in flyout
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Delete_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (sender is FrameworkElement delete)
            {
                FlyoutBase.ShowAttachedFlyout(delete);
            }
        }
        /// <summary>
        /// record the selected token at the right part in the current production
        /// </summary>
        private TokenViewer ToDeleteToken = null;
        /// <summary>
        /// select one token at the right part
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DeleteViewer_Click(object sender, ItemClickEventArgs e)
        {
            ToDeleteToken = (TokenViewer)e.ClickedItem;
        }
        /// <summary>
        /// take the current prodution input
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Create_Click(object sender, RoutedEventArgs e)
        {
            if (StartNonterminal.Count != 0
                && Candidate.Count != 0)
            {
                // check duplicatied produtions
                bool isExised = false;
                foreach (var item in Productions)
                {
                    if (item.Nonterminal == StartNonterminal.ElementAt(0)
                        && item.Candidates.Count == Candidate.Count)
                    {
                        bool isSame = true;
                        for (int i = 0; i < Candidate.Count; i++)
                        {
                            if (item.Candidates[i].Token != Candidate[i].Token)
                            {
                                isSame = false;
                            }
                        }
                        if (isSame)
                        {
                            isExised = true;
                        }
                    }
                }
                if (!isExised)
                {
                    List<TokenViewer> NewCandidate = new List<TokenViewer>();
                    foreach (var item in Candidate)
                    {
                        NewCandidate.Add(item);
                    }

                    Productions.Add(new ProductionViewer
                    {
                        Nonterminal = StartNonterminal.ElementAt(0),
                        Candidates = NewCandidate
                    });

                    Candidate.Clear();
                }
            }
            else
            {
                if (sender is FrameworkElement empty)
                {
                    FlyoutBase.ShowAttachedFlyout(empty);
                }
            }
        }
        /// <summary>
        /// show the delete choice in flyout
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DeleteProduction_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (sender is FrameworkElement delete)
            {
                FlyoutBase.ShowAttachedFlyout(delete);
            }
        }
        /// <summary>
        /// record selected production temporally
        /// </summary>
        private ProductionViewer ToDeleteProduction = null;
        /// <summary>
        /// delete the selected production
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DeleteProduction_Click(object sender, RoutedEventArgs e)
        {
            if (ToDeleteProduction != null)
                Productions.Remove(ToDeleteProduction);
            ToDeleteProduction = null;
        }
        /// <summary>
        /// select one production
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DeleteProductionViewer_Click(object sender, ItemClickEventArgs e)
        {
            ToDeleteProduction = (ProductionViewer)e.ClickedItem;
        }
        /// <summary>
        /// implement LR(1) analysis
        /// </summary>
        /// <param name="sender">button</param>
        /// <param name="e">flyout</param>
        private void ToLR_Click(object sender, RoutedEventArgs e)
        {
            if (Productions.Count == 0)
            {
                if (sender is FrameworkElement empty)
                {
                    Empty.Visibility = Visibility.Visible;
                    Unusable.Visibility = Visibility.Collapsed;
                    FlyoutBase.ShowAttachedFlyout(empty);
                }
            }
            else
            {
                /// test unusable tokens. e.g.
                /// grammar G as:
                /// A -> a
                /// B -> b
                /// thus, considering G as a undirected graph G',
                /// G' is divided into two parts with A or B unusable
                if (BuildAndTest())
                {
                    this.Frame.Navigate(typeof(LRAnalysisPage), Raw);
                }
                else
                {
                    /// check productions again
                    if (sender is FrameworkElement unusable)
                    {
                        Empty.Visibility = Visibility.Collapsed;
                        Unusable.Visibility = Visibility.Visible;
                        FlyoutBase.ShowAttachedFlyout(unusable);
                    }
                }
            }
        }
        /// <summary>
        /// implement LL(1) analysis
        /// </summary>
        /// <param name="sender">button</param>
        /// <param name="e">flyout</param>
        private void ToLL_Click(object sender, RoutedEventArgs e)
        {
            if (Productions.Count == 0)
            {
                if (sender is FrameworkElement empty)
                {
                    Empty.Visibility = Visibility.Visible;
                    Unusable.Visibility = Visibility.Collapsed;
                    FlyoutBase.ShowAttachedFlyout(empty);
                }
            }
            else
            {
                /// test unusable tokens. e.g.
                /// grammar G as:
                /// A -> a
                /// B -> b
                /// thus, considering G as a undirected graph G',
                /// G' is divided into two parts with A or B unusable
                if (BuildAndTest())
                {
                    this.Frame.Navigate(typeof(LLAnalysisPage), Raw);
                }
                else
                {
                    /// check productions again
                    if (sender is FrameworkElement unusable)
                    {
                        Empty.Visibility = Visibility.Collapsed;
                        Unusable.Visibility = Visibility.Visible;
                        FlyoutBase.ShowAttachedFlyout(unusable);
                    }
                }
            }
        }
        /// <summary>
        /// implement SLR(1) analysis
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ToSLR_Click(object sender, RoutedEventArgs e)
        {
            if (Productions.Count == 0)
            {
                if (sender is FrameworkElement empty)
                {
                    Empty.Visibility = Visibility.Visible;
                    Unusable.Visibility = Visibility.Collapsed;
                    FlyoutBase.ShowAttachedFlyout(empty);
                }
            }
            else
            {
                /// test unusable tokens. e.g.
                /// grammar G as:
                /// A -> a
                /// B -> b
                /// thus, considering G as a undirected graph G',
                /// G' is divided into two parts with A or B unusable
                if (BuildAndTest())
                {
                    this.Frame.Navigate(typeof(SLRAnalysisPage), Raw);
                }
                else
                {
                    /// check productions again
                    if (sender is FrameworkElement unusable)
                    {
                        Empty.Visibility = Visibility.Collapsed;
                        Unusable.Visibility = Visibility.Visible;
                        FlyoutBase.ShowAttachedFlyout(unusable);
                    }
                }
            }
        }
        /// <summary>
        /// store to local disk, including
        /// nonterminals, terminals, the starter and productions
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        async private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (Productions.Count == 0)
            {
                if (sender is FrameworkElement empty)
                {
                    FlyoutBase.ShowAttachedFlyout(empty);
                }
            }
            else
            {
                FileSavePicker savePicker = new FileSavePicker
                {
                    SuggestedStartLocation = PickerLocationId.DocumentsLibrary
                };
                // Dropdown of file types the user can save the file as
                savePicker.FileTypeChoices.Add("Grammar", new List<string>() { ".gra" });
                // Default file name if the user does not type one in or select a file to replace
                savePicker.SuggestedFileName = "Untitled";

                StorageFile file = await savePicker.PickSaveFileAsync();
                if (file != null)
                {
                    new Task(() => SaveGrammarInLocal(file, Tokens.ToList(), Productions.ToList())).Start();
                }
            }
        }
        /// <summary>
        /// async storing task
        /// </summary>
        /// <param name="file"></param>
        /// <param name="tokens"></param>
        /// <param name="productions"></param>
        async private void SaveGrammarInLocal(StorageFile file, List<TokenViewer> tokens, List<ProductionViewer> productions)
        {
            string toSave = "";
            // Prevent updates to the remote version of the file until we finish making changes and call CompleteUpdatesAsync.
            CachedFileManager.DeferUpdates(file);
            // write to file
            // tokens including nonterminals and terminals per row
            foreach (var item in tokens)
            {
                switch (item.Type)
                {
                    case TokenType.Nonterminal:
                        toSave += item.Token + ' ';
                        break;
                    default:
                        break;
                }
            }
            toSave = toSave.Remove(toSave.LastIndexOf(' ')) + '\n';
            foreach (var item in tokens)
            {
                switch (item.Type)
                {
                    case TokenType.Terminal:
                        toSave += item.Token + ' ';
                        break;
                    default:
                        break;
                }
            }
            toSave = toSave.Remove(toSave.LastIndexOf(' ')) + '\n';
            // productions
            foreach (var item in productions)
            {
                string production = item.Nonterminal.Token + '→';
                foreach (var token in item.Candidates)
                {
                    switch (token.Type)
                    {
                        case TokenType.Epsilon:
                            production += "ε";
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
                    production += ' ';
                }
                toSave += production.Remove(production.LastIndexOf(' ')) + '\n';
            }
            // the starter
            foreach (var item in tokens)
            {
                if (item.IsStart == true)
                {
                    toSave += "START: " + item.Token;
                }
            }

            await this.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => OnSaveCompletedCallBack(file, toSave));
        }

        async private void OnSaveCompletedCallBack(StorageFile file, string toSave)
        {
            await FileIO.WriteTextAsync(file, toSave);
        }
    }
}
