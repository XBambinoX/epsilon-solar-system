// Simulated days per real second.
const double DaysPerSecond = 1;
const double FieldOfView = Math.PI / 4;
// Far away a planet would be smaller than a pixel, so every body is drawn at least this large.
const double MinimumRadiusInPixels = 3;
// With reverse-Z in a float depth buffer the near plane can be a meter away and the far plane at infinity.
const double NearPlane = 0.001;

var options = WindowOptions.Default with
{
    Title = "Epsilon Solar System",
    Size = new(1280, 720),
    // 4.5 for glClipControl, which reverse-Z needs.
    API = new GraphicsAPI(ContextAPI.OpenGL, ContextProfile.Core, ContextFlags.ForwardCompatible, new APIVersion(4, 5)),
    VSync = true,
};

using var window = Window.Create(options);
GL gl = null!;
IInputContext input = null!;
IKeyboard keyboard = null!;
SceneFramebuffer scene = null!;
Sky sky = null!;
ShaderProgram bodyShader = null!;
ShaderProgram lineShader = null!;
ShaderProgram starShader = null!;
Mesh sphere = null!;
Dictionary<Body, SurfaceMap> maps = null!;
Mesh stars = null!;
Dictionary<Body, Mesh> orbitLines = null!;

var system = new SolarSystem();
var julianDate = SolarSystem.JulianDate(DateTime.UtcNow);
system.Update(julianDate);

// 0 goes over to the Sun, 1 to 8 to the planets, 9 to the Moon; W, A, S, D, E, Q fly; a drag with the left button turns.
var camera = new Camera(system.Sun);
var dragging = false;
System.Numerics.Vector2? lastMouse = null;
// F shows what an engine that keeps its world in float would: the GPU gets coordinates around the Sun instead
// of around the camera, and a float there has steps of 16 km at Earth's distance, 500 km at Neptune's.
var floatWorld = false;

window.Load += () =>
{
    gl = window.CreateOpenGL();
    input = window.CreateInput();
    keyboard = input.Keyboards[0];
    keyboard.KeyDown += (_, key, _) =>
    {
        if (key == Key.Escape)
            window.Close();
        else if (key is >= Key.Number0 and <= Key.Number9)
            camera.GoTo(system.Bodies[key - Key.Number0]);
        else if (key == Key.F)
            floatWorld = !floatWorld;
    };
    foreach (var mouse in input.Mice)
    {
        mouse.Scroll += (_, wheel) => camera.Wheel(wheel.Y);
        // The cursor is captured while dragging, so it never stops at the edge of the screen. Capturing it moves
        // it to the middle of the window, so the first move after that only sets the starting point.
        mouse.MouseDown += (m, button) =>
        {
            if (button != MouseButton.Left)
                return;
            dragging = true;
            m.Cursor.CursorMode = CursorMode.Raw;
            lastMouse = null;
        };
        mouse.MouseUp += (m, button) =>
        {
            if (button != MouseButton.Left)
                return;
            dragging = false;
            m.Cursor.CursorMode = CursorMode.Normal;
        };
        mouse.MouseMove += (_, position) =>
        {
            if (dragging && lastMouse is { } last)
                camera.Turn(position.X - last.X, position.Y - last.Y);
            lastMouse = position;
        };
    }

    Console.WriteLine($"OpenGL {gl.GetStringS(StringName.Version)}, {gl.GetStringS(StringName.Renderer)}");
    Console.WriteLine($"Kepler's equation {Orbit.Kepler.Print()} = 0, its derivative in E {Orbit.KeplerSlope.Print()}");
    // The sky is painted first: that sets its own framebuffer and viewport.
    sky = new Sky(gl);
    // With display scaling the framebuffer is larger than the window, and no resize event comes at the start.
    var size = window.FramebufferSize;
    gl.Viewport(size);
    scene = new SceneFramebuffer(gl, size.X, size.Y);
    gl.ClearColor(0.01f, 0.01f, 0.03f, 1f);
    gl.Enable(EnableCap.CullFace);
    // Reverse-Z: depths from 0 to 1 instead of -1 to 1, 1 at the near plane and 0 at infinity, so nearer is greater.
    gl.Enable(EnableCap.DepthTest);
    gl.ClipControl(ClipControlOrigin.LowerLeft, ClipControlDepth.ZeroToOne);
    gl.ClearDepth(0);
    gl.DepthFunc(DepthFunction.Greater);
    // The stars set their own size, and add their light to the background.
    gl.Enable(EnableCap.ProgramPointSize);
    gl.BlendFunc(BlendingFactor.One, BlendingFactor.One);

    bodyShader = new ShaderProgram(gl, Shaders.BodyVertex, Shaders.BodyFragment);
    lineShader = new ShaderProgram(gl, Shaders.LineVertex, Shaders.LineFragment);
    starShader = new ShaderProgram(gl, Shaders.StarVertex, Shaders.StarFragment);
    sphere = Mesh.Sphere(gl);
    maps = system.Bodies.ToDictionary(body => body, body => new SurfaceMap(gl, body.Texture));
    stars = StarField.Create(gl);
    // The eccentricities change too slowly to matter, so the shapes of the orbits are made once.
    orbitLines = system.Bodies
        .Where(body => body.Orbit is not null)
        .ToDictionary(body => body, body => Mesh.Ellipse(gl, body.OrbitElements.Eccentricity));
};

window.FramebufferResize += size =>
{
    gl.Viewport(size);
    if (size.X > 0 && size.Y > 0)
        scene.Resize(size.X, size.Y);
};

window.Update += deltaTime =>
{
    julianDate += DaysPerSecond * deltaTime;
    system.Update(julianDate);

    // Forward, left, back, right, up, down; Shift is five times faster. The speed is the distance to the
    // nearest surface per second, a meter per second at the least: slow near a body, fast in empty space.
    var direction = Vector3<double>.Zero;
    if (keyboard.IsKeyPressed(Key.W)) direction += Vector3<double>.UnitX;
    if (keyboard.IsKeyPressed(Key.S)) direction -= Vector3<double>.UnitX;
    if (keyboard.IsKeyPressed(Key.A)) direction += Vector3<double>.UnitY;
    if (keyboard.IsKeyPressed(Key.D)) direction -= Vector3<double>.UnitY;
    if (keyboard.IsKeyPressed(Key.E)) direction += Vector3<double>.UnitZ;
    if (keyboard.IsKeyPressed(Key.Q)) direction -= Vector3<double>.UnitZ;
    if (direction != Vector3<double>.Zero)
    {
        var eye = camera.Eye;
        var nearestSurface = system.Bodies.Min(body => eye.Distance(body.Position) - body.Radius);
        var boost = keyboard.IsKeyPressed(Key.ShiftLeft) || keyboard.IsKeyPressed(Key.ShiftRight) ? 5 : 1;
        camera.Fly(direction, boost * Math.Max(nearestSurface, 0.001), deltaTime);
    }
    camera.Update(deltaTime);

    var title = $"Epsilon Solar System - {SolarSystem.ToDateTime(julianDate):yyyy-MM-dd} - " +
        (camera.Destination is { } destination ? $"going to {destination.Name}" :
            $"{(camera.IsFree ? "flying with" : "orbiting")} {camera.Focus.Name}") + " - " +
        (floatWorld ? "float world coordinates (F)" : "camera-relative, double (F)");
    if (window.Title != title)
        window.Title = title;
};

window.Render += _ =>
{
    var size = window.FramebufferSize;
    if (size.X == 0 || size.Y == 0)
        return; // minimized
    scene.Bind();
    gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

    // Everything is computed in double, then turned into float for the GPU around this origin: the camera
    // itself, so the coordinates near it stay small and exact, or the Sun, as a float world would have it.
    var eye = camera.Eye;
    var origin = floatWorld ? Vector3<double>.Zero : eye;
    var cameraPosition = eye - origin;
    var view = Matrix4x4<double>.LookAt(cameraPosition, cameraPosition + camera.Forward, camera.Up);
    var projection = Matrix4x4<double>.Perspective(
        FieldOfView, (double)size.X / size.Y, NearPlane, double.PositiveInfinity, DepthRange.OneToZero);
    var viewProjection = (projection * view).Map(float.CreateTruncating);

    // The sky and the stars first: they are at infinity, behind everything, where the depth test would drop them.
    // The sky is looked up through the inverse of the projection and a view that only turns.
    gl.Disable(EnableCap.DepthTest);
    var turn = Matrix4x4<double>.LookAt(Vector3<double>.Zero, camera.Forward, camera.Up);
    sky.Draw((projection * turn).Inverse().Map(float.CreateTruncating));
    gl.Enable(EnableCap.Blend);
    starShader.Use();
    starShader.Set("uViewProjection", viewProjection);
    stars.Draw();
    gl.Disable(EnableCap.Blend);
    gl.Enable(EnableCap.DepthTest);

    bodyShader.Use();
    bodyShader.Set("uViewProjection", viewProjection);
    bodyShader.Set("uCameraPosition", cameraPosition.Map(float.CreateTruncating));
    bodyShader.Set("uSunPosition", (system.Sun.Position - origin).Map(float.CreateTruncating));
    var radiansPerPixel = FieldOfView / size.Y;
    bodyShader.Set("uSurface", 0);
    foreach (var body in system.Bodies)
    {
        var radius = Math.Max(body.Radius, MinimumRadiusInPixels * radiansPerPixel * eye.Distance(body.Position));
        // A body blown up into a marker is lit evenly, so its night side does not hide it;
        // at four times its size and more it is flat, at its own size it gets the dim ambient light only.
        var marker = Math.Clamp(Math.Log2(radius / body.Radius) / 2, 0, 1);
        var model = Matrix4x4<double>.Compose(body.Position - origin, body.Rotation, Vector3<double>.One * radius);
        bodyShader.Set("uModel", model.Map(float.CreateTruncating));
        bodyShader.Set("uNormalMatrix", model.NormalMatrix().Map(float.CreateTruncating));
        bodyShader.Set("uGlow", body == system.Sun);
        bodyShader.Set("uAmbient", (float)double.Lerp(0.02, 1, marker));
        maps[body].Bind(TextureUnit.Texture0);
        sphere.Draw();
    }

    // An orbit line is the unit ellipse scaled by the semi-major axis, turned into place and centered on the parent.
    lineShader.Use();
    lineShader.Set("uViewProjection", viewProjection);
    foreach (var (body, line) in orbitLines)
    {
        var model = Matrix4x4<double>.Compose(
            body.Parent!.Position - origin, body.OrbitOrientation, Vector3<double>.One * body.OrbitElements.SemiMajorAxis);
        lineShader.Set("uModel", model.Map(float.CreateTruncating));
        lineShader.Set("uColor", body.Color * 0.4f);
        line.Draw();
    }

    scene.CopyToWindow();
};

window.Closing += () =>
{
    foreach (var line in orbitLines.Values)
        line.Dispose();
    stars.Dispose();
    sky.Dispose();
    foreach (var map in maps.Values)
        map.Dispose();
    sphere.Dispose();
    starShader.Dispose();
    lineShader.Dispose();
    bodyShader.Dispose();
    scene.Dispose();
    input.Dispose();
    gl.Dispose();
};

window.Run();
