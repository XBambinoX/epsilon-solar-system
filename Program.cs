var options = WindowOptions.Default with
{
    Title = "Epsilon Solar System",
    Size = new(1280, 720),
    API = new GraphicsAPI(ContextAPI.OpenGL, ContextProfile.Core, ContextFlags.ForwardCompatible, new APIVersion(3, 3)),
    PreferredDepthBufferBits = 24,
    VSync = true,
};

using var window = Window.Create(options);
GL gl = null!;
IInputContext input = null!;
ShaderProgram shader = null!;
SphereMesh sphere = null!;
var time = 0.0;

window.Load += () =>
{
    gl = window.CreateOpenGL();
    input = window.CreateInput();
    foreach (var keyboard in input.Keyboards)
        keyboard.KeyDown += (_, key, _) =>
        {
            if (key == Key.Escape)
                window.Close();
        };

    Console.WriteLine($"OpenGL {gl.GetStringS(StringName.Version)}, {gl.GetStringS(StringName.Renderer)}");
    // With display scaling the framebuffer is larger than the window, and no resize event comes at the start.
    gl.Viewport(window.FramebufferSize);
    gl.ClearColor(0.01f, 0.01f, 0.03f, 1f);
    gl.Enable(EnableCap.DepthTest);
    gl.Enable(EnableCap.CullFace);

    shader = new ShaderProgram(gl, Shaders.BodyVertex, Shaders.BodyFragment);
    sphere = new SphereMesh(gl);
};

window.FramebufferResize += size => gl.Viewport(size);

window.Update += deltaTime => time += deltaTime;

window.Render += _ =>
{
    gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
    var size = window.FramebufferSize;
    if (size.X == 0 || size.Y == 0)
        return; // minimized

    // The camera circles the Sun slowly, a little above the plane of the orbits.
    var angle = 0.2 * time;
    var eye = new Vector3<double>(8 * Math.Cos(angle), 3, 8 * Math.Sin(angle));
    var view = Matrix4x4<double>.LookAt(eye, Vector3<double>.Zero, Vector3<double>.UnitY);
    var projection = Matrix4x4<double>.Perspective(Math.PI / 4, (double)size.X / size.Y, 0.1, 100, DepthRange.MinusOneToOne);

    shader.Use();
    shader.Set("uViewProjection", (projection * view).Map(float.CreateTruncating));
    shader.Set("uCameraPosition", eye.Map(float.CreateTruncating));
    shader.Set("uSunPosition", Vector3<float>.Zero);

    DrawBody(Vector3<double>.Zero, 1, new(1f, 0.8f, 0.35f), emissive: true);
    DrawBody(new(4, 0, 0), 0.35, new(0.25f, 0.5f, 1f), emissive: false);
};

window.Closing += () =>
{
    sphere.Dispose();
    shader.Dispose();
    input.Dispose();
    gl.Dispose();
};

window.Run();

void DrawBody(Vector3<double> position, double radius, Vector3<float> color, bool emissive)
{
    var model = Matrix4x4<double>.Compose(position, Quaternion<double>.Identity, Vector3<double>.One * radius);
    shader.Set("uModel", model.Map(float.CreateTruncating));
    shader.Set("uNormalMatrix", model.NormalMatrix().Map(float.CreateTruncating));
    shader.Set("uColor", color);
    shader.Set("uEmissive", emissive);
    sphere.Draw();
}
