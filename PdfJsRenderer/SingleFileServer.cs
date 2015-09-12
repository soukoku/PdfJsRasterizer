using Microsoft.Owin.Hosting;
using Owin;
using Soukoku.Owin.Files;
using Soukoku.Owin.Files.Services.BuiltIn;
using System;
using System.IO;

namespace PdfJsRenderer
{

    /// <summary>
    /// Single file web server for processing a local pdf file.
    /// </summary>
    sealed class SingleFileServer : IDisposable
    {
        /// <summary>
        /// File path holder for startup use.
        /// </summary>
        [ThreadStatic]
        static string FilePath;

        IDisposable _server;
        public SingleFileServer(string filePath)
        {
            if (!File.Exists(filePath)) { throw new FileNotFoundException(); }

            var serverUrl = "http://localhost:" + ToolServer.GetFreePort();
            FilePath = filePath;
            _server = WebApp.Start<SingleStartUp>(serverUrl);
            FilePath = null;
            FileUrl = serverUrl + "/" + Path.GetFileName(filePath);
        }

        public void Dispose()
        {
            if (_server != null)
            {
                _server.Dispose();
                _server = null;
            }
        }

        public string FileUrl { get; private set; }


        class SingleStartUp
        {
            public void Configuration(IAppBuilder app)
            {
                app.Use<FilesMiddleware>(new FilesConfig(new SingleFilesDataStore(FilePath)));
            }
        }
    }
}
