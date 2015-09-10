using Microsoft.Owin.Hosting;
using Owin;
using Soukoku.Owin.Files;
using Soukoku.Owin.Files.Services.BuiltIn;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;

namespace PdfJsRenderer
{
    /// <summary>
    /// Main owin web server hosting the rendering tool page.
    /// </summary>
    public class ToolServer
    {
        public static int GetFreePort()
        {
            using (var sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                sock.Bind(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 0));
                return ((IPEndPoint)sock.LocalEndPoint).Port;
            }
        }

        public static string ServerUrl { get; private set; }

        public static string SamplePdfUrl { get; private set; }

        public static string RasterizerUrl { get; private set; }

        public static bool IsRunning { get; private set; }

        internal static IDisposable Start()
        {
            if (IsRunning) { return null; }

            ServerUrl = "http://localhost:" + ToolServer.GetFreePort();
            SamplePdfUrl = ServerUrl + "/pdfjs/web/compressed.tracemonkey-pldi-09.pdf";
            RasterizerUrl = ServerUrl + "/rasterizer.html";
            var server = WebApp.Start<MainStartup>(ServerUrl);
            IsRunning = true;
            return server;
        }

        class MainStartup
        {
            public void Configuration(IAppBuilder app)
            {
                app.Map("/pdfjs", mapped =>
                {
                    mapped.Use<FilesMiddleware>(new FilesConfig(new ZippedFileDataStore(WebResources.GetBytes("pdfjs-1.1.215-dist.zip"))));
                });

                var tempFolder = Path.Combine(Path.GetTempPath(), "PdfJsRenderer");
                if (!Directory.Exists(tempFolder)) { Directory.CreateDirectory(tempFolder); }
                File.WriteAllBytes(Path.Combine(tempFolder, "rasterizer.html"), WebResources.GetBytes("rasterizer.html"));

                app.Use<FilesMiddleware>(new FilesConfig(new LooseFilesDataStore(tempFolder)));
            }
        }

    }

}
