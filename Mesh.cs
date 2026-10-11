namespace EpsilonSolarSystem;

/// <summary>
/// Vertices on the GPU drawn as triangles, a closed line or points. A vertex is one or more attributes of
/// the shader, at locations 0, 1, and so on.
/// </summary>
sealed class Mesh : IDisposable
{
    readonly GL gl;
    readonly PrimitiveType primitive;
    readonly uint vertexArray;
    readonly uint vertexBuffer;
    readonly uint vertexCount;

    public Mesh(GL gl, PrimitiveType primitive, Vector3<float>[] vertices) : this(gl, primitive, AsFloats(vertices), [3]) { }

    public Mesh(GL gl, PrimitiveType primitive, Vector4<float>[] vertices) : this(gl, primitive, AsFloats(vertices), [4]) { }

    // The vertices one after another, each with its attributes in order; attribute i has attributeSizes[i] floats.
    Mesh(GL gl, PrimitiveType primitive, ReadOnlySpan<float> data, int[] attributeSizes)
    {
        this.gl = gl;
        this.primitive = primitive;
        var floatsPerVertex = attributeSizes.Sum();
        vertexCount = (uint)(data.Length / floatsPerVertex);

        vertexArray = gl.GenVertexArray();
        gl.BindVertexArray(vertexArray);
        vertexBuffer = gl.GenBuffer();
        gl.BindBuffer(BufferTargetARB.ArrayBuffer, vertexBuffer);
        gl.BufferData(BufferTargetARB.ArrayBuffer, data, BufferUsageARB.StaticDraw);
        var offset = 0;
        for (var i = 0; i < attributeSizes.Length; i++)
        {
            gl.EnableVertexAttribArray((uint)i);
            gl.VertexAttribPointer((uint)i, attributeSizes[i], VertexAttribPointerType.Float, false,
                (uint)(floatsPerVertex * sizeof(float)), offset * sizeof(float));
            offset += attributeSizes[i];
        }
        gl.BindVertexArray(0);
    }

    /// <summary>
    /// The unit sphere around the origin, poles on the z axis; a point on it is also its normal. A vertex is
    /// the point and its place on an equirectangular map: the longitude from 0 at -180 degrees (on -x) to 1 at
    /// +180 degrees, growing eastward (counterclockwise seen from +z), and the latitude from 0 at the north
    /// pole to 1 at the south pole, which is the first row of the map's image downward.
    /// </summary>
    public static Mesh Sphere(GL gl, int stacks = 64, int slices = 128)
    {
        // Latitude-longitude grid: stacks from the north pole down, slices around the z axis. Each cell gives
        // two triangles, counterclockwise seen from outside. The last slice ends where the first begins, with
        // its own vertices at longitude 1 instead of 0, so the map has no seam there.
        var data = new List<float>(stacks * slices * 6 * 5);
        for (var i = 0; i < stacks; i++)
            for (var j = 0; j < slices; j++)
                foreach (var (stack, slice) in (ReadOnlySpan<(int, int)>)[(i, j), (i + 1, j), (i + 1, j + 1), (i, j), (i + 1, j + 1), (i, j + 1)])
                {
                    var polar = Math.PI * stack / stacks;
                    var longitude = (double)slice / slices;
                    var azimuth = 2 * Math.PI * longitude - Math.PI;
                    data.AddRange([
                        (float)(Math.Sin(polar) * Math.Cos(azimuth)), (float)(Math.Sin(polar) * Math.Sin(azimuth)), (float)Math.Cos(polar),
                        (float)longitude, (float)stack / stacks]);
                }
        return new(gl, PrimitiveType.Triangles, [.. data], [3, 2]);
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

    /// <summary>An arrow of length 1 from the origin along +x, its head in the xy plane: three line segments.</summary>
    public static Mesh Arrow(GL gl)
    {
        Vector3<float>[] points = [new(0, 0, 0), new(1, 0, 0), new(1, 0, 0), new(0.8f, 0.07f, 0), new(1, 0, 0), new(0.8f, -0.07f, 0)];
        return new(gl, PrimitiveType.Lines, points);
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

    // The fixed-size vectors store their entries in order without padding, so they go to the GPU as they are.
    static ReadOnlySpan<float> AsFloats<T>(T[] vertices) where T : struct =>
        System.Runtime.InteropServices.MemoryMarshal.Cast<T, float>(vertices);
}
