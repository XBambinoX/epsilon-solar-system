namespace EpsilonSolarSystem;

/// <summary>GLSL sources.</summary>
static class Shaders
{
    /// <summary>
    /// Value noise for the painting of the sky: hash, noise and fractalNoise.
    /// </summary>
    const string Noise = """
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
        """;

    /// <summary>A unit sphere placed by uModel; its point is also its normal, and it brings its place on the map.</summary>
    public const string BodyVertex = """
        #version 330 core

        layout(location = 0) in vec3 aPosition;
        layout(location = 1) in vec2 aMap;

        uniform mat4 uModel;
        uniform mat3 uNormalMatrix;
        uniform mat4 uViewProjection;

        out vec3 vWorldPosition;
        out vec3 vNormal;
        out vec2 vMap;

        void main()
        {
            vec4 world = uModel * vec4(aPosition, 1.0);
            vWorldPosition = world.xyz;
            vNormal = uNormalMatrix * aPosition;
            vMap = aMap;
            gl_Position = uViewProjection * world;
        }
        """;

    /// <summary>
    /// A body with its map. The Sun glows, with limb darkening (the edge of its disk is dimmer than the
    /// center). A planet is lit by the Sun plus an ambient light: dim, so the night side is not black, or
    /// full for a far body drawn as a marker.
    /// </summary>
    public const string BodyFragment = """
        #version 330 core

        in vec3 vWorldPosition;
        in vec3 vNormal;
        in vec2 vMap;

        uniform sampler2D uSurface;
        uniform bool uGlow;
        uniform float uAmbient;
        uniform vec3 uCameraPosition;
        uniform vec3 uSunPosition;

        out vec4 fragColor;

        void main()
        {
            vec3 color = texture(uSurface, vMap).rgb;
            vec3 normal = normalize(vNormal);
            float facing = max(dot(normal, normalize(uCameraPosition - vWorldPosition)), 0.0);
            if (uGlow)
            {
                fragColor = vec4(color * (0.4 + 0.6 * sqrt(facing)), 1.0);
                return;
            }

            float diffuse = max(dot(normal, normalize(uSunPosition - vWorldPosition)), 0.0);
            // Light adds up in linear units, and the power 1/2.2 turns it into the brightness of the screen,
            // which keeps the half-lit part bright. The ambient light comes a little more from the camera,
            // so a night side still shows its round shape.
            float light = diffuse + uAmbient * (0.4 + 0.6 * facing);
            fragColor = vec4(color * pow(min(light, 1.0), 1.0 / 2.2), 1.0);
        }
        """;

    /// <summary>
    /// A star at infinity: its direction with w = 0, so the camera's position does not matter, only its turn.
    /// Brighter stars are larger, the brightest get room for their spikes; the tint comes from a hash of the
    /// direction: mostly blue-white, as in an infrared survey of the sky, some white, a few orange.
    /// </summary>
    public const string StarVertex = """
        #version 330 core

        layout(location = 0) in vec4 aStar; // direction, brightness

        uniform mat4 uViewProjection;

        out vec3 vColor;
        out float vBrightness;

        void main()
        {
            gl_Position = uViewProjection * vec4(aStar.xyz, 0.0);
            float brightness = aStar.w;
            gl_PointSize = brightness > 0.9 ? 17.0 : 1.5 + 3.0 * brightness * brightness;
            float tint = fract(sin(dot(aStar.xyz, vec3(12.9898, 78.233, 37.719))) * 43758.5453);
            vec3 color = tint < 0.6 ? vec3(0.65, 0.9, 1.0) : tint < 0.9 ? vec3(1.0) : vec3(1.0, 0.75, 0.5);
            vColor = brightness * color;
            vBrightness = brightness;
        }
        """;

    /// <summary>
    /// A round point, soft at the edge; a bright star gets a small core, a halo and four thin spikes, as the
    /// optics of a telescope draw it. The stars are added onto the sky.
    /// </summary>
    public const string StarFragment = """
        #version 330 core

        in vec3 vColor;
        in float vBrightness;

        out vec4 fragColor;

        void main()
        {
            vec2 p = 2.0 * gl_PointCoord - 1.0;
            float r = length(p);
            if (vBrightness <= 0.9)
            {
                fragColor = vec4(vColor * (1.0 - smoothstep(0.3, 1.0, r)), 1.0);
                return;
            }
            float core = exp(-60.0 * r * r);
            float halo = 0.3 * exp(-10.0 * r * r);
            float spikes = 0.5 * (exp(-25.0 * abs(p.y)) * (1.0 - abs(p.x)) + exp(-25.0 * abs(p.x)) * (1.0 - abs(p.y)));
            fragColor = vec4(vColor * (core + halo + spikes), 1.0);
        }
        """;

    /// <summary>A triangle that covers the whole screen, given in screen coordinates from -1 to 1 and beyond.</summary>
    public const string ScreenVertex = """
        #version 330 core

        layout(location = 0) in vec3 aPosition;

        out vec2 vScreen;

        void main()
        {
            vScreen = aPosition.xy;
            gl_Position = vec4(aPosition.xy, 0.0, 1.0);
        }
        """;

    /// <summary>
    /// Paints one face of the sky's cube map: a dark violet haze everywhere, the Milky Way as a dim band broken
    /// up by dust, and patches of glowing gas, violet at the thin edges, pink and then pale pink in the
    /// densest knots. All of it is noise of the direction, so the faces meet without seams.
    /// </summary>
    public const string SkyPaintFragment = $$"""
        #version 330 core

        in vec2 vScreen;

        uniform int uFace;
        uniform vec3 uGalacticPole;

        out vec4 fragColor;

        {{Noise}}

        // The direction of a point of a cube map face, by the layout of the faces in the OpenGL specification.
        vec3 direction(int face, vec2 p)
        {
            if (face == 0) return vec3(1.0, -p.y, -p.x);
            if (face == 1) return vec3(-1.0, -p.y, p.x);
            if (face == 2) return vec3(p.x, 1.0, p.y);
            if (face == 3) return vec3(p.x, -1.0, -p.y);
            if (face == 4) return vec3(p.x, -p.y, 1.0);
            return vec3(-p.x, -p.y, -1.0);
        }

        void main()
        {
            vec3 d = normalize(direction(uFace, vScreen));

            vec3 color = vec3(0.03, 0.012, 0.035) * (0.3 + 1.4 * fractalNoise(2.0 * d));

            float latitude = dot(d, uGalacticPole);
            float band = exp(-latitude * latitude / 0.02) * smoothstep(0.3, 0.7, fractalNoise(6.0 * d + 5.0));
            color += band * vec3(0.09, 0.07, 0.1);

            // Wisps: noise whose point is pushed around by more noise.
            float patches = smoothstep(0.42, 0.7, fractalNoise(1.3 * d + vec3(3.1, 7.7, 1.3)));
            vec3 q = 3.0 * d;
            vec3 warp = vec3(fractalNoise(q + 11.0), fractalNoise(q + 23.0), fractalNoise(q + 37.0));
            float density = patches * smoothstep(0.35, 0.85, fractalNoise(5.0 * d + 3.0 * warp));
            vec3 gas = mix(vec3(0.4, 0.08, 0.55), vec3(0.95, 0.3, 0.7), smoothstep(0.1, 0.5, density));
            gas = mix(gas, vec3(1.0, 0.75, 0.95), 0.6 * smoothstep(0.55, 0.9, density));
            color += 0.42 * density * gas;

            fragColor = vec4(color, 1.0);
        }
        """;

    /// <summary>The sky behind a pixel: the cube map in the direction the pixel looks.</summary>
    public const string SkyFragment = """
        #version 330 core

        in vec2 vScreen;

        uniform mat4 uInverseViewProjection;
        uniform samplerCube uSky;

        out vec4 fragColor;

        void main()
        {
            // The point of the near plane on the pixel, for a camera at the origin: its direction is the pixel's.
            vec4 near = uInverseViewProjection * vec4(vScreen, 1.0, 1.0);
            fragColor = vec4(texture(uSky, near.xyz / near.w).rgb, 1.0);
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
