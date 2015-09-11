using MiscUtil.Threading;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;

namespace PdfJsRenderer
{
    /// <summary>
    /// The rendering worker.
    /// </summary>
    public class Renderer : INotifyPropertyChanged, IDisposable
    {
        public Renderer()
        {
            _dpi = 200;
            if (ToolServer.IsRunning)
            {
                _watch = new Stopwatch();
                _savePool = new CustomThreadPool();
                _savePool.SetMinMaxThreads(1, Math.Max(1, Environment.ProcessorCount - 1));
                _savePool.StartMinThreads();
                RunBrowserThread(new Uri(ToolServer.RasterizerUrl));
                PdfFile = ToolServer.SamplePdfUrl;
            }
        }
        private void RunBrowserThread(Uri url)
        {
            var th = new Thread(() =>
            {
                _browser = new WebBrowser
                {
                    ObjectForScripting = new ScriptCallback(this)
                };
                _browser.DocumentCompleted += (s, e) =>
                {
                    _ready = true;
                    RaisePropertyChanged(() => CanStart);
                };
                _browser.Url = new Uri(ToolServer.RasterizerUrl);
                Application.Run();
            });
            th.SetApartmentState(ApartmentState.STA);
            th.Start();
        }

        WebBrowser _browser;
        CustomThreadPool _savePool;
        bool _ready;
        SingleFileServer _singleServer;
        Stopwatch _watch;

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

                Log("Starting render of {0}", PdfFile);
                _watch.Restart();
                _browser.Invoke(new Action(() =>
                {
                    _browser.Document.InvokeScript("renderPdf", new object[] { uri.ToString(), DPI });
                }));
            }
        }

        void Cleanup()
        {
            if (_singleServer != null)
            {
                _singleServer.Dispose();
                _singleServer = null;
            }
        }

        public void Dispose()
        {
            Cleanup();
            if (_browser != null)
            {
                _browser.Invoke(new Action(() => { Application.ExitThread(); }));
                _browser = null;
            }
        }

        #region properties

        public bool Verbose { get; set; }
        public string SaveFolder { get; set; }

        public bool CanStart
        {
            get
            {
                return _ready && !string.IsNullOrEmpty(PdfFile) && !IsBusy;
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
            get
            {
                if (_dpi < MinDPI) { return MinDPI; }
                if (_dpi > MaxDPI) { return MaxDPI; }
                return _dpi;
            }
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

        public delegate void LogCallback(string message, params object[] args);

        private LogCallback _log = (msg, args) => Console.WriteLine(msg, args);
        public LogCallback Log
        {
            get { return _log; }
            set { if (value != null) { _log = value; } }
        }



        #endregion

        #region callbacks

        public event EventHandler<PdfOpenedEvenrArgs> PdfOpened;
        public event EventHandler<PageOpenedEvenrArgs> PageOpened;
        public event EventHandler<PageRenderedEvenrArgs> PageRendered;
        public event EventHandler RenderCompleted;
        public event EventHandler<FailedEvenrArgs> Failed;

        void OnPdfOpened(string id, int pages)
        {
            TotalPages = pages;

            Log("Received pdf info of {0} pages.", pages);

            if (string.IsNullOrEmpty(SaveFolder)) { SaveFolder = Environment.CurrentDirectory; }

            SaveFolder = Path.Combine(Environment.CurrentDirectory, id);

            if (Directory.Exists(SaveFolder))
            {
                foreach (var file in Directory.EnumerateFiles(SaveFolder))
                {
                    File.Delete(file);
                }
            }
            else
            {
                Directory.CreateDirectory(SaveFolder);
            }

            if (PdfOpened != null) { PdfOpened(this, new PdfOpenedEvenrArgs { Id = id, Pages = pages }); }
        }

        void OnPageOpened(int pageNumber, dynamic page)
        {
            if (PageOpened != null) { PageOpened(this, new PageOpenedEvenrArgs { PageNumber = pageNumber, Page = page }); }

            //var viewport = page.getViewport(1);
            //int w = viewport.width;
            //int h = viewport.height;
        }

        void OnPageRendered(int pageNumber, string data)
        {
            if (SaveFolder != null)
            {
                Log("Received rendered page {0}.", pageNumber);

                RenderedPages = pageNumber;

                _savePool.AddWorkItem(new Action(() =>
                {
                    var filePath = Path.Combine(SaveFolder, string.Format("{0:0000}.png", pageNumber));
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

            if (PageRendered != null) { PageRendered(this, new PageRenderedEvenrArgs { PageNumber = pageNumber, Data = data }); }
        }

        void OnRenderCompleted()
        {
            _watch.Stop();
            Log("Render completed in {0}.", _watch.Elapsed);

            IsBusy = false;
            Cleanup();

            while (_savePool.WorkingThreads > 0 || _savePool.QueueLength > 0)
            {
                Thread.Sleep(100);
            }
            using (Process.Start(SaveFolder))
            {
                SaveFolder = null;
            }

            if (RenderCompleted != null) { RenderCompleted(this, EventArgs.Empty); }

        }

        void OnFailed(string info)
        {
            _watch.Stop();
            Log("Render failed: {0}.", info);

            Error = info;
            IsBusy = false;
            Cleanup();

            if (Failed != null) { Failed(this, new FailedEvenrArgs { Info = info }); }
        }

        [ComVisibleAttribute(true)]
        public class ScriptCallback
        {
            private Renderer _renderer;

            internal ScriptCallback(Renderer renderer)
            {
                _renderer = renderer;
            }

            public void pdfOpened(string id, int pages)
            {
                _renderer.OnPdfOpened(id, pages);
            }

            public void pageOpened(int pageNumber, dynamic page)
            {
                _renderer.OnPageOpened(pageNumber, page);
            }

            public void pageRendered(int pageNumber, string data)
            {
                _renderer.OnPageRendered(pageNumber, data);
            }

            public void renderCompleted()
            {
                _renderer.OnRenderCompleted();
            }

            public void failed(string info)
            {
                _renderer.OnFailed(info);
            }

        }
        #endregion
    }
}
