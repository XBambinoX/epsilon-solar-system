namespace EpsilonSolarSystem;

/// <summary>GLSL sources.</summary>
static class Shaders
{
    /// <summary>A unit sphere placed by uModel; its vertex is also its normal and the point of the surface pattern.</summary>
    public const string BodyVertex = """
        #version 330 core

        layout(location = 0) in vec3 aPosition;

        uniform mat4 uModel;
        uniform mat3 uNormalMatrix;
        uniform mat4 uViewProjection;

        out vec3 vObjectPosition;
        out vec3 vWorldPosition;
        out vec3 vNormal;

        void main()
        {
            vObjectPosition = aPosition;
            vec4 world = uModel * vec4(aPosition, 1.0);
            vWorldPosition = world.xyz;
            vNormal = uNormalMatrix * aPosition;
            gl_Position = uViewProjection * world;
        }
        """;

    /// <summary>
    /// The Sun glows with limb darkening (the edge of its disk is dimmer than the center). A planet gets a
    /// pattern fixed to its surface, so the spin and the tilt of its axis show, and is lit by the Sun plus an
    /// ambient light: faint, so the night side is not black, or full for a far body drawn as a marker.
    /// </summary>
    public const string BodyFragment = """
        #version 330 core

        in vec3 vObjectPosition;
        in vec3 vWorldPosition;
        in vec3 vNormal;

        uniform int uPattern; // 0 glow, 1 rocky, 2 continents, 3 bands, as SurfacePattern
        uniform vec3 uColor;
        uniform vec3 uSecondColor;
        uniform float uPolarCaps;
        uniform float uSeed;
        uniform float uAmbient;
        uniform vec3 uCameraPosition;
        uniform vec3 uSunPosition;

        out vec4 fragColor;

        float hash(vec3 p)
        {
            p = fract(p * 0.3183099 + 0.1);
            p *= 17.0;
            return fract(p.x * p.y * p.z * (p.x + p.y + p.z));
        }

        // Value noise: random values at the corners of the unit cubes, smoothly blended inside.
        float noise(vec3 p)
        {
            vec3 i = floor(p);
            vec3 f = fract(p);
            f = f * f * (3.0 - 2.0 * f);
            return mix(
                mix(mix(hash(i), hash(i + vec3(1, 0, 0)), f.x),
                    mix(hash(i + vec3(0, 1, 0)), hash(i + vec3(1, 1, 0)), f.x), f.y),
                mix(mix(hash(i + vec3(0, 0, 1)), hash(i + vec3(1, 0, 1)), f.x),
                    mix(hash(i + vec3(0, 1, 1)), hash(i + vec3(1, 1, 1)), f.x), f.y),
                f.z);
        }

        // Five octaves of noise, each twice as fine and half as strong: from 0 to about 1, mostly near 0.5.
        float fractalNoise(vec3 p)
        {
            float sum = 0.0;
            float amplitude = 0.5;
            for (int octave = 0; octave < 5; octave++)
            {
                sum += amplitude * noise(p);
                p *= 2.0;
                amplitude *= 0.5;
            }
            return sum;
        }

        vec3 surface(vec3 p)
        {
            vec3 q = 2.5 * p + uSeed;
            if (uPattern == 1)
                return mix(uColor, uSecondColor, smoothstep(0.35, 0.65, fractalNoise(q)));
            if (uPattern == 2)
                return fractalNoise(q) > 0.47 ? uSecondColor : uColor;
            // Bands along the latitudes, made wavy by the noise.
            return mix(uColor, uSecondColor, 0.5 + 0.5 * sin(14.0 * p.z + 3.0 * fractalNoise(1.5 * q)));
        }

        void main()
        {
            vec3 normal = normalize(vNormal);
            if (uPattern == 0)
            {
                float facing = max(dot(normal, normalize(uCameraPosition - vWorldPosition)), 0.0);
                fragColor = vec4(uColor * (0.4 + 0.6 * sqrt(facing)), 1.0);
                return;
            }

            vec3 color = surface(vObjectPosition);
            if (uPolarCaps > 0.0 && abs(vObjectPosition.z) > uPolarCaps + 0.06 * (fractalNoise(8.0 * vObjectPosition) - 0.5))
                color = vec3(0.92, 0.94, 0.96);
            float diffuse = max(dot(normal, normalize(uSunPosition - vWorldPosition)), 0.0);
            fragColor = vec4(color * mix(diffuse, 1.0, uAmbient), 1.0);
        }
        """;

    /// <summary>A line placed by uModel, in one color.</summary>
    public const string LineVertex = """
        #version 330 core

        layout(location = 0) in vec3 aPosition;

        uniform mat4 uModel;
        uniform mat4 uViewProjection;

        void main()
        {
            gl_Position = uViewProjection * uModel * vec4(aPosition, 1.0);
        }
        """;

    public const string LineFragment = """
        #version 330 core

        uniform vec3 uColor;

        out vec4 fragColor;

        void main()
        {
            fragColor = vec4(uColor, 1.0);
        }
        """;
}
