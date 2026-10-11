namespace EpsilonSolarSystem;

/// <summary>Points on the GPU drawn as triangles or as a closed line.</summary>
sealed class Mesh : IDisposable
{
    readonly GL gl;
    readonly PrimitiveType primitive;
    readonly uint vertexArray;
    readonly uint vertexBuffer;
    readonly uint vertexCount;

    public Mesh(GL gl, PrimitiveType primitive, Vector3<float>[] vertices)
    {
        this.gl = gl;
        this.primitive = primitive;
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

    /// <summary>The unit sphere around the origin, poles on the z axis; a point on it is also its normal.</summary>
    public static Mesh Sphere(GL gl, int stacks = 32, int slices = 64)
    {
        // Latitude-longitude grid: stacks from the north pole (+z) down, slices around the z axis.
        // Each cell gives two triangles, counterclockwise seen from outside.
        var triangles = new List<Vector3<float>>(stacks * slices * 6);
        for (var i = 0; i < stacks; i++)
            for (var j = 0; j < slices; j++)
            {
                var a = Point(i, j);
                var b = Point(i + 1, j);
                var c = Point(i + 1, j + 1);
                var d = Point(i, j + 1);
                triangles.AddRange([a, b, c, a, c, d]);
            }
        return new(gl, PrimitiveType.Triangles, [.. triangles]);

        Vector3<float> Point(int stack, int slice)
        {
            var polar = MathF.PI * stack / stacks;
            var azimuth = 2 * MathF.PI * slice / slices;
            return new(MathF.Sin(polar) * MathF.Cos(azimuth), MathF.Sin(polar) * MathF.Sin(azimuth), MathF.Cos(polar));
        }
    }

    /// <summary>
    /// The ellipse of semi-major axis 1 with a focus at the origin and the perihelion on +x, in the xy plane:
    /// an orbit in its own plane, which <see cref="Orbit.At"/> turns into place.
    /// </summary>
    public static Mesh Ellipse(GL gl, double eccentricity, int segments = 4096)
    {
        var points = new Vector3<float>[segments];
        var minorAxis = Math.Sqrt(1 - eccentricity * eccentricity);
        for (var i = 0; i < segments; i++)
        {
            var eccentricAnomaly = 2 * Math.PI * i / segments;
            points[i] = new Vector3<double>(Math.Cos(eccentricAnomaly) - eccentricity, minorAxis * Math.Sin(eccentricAnomaly), 0)
                .Map(float.CreateTruncating);
        }
        return new(gl, PrimitiveType.LineLoop, points);
    }

    public void Draw()
    {
        gl.BindVertexArray(vertexArray);
        gl.DrawArrays(primitive, 0, vertexCount);
    }

    public void Dispose()
    {
        gl.DeleteBuffer(vertexBuffer);
        gl.DeleteVertexArray(vertexArray);
    }
}
