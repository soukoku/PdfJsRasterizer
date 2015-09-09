using MiscUtil.Threading;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace PdfJsRenderer
{
    [ComVisibleAttribute(true)]
    public class ScriptCallback : INotifyPropertyChanged
    {
        public ScriptCallback()
        {
            _savePool = new CustomThreadPool();
            _savePool.SetMinMaxThreads(1, Math.Max(1, Environment.ProcessorCount - 1));
            _savePool.StartMinThreads();
            PdfFile = OwinServer.SamplePdfUrl;
        }

        string _saveFolder;
        CustomThreadPool _savePool;

        public event PropertyChangedEventHandler PropertyChanged;
        void RaisePropertyChanged([CallerMemberName] string property = "")
        {
            var hand = PropertyChanged;
            if (hand != null) { hand(this, new PropertyChangedEventArgs(property)); }
        }

        List<string> _tempFilesToClean = new List<string>();
        internal void AddTempFile(string file)
        {
            _tempFilesToClean.Add(file);
        }

        #region properties

        private string _pdfFile;

        public string PdfFile
        {
            get { return _pdfFile; }
            set { _pdfFile = value; RaisePropertyChanged(); }
        }

        private int _dpi = 200;

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
            set { _isBusy = value; RaisePropertyChanged(); }
        }


        #endregion

        #region callbacks

        public void PdfOpened(string id, int pages)
        {
            Error = null;
            IsBusy = true;
            RenderedPages = 0;
            TotalPages = pages;

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

                    // resave to optimize a tiny bit
                    using (var ms = new MemoryStream(binData))
                    using (var origImg = Image.FromStream(ms))
                    using (var outImg = new Bitmap(origImg.Width, origImg.Height, PixelFormat.Format24bppRgb))
                    using (var g = Graphics.FromImage(outImg))
                    {
                        g.DrawImage(origImg, new Rectangle(0, 0, origImg.Width, origImg.Height));
                        outImg.SetResolution(DPI, DPI);
                        outImg.Save(filePath, ImageFormat.Png);
                    }
                }));
            }
        }

        public void RenderCompleted()
        {
            IsBusy = false;
            CleanTemp();

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
            CleanTemp();
        }

        private void CleanTemp()
        {
            foreach (var file in _tempFilesToClean)
            {
                try
                {
                    File.Delete(file);
                }
                catch { }
            }
            _tempFilesToClean.Clear();
        }

        #endregion
    }
}
