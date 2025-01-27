using System.IO;
using System.Reflection;

namespace Veldrid.SceneGraph.IO
{
    public class Assets
    {
        public static byte[] ReadEmbeddedAssetBytes(Assembly asm, string name)
        {
            //string[] names = asm.GetManifestResourceNames();
            
            using (Stream stream = asm.GetManifestResourceStream(name))
            {
                var bytes = new byte[stream.Length];
                using (MemoryStream ms = new MemoryStream(bytes))
                {
                    stream.CopyTo(ms);
                    return bytes;
                }
            }
        }
    }
}