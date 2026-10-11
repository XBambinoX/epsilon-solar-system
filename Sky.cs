namespace EpsilonSolarSystem;

/// <summary>
/// The background of gas, dust and the Milky Way. Its noise is far too slow to compute for every pixel of
/// every frame, so it is painted once, at the start, into the six faces of a cube map; a frame then only
/// looks the cube map up in the direction of each pixel.
/// </summary>
sealed class Sky : IDisposable
{
    const int FaceSize = 1024;

    readonly GL gl;
    readonly uint cubeMap;
    readonly Mesh screen;
    readonly ShaderProgram shader;

    public Sky(GL gl)
    {
        this.gl = gl;
        // One triangle over the whole screen, from (-1, -1) to past the opposite corners.
        Vector3<float>[] corners = [new(-1, -1, 0), new(3, -1, 0), new(-1, 3, 0)];
        screen = new(gl, PrimitiveType.Triangles, corners);
        shader = new(gl, Shaders.ScreenVertex, Shaders.SkyFragment);

        cubeMap = gl.GenTexture();
        gl.BindTexture(TextureTarget.TextureCubeMap, cubeMap);
        gl.TexStorage2D(TextureTarget.TextureCubeMap, 1, SizedInternalFormat.Rgba8, FaceSize, FaceSize);
        gl.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
        gl.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
        gl.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        gl.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
        gl.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapR, (int)TextureWrapMode.ClampToEdge);
        // Filtering across the edges of the faces, so they do not show as lines.
        gl.Enable(EnableCap.TextureCubeMapSeamless);
        Paint();
    }

    /// <summary>
    /// Draws the sky behind everything. The matrix maps the screen back to the camera's space: the inverse of
    /// the projection times a view that only turns.
    /// </summary>
    public void Draw(Matrix4x4<float> inverseViewProjection)
    {
        shader.Use();
        shader.Set("uInverseViewProjection", inverseViewProjection);
        shader.Set("uSky", 0);
        gl.ActiveTexture(TextureUnit.Texture0);
        gl.BindTexture(TextureTarget.TextureCubeMap, cubeMap);
        screen.Draw();
    }

    public void Dispose()
    {
        gl.DeleteTexture(cubeMap);
        shader.Dispose();
        screen.Dispose();
    }

    void Paint()
    {
        using var painter = new ShaderProgram(gl, Shaders.ScreenVertex, Shaders.SkyPaintFragment);
        painter.Use();
        painter.Set("uGalacticPole", StarField.GalacticPole.Map(float.CreateTruncating));

        var framebuffer = gl.GenFramebuffer();
        gl.BindFramebuffer(FramebufferTarget.Framebuffer, framebuffer);
        gl.Viewport(0, 0, FaceSize, FaceSize);
        for (var face = 0; face < 6; face++)
        {
            gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0,
                TextureTarget.TextureCubeMapPositiveX + face, cubeMap, 0);
            painter.Set("uFace", face);
            screen.Draw();
        }
        gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        gl.DeleteFramebuffer(framebuffer);
    }
}
