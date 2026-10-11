// Simulated days per real second.
const double DaysPerSecond = 1;
const double FieldOfView = Math.PI / 4;
// Far away a planet would be smaller than a pixel, so every body is drawn at least this large.
const double MinimumRadiusInPixels = 3;

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
ShaderProgram bodyShader = null!;
ShaderProgram lineShader = null!;
Mesh sphere = null!;
Dictionary<Body, Mesh> orbitLines = null!;

var system = new SolarSystem();
var julianDate = SolarSystem.JulianDate(DateTime.UtcNow);
system.Update(julianDate);
var realTime = 0.0;

// The camera circles the body in focus: 0 is the Sun, 1 to 8 the planets, 9 the Moon; the wheel zooms.
var focus = system.Sun;
var distance = DefaultDistance(focus);

window.Load += () =>
{
    gl = window.CreateOpenGL();
    input = window.CreateInput();
    foreach (var keyboard in input.Keyboards)
        keyboard.KeyDown += (_, key, _) =>
        {
            if (key == Key.Escape)
                window.Close();
            else if (key is >= Key.Number0 and <= Key.Number9)
            {
                focus = system.Bodies[key - Key.Number0];
                distance = DefaultDistance(focus);
            }
        };
    foreach (var mouse in input.Mice)
        mouse.Scroll += (_, wheel) => distance = Math.Max(distance * Math.Pow(1.2, -wheel.Y), 1.5 * focus.Radius);

    Console.WriteLine($"OpenGL {gl.GetStringS(StringName.Version)}, {gl.GetStringS(StringName.Renderer)}");
    // With display scaling the framebuffer is larger than the window, and no resize event comes at the start.
    gl.Viewport(window.FramebufferSize);
    gl.ClearColor(0.01f, 0.01f, 0.03f, 1f);
    gl.Enable(EnableCap.DepthTest);
    gl.Enable(EnableCap.CullFace);

    bodyShader = new ShaderProgram(gl, Shaders.BodyVertex, Shaders.BodyFragment);
    lineShader = new ShaderProgram(gl, Shaders.LineVertex, Shaders.LineFragment);
    sphere = Mesh.Sphere(gl);
    // The eccentricities change too slowly to matter, so the shapes of the orbits are made once.
    orbitLines = system.Bodies
        .Where(body => body.Orbit is not null)
        .ToDictionary(body => body, body => Mesh.Ellipse(gl, body.OrbitElements.Eccentricity));
};

window.FramebufferResize += size => gl.Viewport(size);

window.Update += deltaTime =>
{
    realTime += deltaTime;
    julianDate += DaysPerSecond * deltaTime;
    system.Update(julianDate);

    var title = $"Epsilon Solar System - {SolarSystem.ToDateTime(julianDate):yyyy-MM-dd} - {focus.Name}";
    if (window.Title != title)
        window.Title = title;
};

window.Render += _ =>
{
    gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
    var size = window.FramebufferSize;
    if (size.X == 0 || size.Y == 0)
        return; // minimized

    // One turn in two minutes around the body in focus, 20 degrees above the plane of the ecliptic.
    var azimuth = 2 * Math.PI * realTime / 120;
    var elevation = double.DegreesToRadians(20);
    var direction = new Vector3<double>(
        Math.Cos(elevation) * Math.Cos(azimuth),
        Math.Cos(elevation) * Math.Sin(azimuth),
        Math.Sin(elevation));
    var eye = focus.Position + direction * distance;
    var view = Matrix4x4<double>.LookAt(eye, focus.Position, Vector3<double>.UnitZ);
    // The near plane keeps to the distance from the surface in focus; the far plane is at infinity.
    var near = 0.01 * (distance - focus.Radius);
    var projection = Matrix4x4<double>.Perspective(
        FieldOfView, (double)size.X / size.Y, near, double.PositiveInfinity, DepthRange.MinusOneToOne);
    var viewProjection = (projection * view).Map(float.CreateTruncating);

    bodyShader.Use();
    bodyShader.Set("uViewProjection", viewProjection);
    bodyShader.Set("uCameraPosition", eye.Map(float.CreateTruncating));
    bodyShader.Set("uSunPosition", system.Sun.Position.Map(float.CreateTruncating));
    var radiansPerPixel = FieldOfView / size.Y;
    for (var i = 0; i < system.Bodies.Count; i++)
    {
        var body = system.Bodies[i];
        var radius = Math.Max(body.Radius, MinimumRadiusInPixels * radiansPerPixel * eye.Distance(body.Position));
        // A body blown up into a marker is lit evenly, so its night side does not hide it;
        // at four times its size and more it is flat, at its own size it gets the faint ambient light only.
        var marker = Math.Clamp(Math.Log2(radius / body.Radius) / 2, 0, 1);
        var model = Matrix4x4<double>.Compose(body.Position, body.Rotation, Vector3<double>.One * radius);
        bodyShader.Set("uModel", model.Map(float.CreateTruncating));
        bodyShader.Set("uNormalMatrix", model.NormalMatrix().Map(float.CreateTruncating));
        bodyShader.Set("uPattern", (int)body.Surface.Pattern);
        bodyShader.Set("uColor", body.Surface.Color);
        bodyShader.Set("uSecondColor", body.Surface.SecondColor);
        bodyShader.Set("uPolarCaps", body.Surface.PolarCaps);
        bodyShader.Set("uSeed", 17f * i);
        bodyShader.Set("uAmbient", (float)double.Lerp(0.03, 1, marker));
        sphere.Draw();
    }

    // An orbit line is the unit ellipse scaled by the semi-major axis, turned into place and centered on the parent.
    lineShader.Use();
    lineShader.Set("uViewProjection", viewProjection);
    foreach (var (body, line) in orbitLines)
    {
        var model = Matrix4x4<double>.Compose(
            body.Parent!.Position, body.OrbitOrientation, Vector3<double>.One * body.OrbitElements.SemiMajorAxis);
        lineShader.Set("uModel", model.Map(float.CreateTruncating));
        lineShader.Set("uColor", body.Surface.Color * 0.4f);
        line.Draw();
    }
};

window.Closing += () =>
{
    foreach (var line in orbitLines.Values)
        line.Dispose();
    sphere.Dispose();
    lineShader.Dispose();
    bodyShader.Dispose();
    input.Dispose();
    gl.Dispose();
};

window.Run();

// The Sun is seen with the inner planets around it, any other body from five of its radii.
double DefaultDistance(Body body) => body == system.Sun ? 3 * SolarSystem.Au : 5 * body.Radius;
