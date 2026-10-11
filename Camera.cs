namespace EpsilonSolarSystem;

/// <summary>
/// The camera, in double precision. It circles a body, flies freely moving along with that body, or goes over
/// to another body: at a simulated day per second a planet covers millions of kilometers a second, and a camera
/// left behind in space would lose it at once. Its turn is a yaw about the pole of the ecliptic after a pitch
/// above the ecliptic; in its own frame it looks along +x, with +y to the left and +z up.
/// </summary>
sealed class Camera(Body body)
{
    const double OrbitSeconds = 120;
    const double RadiansPerPixel = 0.003;
    // Going over to another body: first a turn toward it, a third of a second for each radian, then the way there.
    const double TurnSecondsPerRadian = 1.0 / 3;
    const double TravelSeconds = 1.5;
    // Short of straight up or down, where the yaw is lost.
    static readonly double MaxPitch = double.DegreesToRadians(89);
    // An orbiting camera starts, and ends a trip, looking down on its body from 20 degrees above it.
    static readonly double OrbitPitch = double.DegreesToRadians(-20);

    double yaw = Math.PI;
    double pitch = OrbitPitch;
    double distance = OrbitDistance(body);
    // The place of a free camera relative to the body it moves with.
    Vector3<double> offset;

    // A trip to Destination: whether the camera still turns, the orientation it turns from, how long the turn takes,
    // the time since the trip began, the distances from Destination at the start and at the end of the way,
    // and the pitch at the start of the way.
    bool turning;
    Quaternion<double> turnStart;
    double turnSeconds;
    double tripSeconds;
    double travelStart;
    double travelEnd;
    double travelPitch;

    public Body Focus { get; private set; } = body;
    public bool IsFree { get; private set; }
    /// <summary>The body the camera is on its way to, while it is.</summary>
    public Body? Destination { get; private set; }
    /// <summary>The factor of the flying speed, set with the wheel.</summary>
    public double SpeedFactor { get; private set; } = 1;

    /// <summary>
    /// While it turns toward its destination, the camera goes the shortest way from where it looked to where the
    /// destination is now: a Slerp, which may roll a little on the way. Otherwise it is set by the yaw and the pitch.
    /// </summary>
    public Quaternion<double> Orientation => turning
        ? Quaternion<double>.Slerp(turnStart, Facing(Destination!.Position - Eye), Ease(tripSeconds / turnSeconds))
        : Rotation(yaw, pitch);

    public Vector3<double> Forward => Orientation.Rotate(Vector3<double>.UnitX);
    public Vector3<double> Up => Orientation.Rotate(Vector3<double>.UnitZ);
    public Vector3<double> Eye => IsFree ? Focus.Position + offset : Focus.Position - Forward * distance;

    /// <summary>
    /// Goes over to the body: turns toward it where the camera is, then comes to it and circles it, the Sun from a
    /// distance that shows the inner planets, any other body from five of its radii. The distance changes by the
    /// same factor every moment, so a planet grows steadily from a dot to a globe however far away it was; on the
    /// way the camera rises or sinks to look down on the body as an orbiting camera does.
    /// </summary>
    public void GoTo(Body body)
    {
        var orientation = Orientation;
        offset = Eye - Focus.Position;
        IsFree = true;
        Destination = body;
        turning = true;
        turnStart = orientation;
        turnSeconds = TurnSecondsPerRadian * orientation.Angle(Facing(body.Position - Eye));
        tripSeconds = 0;
        travelEnd = OrbitDistance(body);
        // Already facing the body, the camera sets off at once.
        Update(0);
    }

    /// <summary>A drag of the mouse: turns a free camera, or moves an orbiting one around its body; it ends a trip.</summary>
    public void Turn(double pixelsRight, double pixelsDown)
    {
        Stop();
        yaw -= RadiansPerPixel * pixelsRight;
        pitch = Math.Clamp(pitch - RadiansPerPixel * pixelsDown, -MaxPitch, MaxPitch);
    }

    /// <summary>The wheel: zooms an orbiting camera, changes the speed of a free one; it ends a trip.</summary>
    public void Wheel(double steps)
    {
        Stop();
        if (IsFree)
            SpeedFactor *= Math.Pow(1.25, steps);
        else
            distance = Math.Max(distance * Math.Pow(1.2, -steps), 1.5 * Focus.Radius);
    }

    /// <summary>
    /// Flies along a direction of the camera's own frame (x forward, y left, z up) at the given speed in km/s;
    /// an orbiting camera becomes free where it is, and a trip ends.
    /// </summary>
    public void Fly(Vector3<double> direction, double speed, double seconds)
    {
        Stop();
        if (!IsFree)
        {
            offset = Eye - Focus.Position;
            IsFree = true;
        }
        offset += Orientation.Rotate(direction.Normalize()) * (speed * SpeedFactor * seconds);
    }

    /// <summary>Goes on with a trip; an orbiting camera goes around its body once in two minutes.</summary>
    public void Update(double seconds)
    {
        if (Destination is { } destination)
        {
            tripSeconds += seconds;
            if (turning && tripSeconds >= turnSeconds)
            {
                // Facing the destination: from here on the camera circles it, from where it is.
                (yaw, pitch) = Angles(destination.Position - Eye);
                travelStart = Eye.Distance(destination.Position);
                travelPitch = pitch;
                Focus = destination;
                IsFree = false;
                turning = false;
            }
            if (!turning)
            {
                var t = Ease(Math.Min((tripSeconds - turnSeconds) / TravelSeconds, 1));
                distance = travelStart * Math.Pow(travelEnd / travelStart, t);
                pitch = double.Lerp(travelPitch, OrbitPitch, t);
                if (t == 1)
                    Destination = null;
            }
        }
        if (!IsFree)
            yaw += 2 * Math.PI * seconds / OrbitSeconds;
    }

    // Ends a trip where the camera is. During the turn it is left free with its body, looking where it looks,
    // without the roll the Slerp may have added; on the way, it goes on circling the destination from there.
    void Stop()
    {
        if (Destination is null)
            return;
        if (turning)
            (yaw, pitch) = Angles(Forward);
        turning = false;
        Destination = null;
    }

    static double OrbitDistance(Body body) => body.Parent is null ? 3 * SolarSystem.Au : 5 * body.Radius;

    static Quaternion<double> Rotation(double yaw, double pitch) =>
        Quaternion<double>.FromEulerAngles(new(0, -pitch, yaw), EulerOrder.XYZ);

    // The yaw and the pitch that look along the direction.
    static (double Yaw, double Pitch) Angles(Vector3<double> direction) =>
        (Math.Atan2(direction.Y, direction.X), Math.Atan2(direction.Z, double.Hypot(direction.X, direction.Y)));

    static Quaternion<double> Facing(Vector3<double> direction)
    {
        var (yaw, pitch) = Angles(direction);
        return Rotation(yaw, pitch);
    }

    // From 0 to 1, starting and stopping gently.
    static double Ease(double t) => t * t * (3 - 2 * t);
}
