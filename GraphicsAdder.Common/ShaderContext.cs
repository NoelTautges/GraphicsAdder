using AssetsTools.NET;
using uTinyRipper.Classes;
using uTinyRipper.Classes.Shaders;

namespace GraphicsAdder.Common
{
    public class ShaderContext
    {
        public ShaderContext(AssetTypeValueField shader, AssetTypeValueField pass, uint blobIndex = 0)
        {
            Shader = shader;
            ShaderName = shader["m_ParsedForm"]["m_Name"].value.AsString();
            Pass = pass;
            BlobIndex = blobIndex;
        }

        public ShaderContext(AssetTypeValueField shader, AssetTypeValueField pass, ShaderSubProgram program, uint blobIndex = 0)
            : this(shader, pass, blobIndex)
        {
            Program = program;
        }

        public ShaderContext GetContext(ShaderSubProgram program)
        {
            var clone = (ShaderContext)MemberwiseClone();
            clone.Program = program;
            return clone;
        }

        public ShaderContext GetContext(ShaderSubProgram program, uint blobIndex)
        {
            var clone = GetContext(program);
            clone.BlobIndex = blobIndex;
            return clone;
        }

        public AssetTypeValueField Shader { get; set; }
        public string ShaderName { get; set; }
        public AssetTypeValueField Pass { get; set; }
        public ShaderSubProgram? Program { get; set; }
        public uint BlobIndex { get; set; } = 0;
    }
}
