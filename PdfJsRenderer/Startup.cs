using Owin;
using Soukoku.Owin.Files;
using Soukoku.Owin.Files.Services.BuiltIn;
using System;
using System.IO;
using System.Linq;

namespace PdfJsRenderer
{
    class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            var pdfjsZip = Directory.EnumerateFiles(Path.Combine(Environment.CurrentDirectory, "dist"), "pdfjs-*-dist.zip").FirstOrDefault();
            if (pdfjsZip != null)
            {
                app.Map("/pdfjs", mapped =>
                {
                    mapped.Use<FilesMiddleware>(new FilesConfig(new ZippedFileDataStore(File.ReadAllBytes(pdfjsZip))));
                });
            }
            
            app.Use<FilesMiddleware>(new FilesConfig(new LooseFilesDataStore(OwinServer.WebContentFolder)));
        }
    }
}