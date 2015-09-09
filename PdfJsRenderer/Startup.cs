using Owin;
using Soukoku.Owin.Files;
using Soukoku.Owin.Files.Services.BuiltIn;
using System;
using System.IO;
using System.Linq;

namespace PdfJsRenderer
{
    internal class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            var pdfjsZip = Directory.EnumerateFiles(Path.Combine(Environment.CurrentDirectory, "dist"), "pdfjs-*-dist.zip").FirstOrDefault();
            if (pdfjsZip != null)
            {
                var pdfjsOptions = new FilesConfig(new ZippedFileDataStore(File.ReadAllBytes(pdfjsZip)));
                app.Map("/pdfjs", mapped =>
                {
                    mapped.Use<FilesMiddleware>(pdfjsOptions);
                });
            }
            
            var options = new FilesConfig(new LooseFilesDataStore(OwinServer.WebContentFolder));
            app.Use<FilesMiddleware>(options);
        }
    }
}