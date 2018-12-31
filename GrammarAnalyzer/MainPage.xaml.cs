using GrammarAnalyzer.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Provider;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x804 上介绍了“空白页”项模板

namespace GrammarAnalyzer
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
            Current = this;
        }
        public static MainPage Current;

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            this.Board.Navigate(typeof(TokenPage));
        }

        private void Hamburger_Click(object sender, RoutedEventArgs e)
        {
            this.Navigation.IsPaneOpen = !this.Navigation.IsPaneOpen;
        }

        internal void HideBackButton()
        {
            this.BackToTokenPage.Visibility = Visibility.Collapsed;
        }

        internal void ShowBackButton()
        {
            this.BackToTokenPage.Visibility = Visibility.Visible;
        }

        internal void SelectHome()
        {
            this.HomeSelection.IsSelected = true;
        }


        private void Selection_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.HomeSelection.IsSelected)
            {
                this.Board.Navigate(typeof(TokenPage));
            }
            else if (this.HelpSelection.IsSelected)
            {
                this.Board.Navigate(typeof(HelpPage));
            }
        }

        private void BackToTokenPage_Click(object sender, RoutedEventArgs e)
        {
            this.Board.Navigate(typeof(TokenPage));
        }
    }
}
