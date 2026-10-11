namespace EpsilonSolarSystem;

/// <summary>
/// A body at its real size, the radius in kilometers, turned as the IAU defines it: the frame of its
/// equator (z on the north pole, x where the prime meridian is at W = 0), then the prime meridian at the
/// angle W, in degrees, which grows by a constant number of degrees a day (less than zero for a retrograde
/// spin). It wears the map in <see cref="Texture"/>; its orbit line has the color <see cref="Color"/>.
/// </summary>
sealed class Body(string name, double radius, Quaternion<double> frame, double primeMeridian, double degreesPerDay,
    string texture, Vector3<float> color, Body? parent = null, Orbit? orbit = null)
{
    public string Name => name;
    public double Radius => radius;
    public string Texture => texture;
    public Vector3<float> Color => color;
    public Body? Parent => parent;
    public Orbit? Orbit => orbit;

    /// <summary>The center in kilometers, the Sun at the origin, z toward the north pole of the ecliptic.</summary>
    public Vector3<double> Position { get; set; }

    /// <summary>The velocity relative to the parent, in km/s; zero for the Sun.</summary>
    public Vector3<double> Velocity { get; private set; }

    /// <summary>The turn from the sphere mesh to the body; the mesh has longitude 0 on +x.</summary>
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
            var (offset, velocity, orientation) = orbit.At(centuries);
            Position = parent.Position + offset;
            Velocity = velocity;
            OrbitElements = orbit.ElementsAt(centuries);
            OrbitOrientation = orientation;
        }
        var meridian = Math.IEEERemainder(primeMeridian + degreesPerDay * daysSinceJ2000, 360);
        Rotation = frame * Quaternion<double>.RotationZ(double.DegreesToRadians(meridian));
    }
}
