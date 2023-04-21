// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Web.WebView2.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace WebResourceRequestTest
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        public const string SchemeName = "http";
        public const string SoundSchemeName = "sound";
        public const string Host = "dicttango.app";

        private string webPageIndex = "0"; //Web page index
        private string url = "";
        private bool hasInit = false;
        private bool loadingWebPage = false; //Is the web page on loading
        private bool isTimerStopped = true;

        private DateTime startTime = DateTime.Now;
        private DateTime endTime = DateTime.Now;
        DispatcherTimer dispatcherTimer = new DispatcherTimer(); //Timer to load the web page repeatly
        public MainWindow()
        {
            this.InitializeComponent();
            dispatcherTimer.Stop();
            dispatcherTimer.Interval = new TimeSpan(0, 0, 1);
            dispatcherTimer.Tick += DispatcherTimer_Tick;

        }

        private void DispatcherTimer_Tick(object sender, object e)
        {
            if (loadingWebPage)
                return;
            txtLog.Text = "";

            if (webPageIndex == "1")
            {
                webPageIndex = "2";
            }
            else
                webPageIndex = "1";
            url = $"{SchemeName}://viewcontent.{Host}/index_{webPageIndex}.html";
            browser.CoreWebView2.Navigate(url);
            loadingWebPage = true;
            startTime = DateTime.Now;
        }

        private async void myButton_Click(object sender, RoutedEventArgs e)
        {
           
            if (!hasInit)
            {
                await browser.EnsureCoreWebView2Async();
                browser.CoreWebView2.Settings.AreBrowserAcceleratorKeysEnabled = true;
                var filter = $"{SchemeName}://*{Host}*";
                browser.CoreWebView2.AddWebResourceRequestedFilter("*", CoreWebView2WebResourceContext.All);
                browser.CoreWebView2.WebResourceRequested += CoreWebView2_WebResourceRequested;
                
                browser.ContextRequested += Browser_ContextRequested;
                browser.CoreWebView2.DocumentTitleChanged += CoreWebView2_DocumentTitleChanged;
                browser.NavigationCompleted += Browser_NavigationCompleted;
                browser.CoreWebView2.ContentLoading += CoreWebView2_ContentLoading;
                browser.CoreWebView2.DOMContentLoaded += CoreWebView2_DOMContentLoaded;
                hasInit = true;
            }
            if (isTimerStopped)
            {
                dispatcherTimer.Start();
            }
            else
            {
                dispatcherTimer.Start();
            }
            isTimerStopped = !isTimerStopped;
        }

     

        private void CoreWebView2_DocumentTitleChanged(CoreWebView2 sender, object args)
        {
            this.Title = browser.CoreWebView2.DocumentTitle??"";
          
        }

        private void Browser_ContextRequested(UIElement sender, ContextRequestedEventArgs args)
        {
            
        }

        private void Browser_NavigationCompleted(WebView2 sender, CoreWebView2NavigationCompletedEventArgs args)
        {
            txtLog.Text += $"{DateTime.Now.ToString("HH:mm:ss")}: {url} NavigationCompleted\r\n";
            endTime = DateTime.Now;
            var duration = endTime.Subtract(startTime).TotalMilliseconds;
            if (duration > 2000)
            {
                dispatcherTimer.Stop();
                txtLog.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));
                txtLog.Text += $"Duration: {duration} Milliseconds";
            }
            else
            {
                txtLog.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 0, 0));
            }
            loadingWebPage = false;
        }
        private void CoreWebView2_DOMContentLoaded(CoreWebView2 sender, CoreWebView2DOMContentLoadedEventArgs args)
        {
            txtLog.Text += $"{DateTime.Now.ToString("HH:mm:ss")}: {url} DOMContentLoaded\r\n";
        }

        private void CoreWebView2_ContentLoading(CoreWebView2 sender, CoreWebView2ContentLoadingEventArgs args)
        {
            txtLog.Text += $"{DateTime.Now.ToString("HH:mm:ss")}: {url} ContentLoading\r\n";
        }
        private void CoreWebView2_WebResourceRequested(CoreWebView2 sender, CoreWebView2WebResourceRequestedEventArgs args)
        {
            var requestHeaders = args.Request.Headers;
            var uri = new Uri(args.Request.Uri);
            var host = uri.Host;
            var fileName = uri.AbsolutePath.Replace("//", "/");
            if (fileName.StartsWith("/"))
                fileName = fileName.Substring(1);
            var contentHeader = "";
            if (fileName.EndsWith(".html"))
                contentHeader = $"Content-Type: text/html";
            else if (fileName.EndsWith(".js"))
                contentHeader = $"Content-Type: text/javascript";
            else
                contentHeader = $"Content-Type: image/jpeg";

            txtLog.Text += $"{DateTime.Now.ToString("HH:mm:ss")}: Load {fileName} started\r\n";
            var stream = LoadAssetFile(fileName);
            if (stream != null)
            {
                CoreWebView2WebResourceResponse response = browser.CoreWebView2.Environment.CreateWebResourceResponse(stream.AsRandomAccessStream(), 200, "OK", contentHeader);
                args.Response = response;
            }
            txtLog.Text += $"{DateTime.Now.ToString("HH:mm:ss")}: Load {fileName} end\r\n";
        }

        public Stream LoadAssetFile(string fileName)
        {
            return GetEmbeddedResourceStream<WebResourceRequestTest.App>($"WebResourceRequestTest.Assets.Web.{fileName}");
        }

        public static Stream GetEmbeddedResourceStream<TSource>(string embeddedFileName) where TSource : class
        {
            var assembly = typeof(TSource).GetTypeInfo().Assembly;
            var resourceName = assembly.GetManifestResourceNames().FirstOrDefault(s => s.EndsWith(embeddedFileName, StringComparison.CurrentCultureIgnoreCase));
            if (resourceName == null)
                return null;
            var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                throw new InvalidOperationException("Could not load manifest resource stream.");
            }

            return stream;
        }
    }
}
