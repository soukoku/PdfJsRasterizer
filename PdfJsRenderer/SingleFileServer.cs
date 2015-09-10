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
        /// Folder path holder for startup use.
        /// </summary>
        [ThreadStatic]
        static string FolderPath;

        IDisposable _server;
        public SingleFileServer(string filePath)
        {
            if (!File.Exists(filePath)) { throw new FileNotFoundException(); }

            var serverUrl = "http://localhost:" + ToolServer.GetFreePort();
            FolderPath = Path.GetDirectoryName(filePath);
            _server = WebApp.Start<SingleStartUp>(serverUrl);
            FolderPath = null;
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
                app.Use<FilesMiddleware>(new FilesConfig(new LooseFilesDataStore(FolderPath)));
            }
        }
    }
}
