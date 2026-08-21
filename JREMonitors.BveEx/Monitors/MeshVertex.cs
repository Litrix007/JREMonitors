using System.Runtime.InteropServices;
using SlimDX;
using SlimDX.Direct3D9;

namespace JREMonitors.BveEx.Monitors
{
    [StructLayout(LayoutKind.Sequential)]
    public struct MeshVertex
    {
        public Vector3 Position;
        public Vector3 Normal;
        public Vector2 TexCoord;
        public const VertexFormat Format = VertexFormat.Position | VertexFormat.Normal | VertexFormat.Texture1;
    }
}