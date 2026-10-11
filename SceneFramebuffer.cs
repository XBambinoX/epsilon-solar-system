namespace EpsilonSolarSystem;

/// <summary>
/// The frame is drawn here and then copied to the window: the window's own depth buffer holds 24-bit
/// integers, and reverse-Z needs 32-bit floats. The color is multisampled, which smooths the orbit lines.
/// </summary>
sealed class SceneFramebuffer : IDisposable
{
    const uint Samples = 4;

    readonly GL gl;
    readonly uint framebuffer;
    uint colorBuffer;
    uint depthBuffer;
    int width;
    int height;

    public SceneFramebuffer(GL gl, int width, int height)
    {
        this.gl = gl;
        framebuffer = gl.GenFramebuffer();
        Resize(width, height);
    }

    public void Resize(int newWidth, int newHeight)
    {
        width = newWidth;
        height = newHeight;
        gl.DeleteRenderbuffer(colorBuffer);
        gl.DeleteRenderbuffer(depthBuffer);
        colorBuffer = Renderbuffer(InternalFormat.Rgba8);
        depthBuffer = Renderbuffer(InternalFormat.DepthComponent32f);

        gl.BindFramebuffer(FramebufferTarget.Framebuffer, framebuffer);
        gl.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, RenderbufferTarget.Renderbuffer, colorBuffer);
        gl.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, RenderbufferTarget.Renderbuffer, depthBuffer);
        var status = gl.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
        if (status != GLEnum.FramebufferComplete)
            throw new InvalidOperationException($"The scene framebuffer is incomplete: {status}.");
        gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
    }

    public void Bind() => gl.BindFramebuffer(FramebufferTarget.Framebuffer, framebuffer);

    /// <summary>Copies the frame to the window, averaging the samples of each pixel.</summary>
    public void CopyToWindow()
    {
        gl.BindFramebuffer(FramebufferTarget.ReadFramebuffer, framebuffer);
        gl.BindFramebuffer(FramebufferTarget.DrawFramebuffer, 0);
        gl.BlitFramebuffer(0, 0, width, height, 0, 0, width, height, ClearBufferMask.ColorBufferBit, BlitFramebufferFilter.Nearest);
        gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
    }

    public void Dispose()
    {
        gl.DeleteRenderbuffer(colorBuffer);
        gl.DeleteRenderbuffer(depthBuffer);
        gl.DeleteFramebuffer(framebuffer);
    }

    uint Renderbuffer(InternalFormat format)
    {
        var renderbuffer = gl.GenRenderbuffer();
        gl.BindRenderbuffer(RenderbufferTarget.Renderbuffer, renderbuffer);
        gl.RenderbufferStorageMultisample(RenderbufferTarget.Renderbuffer, Samples, format, (uint)width, (uint)height);
        return renderbuffer;
    }
}
