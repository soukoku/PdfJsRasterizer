using Microsoft.Owin.Hosting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfJsRenderer
{
    class OwinServer
    {
        public const string ServerUrl = "http://localhost:12345";

        public static readonly string SamplePdfUrl = ServerUrl + "/pdfjs/web/compressed.tracemonkey-pldi-09.pdf";

        public static readonly string RasterizerUrl = ServerUrl + "/rasterizer.html";

        public static string WebContentFolder { get; private set; }

        internal static void Start()
        {
            WebContentFolder = Path.Combine(Environment.CurrentDirectory, "web");
            WebApp.Start<Startup>(ServerUrl);
        }
    }
}
