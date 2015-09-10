using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfJsRenderer
{
    /// <summary>
    /// Contains commandline option.
    /// </summary>
    class CommandOptions
    {
        [Option('f', "file", Required = true, HelpText = "PDF file to be rendered.")]
        public string InputFile { get; set; }

        [Option('o', "out", Required = false, HelpText = "Render output folder.")]
        public string OutFolder { get; set; }

        [Option('d', "dpi", Required = false, HelpText = "Rendered image DPI.", DefaultValue = 200)]
        public int DPI { get; set; }

        [Option('v', "verbose", HelpText = "Print details during execution.")]
        public bool Verbose { get; set; }

        [HelpOption('h', "help")]
        public string GetUsage()
        {
            var test = CommandLine.Text.HelpText.AutoBuild(this);
            return test.ToString();
        }
    }
}
