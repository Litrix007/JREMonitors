using System;
using System.IO;
using System.Text;

namespace JREMonitors.Core.Utils
{
    public static class ResourceHelper
    {
        public static Stream GetResourceStream(Type type, string resourceName)
        {
            var assembly = type.Assembly;
            var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null) throw new FileNotFoundException($"Resource '{resourceName}' is not found.");
            return stream;
        }

        public static string GetResourceString(Type type, string resourceName)
        {
            var assembly = type.Assembly;
            using (var stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null) throw new FileNotFoundException($"resource '{resourceName}' is not found.");
                using (var reader = new StreamReader(stream, Encoding.UTF8))
                {
                    return reader.ReadToEnd();
                }
            }
        }

        public static byte[] GetResourceBytes(Type type, string resourceName)
        {
            var assembly = type.Assembly;
            using (var stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null) throw new FileNotFoundException($"resource '{resourceName}' is not found.");

                using (var ms = new MemoryStream())
                {
                    stream.CopyTo(ms);
                    return ms.ToArray();
                }
            }
        }
    }
}