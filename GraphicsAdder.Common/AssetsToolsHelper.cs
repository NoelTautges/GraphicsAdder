using AssetsTools.NET;
using AssetsTools.NET.Extra;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GraphicsAdder.Common
{
    internal class AssetsToolsHelper
    {
        public static AssetTypeValueField[] GetArrayFromTemplate<T>(AssetTypeValueField template, T[] array)
        {
            return array.Select(value =>
            {
                var val = ValueBuilder.DefaultValueFieldFromArrayTemplate(template);
                val.value.Set(value);
                return val;
            }).ToArray();
        }

        public static void ConcatArray<T>(AssetTypeValueField field, T[] array)
        {
            field = field["Array"];
            var convertedArr = GetArrayFromTemplate(field, array);
            var newChildren = field.children.Concat(convertedArr).ToArray();
            field.SetChildrenList(newChildren);
        }

        public static void SetArray<T>(AssetTypeValueField field, T[] array)
        {
            field = field["Array"];
            field.SetChildrenList(GetArrayFromTemplate(field, array));
        }

        public static uint[] GetUIntArray(AssetTypeValueField field)
        {
            return field["Array"].children.Select(child => child.value.AsUInt()).ToArray();
        }

        public static uint[][] GetUIntDoubleArray(AssetTypeValueField field)
        {
            return field["Array"].children.Select(child => GetUIntArray(child)).ToArray();
        }

        public static void AddReplacer(List<AssetsReplacer> replacers, AssetFileInfoEx assetInfo, AssetTypeValueField field)
        {
            replacers.Add(new AssetsReplacerFromMemory(0, assetInfo.index, (int)assetInfo.curFileType, 0xFFFF, field.WriteToByteArray()));
        }

        public static List<string> PrintFieldChildren(AssetTypeValueField field, int indentCount = 0)
        {
            var lines = new List<string>();
            var indent = new string(' ', indentCount);

            var value = field.templateField.type == "Array" ? $"({field.childrenCount} children)" : field.value != null ? $"= {field.value.AsString()}" : "";
            lines.Add($"{indent}{field.templateField.type} {field.templateField.name} {value}");
            Console.WriteLine(lines.Last());

            foreach (var child in field.children ?? Array.Empty<AssetTypeValueField>())
            {
                lines.AddRange(PrintFieldChildren(child, indentCount + 2));
            }

            return lines;
        }
    }
}
