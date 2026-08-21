using BveTypes.ClassWrappers;
using SlimDX;
using SlimDX.Direct3D9;

namespace JREMonitors.BveEx.Monitors
{
    public static class PanelElementFactory
    {
        public static Mesh CreateMonitorMesh(Vector3 v0, Vector3 v1, Vector3 v2, Vector3 v3, Vector3 normal)
        {
            var mesh = new Mesh(Direct3DProvider.Instance.Device, 2, 4, MeshFlags.Managed,
                MeshVertex.Format);

            using (var vertexStream = mesh.LockVertexBuffer(LockFlags.None))
            {
                MeshVertex[] vertices =
                {
                    new MeshVertex
                    {
                        Position = v0, Normal = normal, TexCoord = new Vector2(0, 0)
                    },
                    new MeshVertex
                    {
                        Position = v1, Normal = normal, TexCoord = new Vector2(1, 0)
                    },
                    new MeshVertex
                    {
                        Position = v3, Normal = normal, TexCoord = new Vector2(0, 1)
                    },
                    new MeshVertex
                    {
                        Position = v2, Normal = normal, TexCoord = new Vector2(1, 1)
                    }
                };
                vertexStream.WriteRange(vertices);
                mesh.UnlockVertexBuffer();
            }

            using (var indexStream = mesh.LockIndexBuffer(LockFlags.None))
            {
                short[] indices =
                {
                    0, 1, 2,
                    2, 1, 3
                };
                indexStream.WriteRange(indices);
                mesh.UnlockIndexBuffer();
            }

            return mesh;
        }
    }
}