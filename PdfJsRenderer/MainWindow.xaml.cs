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
    partial class MainWindow : Window
    {
        Renderer _vm;
        public MainWindow()
        {
            InitializeComponent();
            _vm = (Renderer)DataContext;
        }

        protected override void OnClosed(EventArgs e)
        {
            _vm.Dispose();
            base.OnClosed(e);
        }

        private void startBtn_Click(object sender, RoutedEventArgs e)
        {
            _vm.Start();
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
