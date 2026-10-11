namespace EpsilonSolarSystem;

/// <summary>
/// The camera, in double precision. It either circles a body or flies freely, moving along with that body:
/// at a simulated day per second a planet covers millions of kilometers a second, and a camera left behind
/// in space would lose it at once. Its turn is a yaw about the pole of the ecliptic after a pitch above the
/// ecliptic; in its own frame it looks along +x, with +y to the left and +z up.
/// </summary>
sealed class Camera
{
    const double OrbitSeconds = 120;
    const double RadiansPerPixel = 0.003;
    // Short of straight up or down, where the yaw is lost.
    static readonly double MaxPitch = double.DegreesToRadians(89);

    double yaw = Math.PI;
    double pitch = double.DegreesToRadians(-20);
    double distance;
    // The place of a free camera relative to the body it moves with.
    Vector3<double> offset;

    public Camera(Body body) => Orbit(body);

    public Body Focus { get; private set; } = null!;
    public bool IsFree { get; private set; }
    /// <summary>The factor of the flying speed, set with the wheel.</summary>
    public double SpeedFactor { get; private set; } = 1;

    public Quaternion<double> Orientation => Quaternion<double>.FromEulerAngles(new(0, -pitch, yaw), EulerOrder.XYZ);
    public Vector3<double> Forward => Orientation.Rotate(Vector3<double>.UnitX);
    public Vector3<double> Up => Orientation.Rotate(Vector3<double>.UnitZ);
    public Vector3<double> Eye => IsFree ? Focus.Position + offset : Focus.Position - Forward * distance;

    /// <summary>Circles the body, the Sun from a distance that shows the inner planets, any other body from five of its radii.</summary>
    public void Orbit(Body body)
    {
        Focus = body;
        IsFree = false;
        distance = body.Parent is null ? 3 * SolarSystem.Au : 5 * body.Radius;
    }

    /// <summary>A drag of the mouse: turns a free camera, or moves an orbiting one around its body.</summary>
    public void Turn(double pixelsRight, double pixelsDown)
    {
        yaw -= RadiansPerPixel * pixelsRight;
        pitch = Math.Clamp(pitch - RadiansPerPixel * pixelsDown, -MaxPitch, MaxPitch);
    }

    /// <summary>The wheel: zooms an orbiting camera, changes the speed of a free one.</summary>
    public void Wheel(double steps)
    {
        if (IsFree)
            SpeedFactor *= Math.Pow(1.25, steps);
        else
            distance = Math.Max(distance * Math.Pow(1.2, -steps), 1.5 * Focus.Radius);
    }

    /// <summary>
    /// Flies along a direction of the camera's own frame (x forward, y left, z up) at the given speed in km/s;
    /// an orbiting camera becomes free where it is.
    /// </summary>
    public void Fly(Vector3<double> direction, double speed, double seconds)
    {
        if (!IsFree)
        {
            offset = Eye - Focus.Position;
            IsFree = true;
        }
        offset += Orientation.Rotate(direction.Normalize()) * (speed * SpeedFactor * seconds);
    }

    /// <summary>An orbiting camera goes around its body once in two minutes.</summary>
    public void Update(double seconds)
    {
        if (!IsFree)
            yaw += 2 * Math.PI * seconds / OrbitSeconds;
    }
}
