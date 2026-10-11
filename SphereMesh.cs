namespace EpsilonSolarSystem;

/// <summary>The unit sphere around the origin as a list of triangles; a point on it is also its normal.</summary>
sealed class SphereMesh : IDisposable
{
    readonly GL gl;
    readonly uint vertexArray;
    readonly uint vertexBuffer;
    readonly uint vertexCount;

    public SphereMesh(GL gl, int stacks = 32, int slices = 64)
    {
        this.gl = gl;
        var vertices = Triangles(stacks, slices);
        vertexCount = (uint)vertices.Length;

        vertexArray = gl.GenVertexArray();
        gl.BindVertexArray(vertexArray);
        vertexBuffer = gl.GenBuffer();
        gl.BindBuffer(BufferTargetARB.ArrayBuffer, vertexBuffer);
        gl.BufferData(BufferTargetARB.ArrayBuffer, vertices, BufferUsageARB.StaticDraw);
        gl.EnableVertexAttribArray(0);
        gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
        gl.BindVertexArray(0);
    }

    public void Draw()
    {
        gl.BindVertexArray(vertexArray);
        gl.DrawArrays(PrimitiveType.Triangles, 0, vertexCount);
    }

    public void Dispose()
    {
        gl.DeleteBuffer(vertexBuffer);
        gl.DeleteVertexArray(vertexArray);
    }

    // Latitude-longitude grid: stacks from the north pole (+y) down, slices around the y axis.
    // Each cell gives two triangles, counterclockwise seen from outside.
    static Vector3<float>[] Triangles(int stacks, int slices)
    {
        var triangles = new List<Vector3<float>>(stacks * slices * 6);
        for (var i = 0; i < stacks; i++)
            for (var j = 0; j < slices; j++)
            {
                var a = Point(i, j);
                var b = Point(i + 1, j);
                var c = Point(i + 1, j + 1);
                var d = Point(i, j + 1);
                triangles.AddRange([a, c, b, a, d, c]);
            }
        return [.. triangles];

        Vector3<float> Point(int stack, int slice)
        {
            var polar = MathF.PI * stack / stacks;
            var azimuth = 2 * MathF.PI * slice / slices;
            return new(MathF.Sin(polar) * MathF.Cos(azimuth), MathF.Cos(polar), MathF.Sin(polar) * MathF.Sin(azimuth));
        }
    }
}
