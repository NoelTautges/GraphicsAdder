using DXShaderRestorer;
using HLSLccWrapper;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using uTinyRipper.Classes.Shaders;
using UnityVersion = uTinyRipper.Version;

namespace GraphicsAdder
{
    class GLSLCache
    {
        public UnityVersion Version;
        private Dictionary<uint, string> Map;

        public GLSLCache(UnityVersion version)
        {
            Version = version;
            Map = new Dictionary<uint, string>();
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

                    var startLines = shader.Text.Split("\n");
                    var endLines = new List<string>(startLines.Length);
                    bool inLayout = false;

                    foreach (var line in startLines)
                    {
                        if (line.StartsWith('#') && line.IndexOf("#extension") == -1 && line.IndexOf("#version") == -1)
                        {
                            continue;
                        }

                        if (line.IndexOf("layout(std140)") != -1)
                        {
                            inLayout = true;
                            continue;
                        }
                        else if (line == "};")
                        {
                            inLayout = false;
                            continue;
                        }

                        if (line.IndexOf("unused") == -1)
                        {
                            if (inLayout)
                            {
                                endLines.Add(string.Join("", "uniform".Concat(line)));
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
                        }

                        if (line == "#extension GL_ARB_explicit_attrib_location : require")
                        {
                            endLines.Add("#extension GL_ARB_shader_bit_encoding : enable");
                        }
                    }

                    return string.Join('\n', endLines); ;
                }
            }
        }

        public string GetGLSL(ShaderSubProgram program, uint blobIndex)
        {
            if (!Map.ContainsKey(blobIndex))
                Map[blobIndex] = ConvertToGLSL(program, Version);

            return Map[blobIndex];
        }
    }
}
