using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfJsRenderer
{
    public class PdfOpenedEvenrArgs : EventArgs
    {
        public string Id { get; set; }

        public int Pages { get; set; }
    }
    public class PageOpenedEvenrArgs : EventArgs
    {
        public int PageNumber { get; set; }

        public dynamic Page { get; set; }
    }
    public class PageRenderedEvenrArgs : EventArgs
    {
        public int PageNumber { get; set; }

        public string Data { get; set; }
    }
    public class FailedEvenrArgs : EventArgs
    {
        public string Info { get; set; }
    }
}
