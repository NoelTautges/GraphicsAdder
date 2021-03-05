using uTinyRipper.Classes;
using uTinyRipper.Classes.Shaders;

namespace GraphicsAdder.Common
{
    public class ShaderContext
    {
        public ShaderContext(Shader shader, SerializedPass pass, uint blobIndex = 0)
        {
            Shader = shader;
            Pass = pass;
            BlobIndex = blobIndex;
        }

        public ShaderContext(Shader shader, SerializedPass pass, ShaderSubProgram program, uint blobIndex = 0)
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

        public Shader Shader { get; set; }
        public SerializedPass Pass { get; set; }
        public ShaderSubProgram? Program { get; set; }
        public uint BlobIndex { get; set; } = 0;
    }
}
