using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PdfJsRenderer
{
    class Program
    {
        /// <summary>
        /// Application Entry Point.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                // UI mode
                Console.WriteLine("Starting UI in stand-alone mode...");
                ToolServer.Start();
                App app = new App();
                app.Run();
            }
            else
            {
                // command mode
                var options = new CommandOptions();
                if (CommandLine.Parser.Default.ParseArguments(args, options))
                {
                    ToolServer.Start();
                    var r = new Renderer
                    {
                        PdfFile = options.InputFile,
                        DPI = options.DPI,
                        SaveFolder = options.OutFolder,
                        Verbose = options.Verbose
                    };
                    while (!r.CanStart) { Thread.Sleep(100); }
                    r.Start();
                    while (r.IsBusy) { Thread.Sleep(100); }
                    r.ExitBrowserThread();
                }

#if DEBUG
                Console.ReadLine();
#endif
            }
        }
    }
}
