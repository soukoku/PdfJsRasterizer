using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace PdfJsRenderer
{
    static class WebResources
    {
        public static Stream GetStream(string fileName)
        {
            return Assembly.GetExecutingAssembly().GetManifestResourceStream("PdfJsRenderer.Web." + fileName);
        }

        public static byte[] GetBytes(string fileName)
        {
            using (var s = GetStream(fileName))
            {
                if (s != null)
                {
                    byte[] buff = new byte[s.Length];
                    s.Read(buff, 0, buff.Length);
                    return buff;
                }
            }
            return null;
        }
    }
}
