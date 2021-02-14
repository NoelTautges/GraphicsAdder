﻿using DXShaderRestorer;
using HLSLccWrapper;
using SmartFormat;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using uTinyRipper.Classes.Shaders;
using UnityVersion = uTinyRipper.Version;

namespace GraphicsAdder.Common
{
    public class GLSLCache
    {
        private readonly Dictionary<string, object> Constants = new()
        {
            { "SmallFloat", 0.0001 }
        };

        private UnityVersion version;
        private Dictionary<string, string> unprocessedMap = new();
        private Dictionary<string, string> processedMap = new();
        private Dictionary<string, List<string>> replacements = new();

        public GLSLCache(UnityVersion version)
        {
            this.version = version;

            foreach (var path in Directory.EnumerateFiles("Patches"))
            {
                var name = Path.GetFileName(path).Replace("_", "/").Replace(".txt", "");
                replacements.Add(name, new());

                foreach (var line in File.ReadLines(path))
                {
                    replacements[name].Add(Smart.Format(line.Replace("\n", ""), Constants));
                }
            }
        }

        private string ConvertToGLSL(ShaderSubProgram program, UnityVersion version)
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

        private string ProcessGLSL(string glsl, string shaderName)
        {
            var startLines = glsl.Split("\n");
            var endLines = new List<string>(startLines.Length);
            var pastHeader = false;
            var inLayout = false;
            var inStruct = false;
            var structNames = new List<string>();
            var inInputs = false;
            var inputNum = 0;
            var inputClass = "";
            var inputClassNum = 0;

            foreach (var line in startLines)
            {
                if (!pastHeader && line == "")
                {
                    pastHeader = true;
                }
                else if ((pastHeader && line.StartsWith('#')) ||
                    line.Contains("unused") ||
                    line.Contains("//"))
                {
                    continue;
                }

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

                if (line.Contains("struct"))
                {
                    inStruct = true;
                    structNames.Add(line.Split(" ")[1].Replace("_Type", ""));
                }
                else if (inStruct && line == "};")
                {
                    inStruct = false;
                }

                if (line.Contains("layout(location = ") && line.Contains(" in "))
                {
                    inInputs = true;
                }
                else if (inInputs && !line.Contains(" in "))
                {
                    inInputs = false;
                }
                if (inInputs)
                {
                    var identifier = line.Split(" ").Last().Replace(";", "");
                    var currentInputClass = Regex.Replace(identifier, @"\d", "");
                    int.TryParse(identifier.Replace(currentInputClass, ""), out var currentInputClassNum);
                    var expectedClassNum = 0;
                    
                    if (currentInputClass == inputClass)
                    {
                        expectedClassNum = inputClassNum + 1;
                    }

                    inputNum += 1 + currentInputClassNum - expectedClassNum;
                    inputClass = currentInputClass;
                    inputClassNum = currentInputClassNum;
                }

                if (inLayout)
                {
                    endLines.Add(string.Join("", "uniform".Concat(line)));
                }
                else if (inStruct && line.Contains("."))
                {
                    endLines.Add(line.Replace(structNames.Last() + ".", ""));
                }
                else if (inInputs)
                {
                    endLines.Add(Regex.Replace(line, @"location = \d+", $"location = {inputNum - 1}"));
                }
                else if (line.Contains(" / _ProjectionParams.y;"))
                {
                    var statement = line.Split(" = ")[1];
                    var identifier = statement.Split(".")[0];
                    endLines.Add(line.Replace(statement, $"({identifier}.w - {identifier}.z) / _ProjectionParams.y / 2.0;"));
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

            var processed = string.Join('\n', endLines);

            if (replacements.TryGetValue("general", out var generalReplacements))
            {
                for (int i = 0; i < Math.Floor(generalReplacements.Count / 2.0); i++)
                {
                    processed = processed.Replace(generalReplacements[i * 2], generalReplacements[i * 2 + 1]);
                }
            }
            if (replacements.TryGetValue(shaderName, out var specificReplacements))
            {
                for (int i = 0; i < Math.Floor(specificReplacements.Count / 2.0); i++)
                {
                    processed = processed.Replace(specificReplacements[i * 2], specificReplacements[i * 2 + 1]);
                }
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
            var map = unprocessed ? unprocessedMap : processedMap;

            if (!map.ContainsKey(key))
            {
                unprocessedMap[key] = ConvertToGLSL(program, version);
                processedMap[key] = ProcessGLSL(unprocessedMap[key], shaderName.Split(" (")[0]);
            }

            return map[key];
        }
    }
}
