namespace EpsilonSolarSystem;

/// <summary>An image from the Textures folder on the GPU, with mipmaps.</summary>
sealed class SurfaceMap : IDisposable
{
    readonly GL gl;
    readonly uint handle;

    public SurfaceMap(GL gl, string fileName)
    {
        this.gl = gl;
        using var stream = File.OpenRead(Path.Combine(AppContext.BaseDirectory, "Textures", fileName));
        var image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlue);

        handle = gl.GenTexture();
        gl.BindTexture(TextureTarget.Texture2D, handle);
        gl.PixelStore(PixelStoreParameter.UnpackAlignment, 1);
        gl.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgb8, (uint)image.Width, (uint)image.Height, 0,
            PixelFormat.Rgb, PixelType.UnsignedByte, (ReadOnlySpan<byte>)image.Data);
        gl.GenerateMipmap(TextureTarget.Texture2D);
        // Around the body the map repeats; at the poles it stops.
        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
        // Sharp near the edge of the disk too, where the surface is seen at a slant.
        gl.TexParameter(TextureTarget.Texture2D, GLEnum.TextureMaxAnisotropy, 8f);
    }

    public void Bind(TextureUnit unit)
    {
        gl.ActiveTexture(unit);
        gl.BindTexture(TextureTarget.Texture2D, handle);
    }

    public void Dispose() => gl.DeleteTexture(handle);
}
