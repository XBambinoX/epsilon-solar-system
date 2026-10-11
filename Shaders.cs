namespace EpsilonSolarSystem;

/// <summary>GLSL sources.</summary>
static class Shaders
{
    /// <summary>A sphere placed by uModel; the vertex of the unit sphere is also its normal.</summary>
    public const string BodyVertex = """
        #version 330 core

        layout(location = 0) in vec3 aPosition;

        uniform mat4 uModel;
        uniform mat3 uNormalMatrix;
        uniform mat4 uViewProjection;

        out vec3 vWorldPosition;
        out vec3 vNormal;

        void main()
        {
            vec4 world = uModel * vec4(aPosition, 1.0);
            vWorldPosition = world.xyz;
            vNormal = uNormalMatrix * aPosition;
            gl_Position = uViewProjection * world;
        }
        """;

    /// <summary>
    /// The Sun glows with limb darkening (the edge of its disk is dimmer than the center);
    /// a planet is lit by the Sun, with a faint ambient light so its night side is not black.
    /// </summary>
    public const string BodyFragment = """
        #version 330 core

        in vec3 vWorldPosition;
        in vec3 vNormal;

        uniform vec3 uColor;
        uniform vec3 uCameraPosition;
        uniform vec3 uSunPosition;
        uniform bool uEmissive;

        out vec4 fragColor;

        void main()
        {
            vec3 normal = normalize(vNormal);
            if (uEmissive)
            {
                float facing = max(dot(normal, normalize(uCameraPosition - vWorldPosition)), 0.0);
                fragColor = vec4(uColor * (0.4 + 0.6 * sqrt(facing)), 1.0);
            }
            else
            {
                float diffuse = max(dot(normal, normalize(uSunPosition - vWorldPosition)), 0.0);
                fragColor = vec4(uColor * (0.03 + 0.97 * diffuse), 1.0);
            }
        }
        """;
}
