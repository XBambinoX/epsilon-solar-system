namespace EpsilonSolarSystem;

/// <summary>What the shader draws on a body; the numbers match uPattern in <see cref="Shaders.BodyFragment"/>.</summary>
enum SurfacePattern { Glow, Rocky, Continents, Bands }

/// <summary>
/// The look of a body: a pattern in two colors, and white polar caps from the given height above
/// the equator of the unit sphere (0 for none).
/// </summary>
sealed record Surface(SurfacePattern Pattern, Vector3<float> Color, Vector3<float> SecondColor, float PolarCaps = 0);

/// <summary>
/// A body at its real size: the radius in kilometers, the sidereal rotation period in hours (negative
/// for a retrograde spin about the north pole), the north pole as a unit vector of the ecliptic frame.
/// </summary>
sealed class Body(string name, double radius, double rotationHours, Vector3<double> pole, Surface surface,
    Body? parent = null, Orbit? orbit = null)
{
    // Turns the pole of the sphere mesh (+z) to the body's north pole; the spin then happens about the mesh's z.
    readonly Quaternion<double> tilt = Quaternion<double>.FromToRotation(Vector3<double>.UnitZ, pole);

    public string Name => name;
    public double Radius => radius;
    public Surface Surface => surface;
    public Body? Parent => parent;
    public Orbit? Orbit => orbit;

    /// <summary>The center in kilometers, the Sun at the origin, z toward the north pole of the ecliptic.</summary>
    public Vector3<double> Position { get; set; }

    /// <summary>The turn from the mesh to the body: the tilt of the axis after the spin of the day.</summary>
    public Quaternion<double> Rotation { get; private set; } = Quaternion<double>.Identity;

    /// <summary>The elements of the orbit now; their size and shape draw the orbit line.</summary>
    public OrbitalElements OrbitElements { get; private set; }

    /// <summary>The turn from the plane of the orbit to the ecliptic, as <see cref="Orbit.At"/> gives it.</summary>
    public Quaternion<double> OrbitOrientation { get; private set; } = Quaternion<double>.Identity;

    /// <summary>Moves the body to its place at the given time; the parent must be updated first.</summary>
    public void Update(double daysSinceJ2000)
    {
        if (orbit is not null && parent is not null)
        {
            var centuries = daysSinceJ2000 / 36525;
            var (offset, orientation) = orbit.At(centuries);
            Position = parent.Position + offset;
            OrbitElements = orbit.ElementsAt(centuries);
            OrbitOrientation = orientation;
        }
        var spin = 2 * Math.PI * Math.IEEERemainder(24 * daysSinceJ2000 / rotationHours, 1);
        Rotation = tilt * Quaternion<double>.RotationZ(spin);
    }
}
