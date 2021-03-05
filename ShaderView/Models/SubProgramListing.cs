using GraphicsAdder.Common;
using System;
using uTinyRipper.Classes.Shaders;

namespace ShaderView.Models
{
    public class SubProgramListing : ComponentListing
    {
        public SubProgramListing(ShaderContext ctx)
        {
            Context = ctx;
        }

        public ShaderContext Context { get; set; }
        public ShaderSubProgramBlob[] Blobs { get; set; } = Array.Empty<ShaderSubProgramBlob>();
        public int DirectXIndex { get; set; } = -1;
        public int OpenGLIndex { get; set; } = -1;
    }
}
