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

        public static string SamplePdfUrl { get; private set; }

        public static string RasterizerUrl { get; private set; }

        public static string WebContentFolder { get; private set; }

        internal static void Start()
        {
            SamplePdfUrl = ServerUrl + "/pdfjs/web/compressed.tracemonkey-pldi-09.pdf";
            RasterizerUrl = ServerUrl + "/rasterizer.html";
            WebContentFolder = Path.Combine(Environment.CurrentDirectory, "web");
            WebApp.Start<Startup>(ServerUrl);
        }
    }
}
