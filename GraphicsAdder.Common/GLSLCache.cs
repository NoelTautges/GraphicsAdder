using DXShaderRestorer;
using GraphicsAdder.Common;
using HLSLccWrapper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using uTinyRipper.Classes.Shaders;
using UnityVersion = uTinyRipper.Version;

namespace GraphicsAdder.Common
{
    public class GLSLCache
    {
        public UnityVersion Version;
        private Dictionary<string, string> UnprocessedMap;
        private Dictionary<string, string> ProcessedMap;
        private Dictionary<string, List<string>> Replacements = new Dictionary<string, List<string>>()
        {
            {
                "Hidden/Post FX/SubpixelMorphologicalAntialiasing",
                new List<string> {
                    "SV_Target0 = textureLod(_MainTex, vs_TEXCOORD0.xy, 0.0);",
                    "SV_Target0 = textureLod(_MainTex, vec2(vs_TEXCOORD0.x, 1 - vs_TEXCOORD0.y), 0.0);"
                }
            }
        };

        public GLSLCache(UnityVersion version)
        {
            Version = version;
            UnprocessedMap = new Dictionary<string, string>();
            ProcessedMap = new Dictionary<string, string>();
        }

        private string ConvertToGLSL(ShaderSubProgram program, UnityVersion version)
        {
            using (MemoryStream stream = new MemoryStream(program.ProgramData.Skip(6).ToArray()))
            {
                using (BinaryReader reader = new BinaryReader(stream))
                {
                    byte[] restoredData = DXShaderProgramRestorer.RestoreProgramData(reader, version, ref program);
                    WrappedGlExtensions ext = new WrappedGlExtensions();
                    ext.ARB_explicit_attrib_location = 1;
                    ext.ARB_explicit_uniform_location = 0;
                    ext.ARB_shading_language_420pack = 0;
                    ext.OVR_multiview = 0;
                    ext.EXT_shader_framebuffer_fetch = 0;
                    Shader shader = Shader.TranslateFromMem(restoredData, WrappedGLLang.LANG_410, ext);

                    if (shader.OK == 0) throw new InvalidDataException();

                    return shader.Text;
                }
            }
        }

        private string ProcessGLSL(string glsl, string shaderName)
        {
            var startLines = glsl.Split("\n");
            var endLines = new List<string>(startLines.Length);
            bool pastHeader = false;
            bool inLayout = false;
            bool inStruct = false;
            var structNames = new List<string>();

            foreach (var line in startLines)
            {
                if (!pastHeader && line == "")
                {
                    pastHeader = true;
                }
                else if (pastHeader && line.StartsWith('#') || line.IndexOf("unused") != -1)
                {
                    continue;
                }

                if (line.IndexOf("layout(std140)") != -1 && line.IndexOf("UnityInstancing") == -1)
                {
                    inLayout = true;
                    continue;
                }
                else if (inLayout && line == "};")
                {
                    inLayout = false;
                    continue;
                }

                if (line.IndexOf("struct") != -1)
                {
                    inStruct = true;
                    structNames.Add(line.Split(" ")[1].Replace("_Type", ""));
                }
                else if (inStruct && line == "};")
                {
                    inStruct = false;
                }

                if (inLayout)
                {
                    endLines.Add(string.Join("", "uniform".Concat(line)));
                }
                else if (inStruct && line.IndexOf(".") != -1)
                {
                    endLines.Add(line.Replace(structNames.Last() + ".", ""));
                }
                else
                {
                    if (line.IndexOf("layout(location = ") != -1 && false)
                    {
                        endLines.Add(line.Split(") ")[1]);
                    }
                    else
                    {
                        endLines.Add(line);
                    }
                }

                if (line == "#extension GL_ARB_explicit_attrib_location : require")
                {
                    endLines.Add("#extension GL_ARB_shader_bit_encoding : enable");
                }
            }

            var processed = string.Join('\n', endLines);
            var replacements = Replacements.GetValueOrDefault(shaderName, new List<string>());

            for (int i = 0; i < Math.Floor(replacements.Count / 2.0); i++)
            {
                processed = processed.Replace(replacements[i * 2], replacements[i * 2 + 1]);
            }

            foreach (var structName in structNames)
            {
                processed = processed.Replace($".{structName}.", ".");
            }

            return processed;
        }

        public string GetGLSL(ShaderSubProgram program, string shaderName, uint blobIndex, bool unprocessed = false)
        {
            var key = $"{shaderName}-{blobIndex}";
            var map = unprocessed ? UnprocessedMap : ProcessedMap;

            if (!map.ContainsKey(key))
            {
                UnprocessedMap[key] = ConvertToGLSL(program, Version);
                ProcessedMap[key] = ProcessGLSL(UnprocessedMap[key], shaderName);
            }

            return map[key];
        }
    }
}
