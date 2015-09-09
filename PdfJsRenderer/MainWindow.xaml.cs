using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace PdfJsRenderer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        ScriptCallback _vm;
        public MainWindow()
        {
            InitializeComponent();
            _vm = (ScriptCallback)DataContext;
            if (!DesignerProperties.GetIsInDesignMode(this))
            {
                browser.ObjectForScripting = _vm;
                browser.Url = new Uri(OwinServer.RasterizerUrl);
            }
        }

        bool _ready;

        private void browser_DocumentCompleted(object sender, System.Windows.Forms.WebBrowserDocumentCompletedEventArgs e)
        {
            _ready = true;
        }

        private void startBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_ready && !string.IsNullOrEmpty(_vm.PdfFile))
            {
                var uri = new Uri(_vm.PdfFile);
                if (uri.IsFile)
                {
                    var fileName = System.IO.Path.GetFileName(_vm.PdfFile);
                    var copyTo = System.IO.Path.Combine(OwinServer.WebContentFolder, fileName);
                    File.Copy(_vm.PdfFile, copyTo, true);
                    _vm.AddTempFile(copyTo);
                    uri = new Uri(OwinServer.ServerUrl + "/" + fileName);
                }
                browser.Document.InvokeScript("renderPdf", new object[] { uri.ToString(), _vm.DPI });
            }
        }

        private void Window_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
            }
        }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            var pdfFile = ((IList<string>)e.Data.GetData(DataFormats.FileDrop)).FirstOrDefault(f => f.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase));
            if (pdfFile != null)
            {
                _vm.PdfFile = pdfFile;
            }
        }
    }
}
