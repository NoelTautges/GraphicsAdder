using DXShaderRestorer;
using HLSLccWrapper;
using SmartFormat;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using uTinyRipper.Classes.Shaders;
using UnityVersion = uTinyRipper.Version;

namespace GraphicsAdder.Common
{
    public class LanguageConverter
    {
        private UnityVersion version;
        private ConcurrentDictionary<string, List<string>> replacements;

        public LanguageConverter(UnityVersion version)
        {
            this.version = version;
            var constants = new Dictionary<string, string>();

            foreach (var line in File.ReadLines("Patches/constants.txt"))
            {
                var split = line.Trim().Split(" = ");
                constants[split[0]] = split[1];
            }

            replacements = new();

            foreach (var path in Directory.EnumerateFiles("Patches"))
            {
                var name = Path.GetFileName(path).Replace("_", "/").Replace(".txt", "");
                replacements[name] = new();

                foreach (var line in File.ReadLines(path))
                {
                    replacements[name].Add(Smart.Format(line.Replace("\n", ""), constants));
                }
            }
        }

        public string ConvertToLang(ShaderSubProgram program)
        {
            using var stream = new MemoryStream(program.ProgramData.Skip(6).ToArray());
            using var reader = new BinaryReader(stream);
            var restoredData = DXShaderProgramRestorer.RestoreProgramData(reader, version, ref program);
            var ext = new WrappedGlExtensions();
            ext.ARB_explicit_attrib_location = 1;
            ext.ARB_explicit_uniform_location = 0;
            ext.ARB_shading_language_420pack = 0;
            ext.OVR_multiview = 0;
            ext.EXT_shader_framebuffer_fetch = 0;
            var shader = Shader.TranslateFromMem(restoredData, WrappedGLLang.LANG_410, ext);

            if (shader.OK == 0)
            {
                throw new InvalidDataException();
            }

            return shader.Text;
        }

        public string ProcessGLSL(string glsl, ShaderContext ctx)
        {
            var startLines = glsl.Split("\n");
            var endLines = new List<string>(startLines.Length);
            var pastHeader = false;
            var inLayout = false;
            var inStruct = false;
            var structNames = new List<string>();

            foreach (var line in startLines)
            {
                // Mark us as being past the header so we begin to strip defines
                // Necessary for version ID + UnityInstancing define, if applicable
                if (!pastHeader && line == "")
                {
                    pastHeader = true;
                }
                // Skip defines (if past the header), unused variables
                // injected by DXShaderRestorer, and comments
                else if ((pastHeader && line.StartsWith('#')) ||
                    line.Contains("unused") ||
                    line.Contains("//"))
                {
                    continue;
                }

                // Skip unused inputs injected in DXShaderRestorer to enable input gaps
                if (line.Contains(" in "))
                {
                    var identifier = line.Split(" ").Last().Replace(";", "");
                    if (glsl.IndexOf(identifier, glsl.IndexOf(identifier) + 1) == -1)
                    {
                        continue;
                    }
                }

                // Strip layout blocks except for UnityInstancing, where it's necessary
                if (line.Contains("layout(std140)") && !line.Contains("UnityInstancing"))
                {
                    inLayout = true;
                    continue;
                }
                else if (inLayout && line == "};")
                {
                    inLayout = false;
                    continue;
                }

                // Mark us as being in a struct and add the corrected type name to the list of structs
                if (line.Contains("struct"))
                {
                    inStruct = true;
                    structNames.Add(line.Split(" ")[1].Replace("_Type", ""));
                }
                else if (inStruct && line == "};")
                {
                    inStruct = false;
                }

                // If we're in a layout, prefix the line with "uniform" to mark the declaration as uniform instead of the block
                if (inLayout)
                {
                    endLines.Add(string.Join("", "uniform".Concat(line)));
                }
                // If we're in a struct, remove the "<struct name>." prefix from each field
                else if (inStruct && line.Contains("."))
                {
                    endLines.Add(line.Replace(structNames.Last() + ".", ""));
                }
                // Correct manually converted Normalized Device Coordinates from the vertex shader
                else if (line.Contains(" / _ProjectionParams.y;"))
                {
                    var statement = line.Split(" = ")[1];
                    var identifier = statement.Split(".")[0];
                    endLines.Add(line.Replace(statement, $"({identifier}.w - {identifier}.z) / _ProjectionParams.y / 2.0;"));
                }
                // Compensate for incorrect _LightTexture[B]0 000R swizzle
                else if (line.Contains("texture(_LightTexture") && !glsl.Contains("samplerCube _LightTexture"))
                {
                    var split = line.Split(" = ");
                    endLines.Add(split[0] + " = vec4(" + split[1].Replace(";", ".w, 0.0, 0.0, 0.0);"));
                }
                else
                {
                    endLines.Add(line);
                }

                if (line == "#extension GL_ARB_explicit_attrib_location : require")
                {
                    endLines.Add("#extension GL_ARB_shader_bit_encoding : enable");
                }
            }

            // Apply general and specific patches for this shader
            var processed = string.Join('\n', endLines);

            // General patches are guaranteed to be there, but nullability dictates that we try to get it anyway
            if (replacements.TryGetValue("general", out var generalReplacements))
            {
                for (int i = 0; i < Math.Floor(generalReplacements.Count / 2.0); i++)
                {
                    processed = processed.Replace(generalReplacements[i * 2], generalReplacements[i * 2 + 1]);
                }
            }
            if (replacements.TryGetValue(ctx.Shader.ParsedForm.Name, out var specificReplacements))
            {
                for (int i = 0; i < Math.Floor(specificReplacements.Count / 2.0); i++)
                {
                    processed = processed.Replace(specificReplacements[i * 2], specificReplacements[i * 2 + 1]);
                }
            }

            // Remove extra struct dotting in main function
            foreach (var structName in structNames)
            {
                processed = processed.Replace($".{structName}.", ".");
            }

            return processed;
        }
    }
}
