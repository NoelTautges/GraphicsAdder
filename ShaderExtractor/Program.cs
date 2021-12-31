using GraphicsAdder.Common;
using System;
using System.IO;

namespace ShaderExtractor
{
    class Program
    {
        static void Main(string[] args)
        {
            var path = @"D:\test\backup";
            var destPath = @"D:\test";
            var converter = new GraphicsConverter(path);

            foreach (var dep in converter.EngineSettings.dependencies)
            {
                if (dep == null)
                {
                    continue;
                }

                Console.WriteLine(dep.name);
                var relative = Path.GetRelativePath(converter.DataPath, dep.path);
                Directory.CreateDirectory(Path.Combine(path, Path.GetDirectoryName(relative) ?? ""));
                converter.ConvertFile(dep, Path.Combine(destPath, relative), true);
            }
        }
    }
}
