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
        public static ProductionPage Current;

        public ObservableCollection<TokenViewer> StartNonterminal = new ObservableCollection<TokenViewer>();
        public ObservableCollection<TokenViewer> Candidate = new ObservableCollection<TokenViewer>();
        public ObservableCollection<ProductionViewer> Productions = new ObservableCollection<ProductionViewer>();

        public ObservableCollection<TokenViewer> Tokens = new ObservableCollection<TokenViewer>();
        public ObservableCollection<TokenViewer> Nonterminals = new ObservableCollection<TokenViewer>();

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
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

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (ToDeleteToken != null)
                Candidate.Remove(ToDeleteToken);
            ToDeleteToken = null;
        }

        private void NonterminalViewer_Click(object sender, ItemClickEventArgs e)
        {
            TokenViewer tokenViewer = (TokenViewer)e.ClickedItem;
            StartNonterminal.Clear();
            StartNonterminal.Add(tokenViewer);
        }

        private void TerminalViewer_Click(object sender, ItemClickEventArgs e)
        {
            TokenViewer tokenViewer = (TokenViewer)e.ClickedItem;
            if (tokenViewer.Type != TokenType.Epsilon)
            {
                try
                {
                    Candidate.First(elem => elem.Type == TokenType.Epsilon);
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
            else if (Candidate.Count == 0)
            {
                Candidate.Add(new TokenViewer
                {
                    Token = tokenViewer.Token,
                    Type = TokenType.Epsilon
                });
            }
        }

        private void Delete_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (sender is FrameworkElement delete)
            {
                FlyoutBase.ShowAttachedFlyout(delete);
            }
        }

        private TokenViewer ToDeleteToken = null;
        private void DeleteViewer_Click(object sender, ItemClickEventArgs e)
        {
            ToDeleteToken = (TokenViewer)e.ClickedItem;
        }

        private void Create_Click(object sender, RoutedEventArgs e)
        {
            if (StartNonterminal.Count != 0
                && Candidate.Count != 0)
            {
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

        private void DeleteProduction_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (sender is FrameworkElement delete)
            {
                FlyoutBase.ShowAttachedFlyout(delete);
            }
        }

        private ProductionViewer ToDeleteProduction = null;
        private void DeleteProduction_Click(object sender, RoutedEventArgs e)
        {
            if (ToDeleteProduction != null)
                Productions.Remove(ToDeleteProduction);
            ToDeleteProduction = null;
        }

        private void DeleteProductionViewer_Click(object sender, ItemClickEventArgs e)
        {
            ToDeleteProduction = (ProductionViewer)e.ClickedItem;
        }

        private void ToLR_Click(object sender, RoutedEventArgs e)
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
                this.Frame.Navigate(typeof(LRAnalysisPage), Tokens);
            }
        }

        private void ToLL_Click(object sender, RoutedEventArgs e)
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
                this.Frame.Navigate(typeof(LLAnalysisPage), Tokens);
            }
        }

        private void ToSLR_Click(object sender, RoutedEventArgs e)
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
                this.Frame.Navigate(typeof(SLRAnalysisPage), Tokens);
            }
        }

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

        async private void SaveGrammarInLocal(StorageFile file, List<TokenViewer> tokens, List<ProductionViewer> productions)
        {
            string toSave = "";
            // Prevent updates to the remote version of the file until we finish making changes and call CompleteUpdatesAsync.
            CachedFileManager.DeferUpdates(file);
            // write to file
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
            toSave += '\n';
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
            toSave += '\n';
            foreach (var item in productions)
            {
                string production = item.Nonterminal.Token + "#";
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
                    production += '.';
                }
                production.Remove(production.LastIndexOf('.'));
                toSave += production + '\n';
            }
            toSave += "#\n";
            foreach (var item in tokens)
            {
                if (item.IsStart == true)
                {
                    toSave += item.Token;
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
