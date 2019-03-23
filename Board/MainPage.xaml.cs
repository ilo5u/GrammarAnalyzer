using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Composition;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x804 上介绍了“空白页”项模板

namespace Board
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();

            InitializeFrostedGlass(this.GlassHost);
        }

        private void InitializeFrostedGlass(UIElement uIElement)
        {
            Visual hostVisual = ElementCompositionPreview.GetElementVisual(uIElement);
            Compositor compositor = hostVisual.Compositor;
            var backdropBrush = compositor.CreateHostBackdropBrush();
            var glassVisual = compositor.CreateSpriteVisual();
            glassVisual.Brush = backdropBrush;
            ElementCompositionPreview.SetElementChildVisual(uIElement, glassVisual);
            var bindSizeAnimation = compositor.CreateExpressionAnimation("hostVisual.Size");
            bindSizeAnimation.SetReferenceParameter("hostVisual", hostVisual);
            glassVisual.StartAnimation("Size", bindSizeAnimation);
        }

        private void HelpButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            this.HelpRect.Fill = new SolidColorBrush(Colors.LightBlue);
            this.EditRect.Fill = new SolidColorBrush(Colors.Transparent);
        }

        private void EditButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            this.EditRect.Fill = new SolidColorBrush(Colors.LightBlue);
            this.HelpRect.Fill = new SolidColorBrush(Colors.Transparent);

            this.MainNavigationFrame.Navigate(typeof(EditPage));
        }
    }
}
