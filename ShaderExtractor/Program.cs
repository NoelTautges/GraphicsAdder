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

            foreach (var dep in converter.RootFile.dependencies)
            {
                if (dep == null)
                {
                    continue;
                }

                Console.WriteLine(dep.name);
                var relative = Path.GetRelativePath(converter.RootPath, dep.path);
                Directory.CreateDirectory(Path.Combine(destPath, Path.GetDirectoryName(relative) ?? ""));
                converter.ConvertFile(dep, Path.Combine(@"D:\test", relative), false);
            }
        }
    }
}
