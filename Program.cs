var options = WindowOptions.Default with
{
    Title = "Epsilon Solar System",
    Size = new Vector2D<int>(1280, 720),
    API = new GraphicsAPI(ContextAPI.OpenGL, ContextProfile.Core, ContextFlags.ForwardCompatible, new APIVersion(3, 3)),
    PreferredDepthBufferBits = 24,
    VSync = true,
};

using var window = Window.Create(options);
GL gl = null!;
IInputContext input = null!;

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
    gl.ClearColor(0.01f, 0.01f, 0.03f, 1f);
    gl.Enable(EnableCap.DepthTest);
};

window.FramebufferResize += size => gl.Viewport(size);

window.Render += _ => gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

window.Closing += () =>
{
    input.Dispose();
    gl.Dispose();
};

window.Run();
