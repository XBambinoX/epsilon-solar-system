namespace EpsilonSolarSystem;

/// <summary>A linked vertex and fragment shader with setters for the uniforms of Epsilon types.</summary>
sealed class ShaderProgram : IDisposable
{
    readonly GL gl;
    readonly uint handle;

    public ShaderProgram(GL gl, string vertexSource, string fragmentSource)
    {
        this.gl = gl;
        var vertex = Compile(ShaderType.VertexShader, vertexSource);
        var fragment = Compile(ShaderType.FragmentShader, fragmentSource);
        handle = gl.CreateProgram();
        gl.AttachShader(handle, vertex);
        gl.AttachShader(handle, fragment);
        gl.LinkProgram(handle);
        gl.DeleteShader(vertex);
        gl.DeleteShader(fragment);
        gl.GetProgram(handle, ProgramPropertyARB.LinkStatus, out var linked);
        if (linked == 0)
            throw new InvalidOperationException($"The shader program did not link: {gl.GetProgramInfoLog(handle)}");
    }

    public void Use() => gl.UseProgram(handle);

    // Epsilon matrices store their entries row by row, and GLSL reads column by column,
    // so the matrix goes to the GPU as it is, with transpose = true.
    public void Set(string name, Matrix4x4<float> value) =>
        gl.UniformMatrix4(Location(name), true, AsFloats(in value));

    public void Set(string name, Matrix3x3<float> value) =>
        gl.UniformMatrix3(Location(name), true, AsFloats(in value));

    public void Set(string name, Vector3<float> value) => gl.Uniform3(Location(name), value.X, value.Y, value.Z);

    public void Set(string name, float value) => gl.Uniform1(Location(name), value);

    public void Set(string name, int value) => gl.Uniform1(Location(name), value);

    public void Set(string name, bool value) => gl.Uniform1(Location(name), value ? 1 : 0);

    public void Dispose() => gl.DeleteProgram(handle);

    uint Compile(ShaderType type, string source)
    {
        var shader = gl.CreateShader(type);
        gl.ShaderSource(shader, source);
        gl.CompileShader(shader);
        gl.GetShader(shader, ShaderParameterName.CompileStatus, out var compiled);
        if (compiled == 0)
            throw new InvalidOperationException($"The {type} did not compile: {gl.GetShaderInfoLog(shader)}");
        return shader;
    }

    int Location(string name) => gl.GetUniformLocation(handle, name);

    static ReadOnlySpan<float> AsFloats<T>(in T value) where T : struct =>
        System.Runtime.InteropServices.MemoryMarshal.Cast<T, float>(new ReadOnlySpan<T>(in value));
}
