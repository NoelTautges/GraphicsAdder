using System.Collections.Concurrent;
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
            var key = $"{ctx.Shader.ParsedForm.Name}-{ctx.BlobIndex}";
            var map = unprocessed ? unprocessedMap : processedMap;

            if (!map.ContainsKey(key))
            {
                unprocessedMap[key] = converter.ConvertToLang(ctx.Program.GetValueOrDefault());
                processedMap[key] = converter.ProcessGLSL(unprocessedMap[key], ctx);
            }

            return map[key];
        }

        private void ProcessSubProgramList(ShaderContext ctx, SerializedProgram program, ShaderSubProgramBlob blob)
        {
            Parallel.ForEach(program.SubPrograms, subProgram =>
            {
                GetGLSL(ctx.GetContext(blob.SubPrograms[subProgram.BlobIndex], subProgram.BlobIndex));
            });
        }

        public void ProcessSubPrograms(ShaderContext ctx, ShaderSubProgramBlob blob)
        {
            ProcessSubProgramList(ctx, ctx.Pass.ProgVertex, blob);
            ProcessSubProgramList(ctx, ctx.Pass.ProgFragment, blob);
        }
    }
}
