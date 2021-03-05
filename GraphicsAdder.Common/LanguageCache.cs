using System.Collections.Concurrent;
using System.Threading.Tasks;
using uTinyRipper.Classes.Shaders;
using UnityVersion = uTinyRipper.Version;

namespace GraphicsAdder.Common
{
    public class LanguageCache
    {

        private UnityVersion version;
        private ConcurrentDictionary<string, string> unprocessedMap;
        private ConcurrentDictionary<string, string> processedMap;
        private LanguageConverter converter;

        public LanguageCache(UnityVersion version)
        {
            this.version = version;
            unprocessedMap = new();
            processedMap = new();
            converter = new();
        }

        public string GetGLSL(ShaderSubProgram program, string shaderName, uint blobIndex, bool unprocessed = false)
        {
            var key = $"{shaderName}-{blobIndex}";
            var map = unprocessed ? unprocessedMap : processedMap;

            if (!map.ContainsKey(key))
            {
                unprocessedMap[key] = converter.ConvertToGLSL(program, version);
                processedMap[key] = converter.ProcessGLSL(unprocessedMap[key], shaderName.Split(" (")[0]);
            }

            return map[key];
        }

        private void ProcessSubProgramList(SerializedProgram program, ShaderSubProgramBlob blob, string shaderName)
        {
            Parallel.ForEach(program.SubPrograms, subProgram =>
            {
                GetGLSL(blob.SubPrograms[subProgram.BlobIndex], shaderName, subProgram.BlobIndex);
            });
        }

        public void ProcessSubPrograms(SerializedPass pass, ShaderSubProgramBlob blob, string shaderName)
        {
            ProcessSubProgramList(pass.ProgVertex, blob, shaderName);
            ProcessSubProgramList(pass.ProgFragment, blob, shaderName);
        }
    }
}
