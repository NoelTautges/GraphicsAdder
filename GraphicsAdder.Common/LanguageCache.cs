using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using uTinyRipper.Classes.Shaders;

using UnityVersion = uTinyRipper.Version;

namespace GraphicsAdder.Common
{
    public class LanguageCache
    {
        private ConcurrentDictionary<string, string> unprocessedMap;
        private ConcurrentDictionary<string, string> processedMap;
        private LanguageConverter converter;

        public LanguageCache(UnityVersion version)
        {
            unprocessedMap = new();
            processedMap = new();
            converter = new(version);
        }

        public string GetGLSL(ShaderContext ctx, bool unprocessed = false)
        {
            var key = $"{ctx.ShaderName}-{ctx.BlobIndex}";
            var map = unprocessed ? unprocessedMap : processedMap;

            if (!map.ContainsKey(key))
            {
                unprocessedMap[key] = converter.ConvertToLang(ctx.Program.GetValueOrDefault());
                processedMap[key] = converter.ProcessGLSL(unprocessedMap[key], ctx);
            }

            return map[key];
        }

        public void ProcessSubPrograms(ShaderContext ctx, ShaderSubProgramBlob blob)
        {
            var vertexSubPrograms = ctx.Pass["progVertex"]["m_SubPrograms"]["Array"].children;
            var fragmentSubPrograms = ctx.Pass["progFragment"]["m_SubPrograms"]["Array"].children;
            Parallel.ForEach(vertexSubPrograms.Concat(fragmentSubPrograms), subProgram =>
            {
                var blobIndex = subProgram["m_BlobIndex"].value.AsUInt();
                GetGLSL(ctx.GetContext(blob.SubPrograms[blobIndex], blobIndex));
            });
        }
    }
}
