using MiscUtil.Threading;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PdfJsRenderer
{
    /// <summary>
    /// The rendering worker.
    /// </summary>
    [ComVisibleAttribute(true)]
    public class Renderer : INotifyPropertyChanged
    {
        public Renderer()
        {
            _dpi = 200;
            if (ToolServer.IsRunning)
            {
                _savePool = new CustomThreadPool();
                _savePool.SetMinMaxThreads(1, Math.Max(1, Environment.ProcessorCount - 1));
                _savePool.StartMinThreads();
                _browser = new WebBrowser { ObjectForScripting = this };
                _browser.DocumentCompleted += (s, e) => { _ready = true; RaisePropertyChanged(() => CanStart); };
                _browser.Url = new Uri(ToolServer.RasterizerUrl);
                PdfFile = ToolServer.SamplePdfUrl;
            }
        }

        WebBrowser _browser;
        string _saveFolder;
        CustomThreadPool _savePool;
        bool _ready;
        SingleFileServer _singleServer;

        public event PropertyChangedEventHandler PropertyChanged;
        void RaisePropertyChanged([CallerMemberName] string property = "")
        {
            var hand = PropertyChanged;
            if (hand != null) { hand(this, new PropertyChangedEventArgs(property)); }
        }

        void RaisePropertyChanged<T>(Expression<Func<T>> expr)
        {
            var body = expr.Body as MemberExpression;
            if (body != null)
            {
                RaisePropertyChanged(body.Member.Name);
            }
        }


        #region properties

        public bool CanStart
        {
            get
            {
                return _ready && !string.IsNullOrEmpty(PdfFile) && !IsBusy;
            }
        }

        public void Start()
        {
            if (CanStart)
            {
                Error = null;
                IsBusy = true;
                RenderedPages = 0;

                var uri = new Uri(PdfFile);
                if (uri.IsFile)
                {
                    _singleServer = new SingleFileServer(PdfFile);
                    uri = new Uri(_singleServer.FileUrl);
                }
                _browser.Document.InvokeScript("renderPdf", new object[] { uri.ToString(), DPI });
            }
        }

        private string _pdfFile;
        public string PdfFile
        {
            get { return _pdfFile; }
            set
            {
                _pdfFile = value;
                RaisePropertyChanged();
                RaisePropertyChanged(() => CanStart);
            }
        }

        public int MinDPI { get { return 96; } }
        public int MaxDPI { get { return 300; } }

        private int _dpi;
        public int DPI
        {
            get { return _dpi; }
            set { _dpi = value; RaisePropertyChanged(); }
        }


        private string _error;
        public string Error
        {
            get { return _error; }
            set { _error = value; RaisePropertyChanged(); }
        }

        private int _totalPgs;
        public int TotalPages
        {
            get { return _totalPgs; }
            set { _totalPgs = value; RaisePropertyChanged(); }
        }

        private int _renderedPages;
        public int RenderedPages
        {
            get { return _renderedPages; }
            set { _renderedPages = value; RaisePropertyChanged(); }
        }

        private bool _isBusy;
        public bool IsBusy
        {
            get { return _isBusy; }
            set
            {
                _isBusy = value;
                RaisePropertyChanged();
                RaisePropertyChanged(() => CanStart);
            }
        }


        #endregion

        #region callbacks

        public void PdfOpened(string id, int pages)
        {
            TotalPages = pages;

            _saveFolder = ConfigurationManager.AppSettings["SaveFolder"];
            if (string.IsNullOrEmpty(_saveFolder)) { _saveFolder = Environment.CurrentDirectory; }

            _saveFolder = Path.Combine(Environment.CurrentDirectory, id);

            if (Directory.Exists(_saveFolder))
            {
                foreach (var file in Directory.EnumerateFiles(_saveFolder))
                {
                    File.Delete(file);
                }
            }
            else
            {
                Directory.CreateDirectory(_saveFolder);
            }
        }

        public void PageRendered(int page, string data)
        {
            if (_saveFolder != null)
            {
                RenderedPages = page;

                _savePool.AddWorkItem(new Action(() =>
                {
                    var filePath = Path.Combine(_saveFolder, string.Format("{0:0000}.png", page));
                    var base64Data = Regex.Match(data, @"data:image/(?<type>.+?),(?<data>.+)").Groups["data"].Value;
                    var binData = Convert.FromBase64String(base64Data);

                    // resave to set dpi
                    using (var ms = new MemoryStream(binData))
                    using (var img = (Bitmap)Image.FromStream(ms))
                    {
                        img.SetResolution(DPI, DPI);
                        img.Save(filePath, ImageFormat.Png);
                    }
                }));
            }
        }

        public void RenderCompleted()
        {
            IsBusy = false;
            Cleanup();

            while (_savePool.WorkingThreads > 0 || _savePool.QueueLength > 0)
            {
                Thread.Sleep(100);
            }
            using (Process.Start(_saveFolder))
            {
                _saveFolder = null;
            }
        }

        public void Failed(string info)
        {
            Error = info;
            IsBusy = false;
            Cleanup();
        }

        void Cleanup()
        {
            if (_singleServer != null)
            {
                _singleServer.Dispose();
                _singleServer = null;
            }
        }

        #endregion
    }
}
