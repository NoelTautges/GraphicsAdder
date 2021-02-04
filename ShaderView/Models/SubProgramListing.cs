using System;
using uTinyRipper.Classes.Shaders;

namespace ShaderView.Models
{
    public class SubProgramListing : ComponentListing
    {
        public int ShaderIndex { get; set; } = -1;
        public ShaderSubProgramBlob[] Blobs { get; set; } = Array.Empty<ShaderSubProgramBlob>();
        public int DirectXIndex { get; set; } = -1;
        public int OpenGLIndex { get; set; } = -1;
        public uint BlobIndex { get; set; } = 0;
    }
}
