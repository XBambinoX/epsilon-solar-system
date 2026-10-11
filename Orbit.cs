namespace EpsilonSolarSystem;

/// <summary>
/// The six Keplerian elements in the columns of the JPL table: semi-major axis (km), eccentricity,
/// inclination, mean longitude, longitude of perihelion and longitude of the ascending node (degrees),
/// all with respect to the ecliptic.
/// </summary>
readonly record struct OrbitalElements(
    double SemiMajorAxis, double Eccentricity, double Inclination,
    double MeanLongitude, double PerihelionLongitude, double NodeLongitude);

/// <summary>A Keplerian orbit around the parent body: the elements at J2000 and their rates per Julian century.</summary>
sealed class Orbit(OrbitalElements atJ2000, OrbitalElements perCentury)
{
    const double SecondsPerCentury = 36525 * 86400.0;

    // The variables of the expressions below: the semi-major axis a, the eccentricity e, the mean anomaly M (which
    // grows evenly with time) and the eccentric anomaly E (the angle on the circle around the ellipse).
    static readonly Variable A = new("a"), Eccentricity = new("e"), M = new("M"), E = new("E");

    /// <summary>
    /// Kepler's equation M = E - e sin E, written to be zero at the answer, as an Epsilon expression. (Parsed from
    /// text, an "e" would be Euler's number, so it is built with the C# operators.)
    /// </summary>
    public static Expr Kepler { get; } = E - Eccentricity * new Sin(E) - M;

    /// <summary>The derivative of <see cref="Kepler"/> in E, found by Epsilon symbolically: 1 - e cos E.</summary>
    public static Expr KeplerSlope { get; } = Kepler.Differentiate("E");

    // Where Newton's method starts: E = M + e sin M, close to the answer for a small eccentricity.
    static readonly Expr FirstGuess = M + Eccentricity * new Sin(M);

    /// <summary>
    /// How fast E grows with M: Kepler's equation holds all along the orbit, so by implicit differentiation
    /// dE/dM = -(dK/dM) / (dK/dE), both derivatives found symbolically.
    /// </summary>
    public static Expr AnomalyRate { get; } = (-Kepler.Differentiate("M") / KeplerSlope).Simplify();

    /// <summary>The place in the plane of the orbit at the eccentric anomaly E, the parent at the origin and the perihelion on +x.</summary>
    public static Expr PlaneX { get; } = A * (new Cos(E) - Eccentricity);
    public static Expr PlaneY { get; } = A * new Sqrt(1 - Eccentricity * Eccentricity) * new Sin(E);

    /// <summary>The velocity in the plane of the orbit per radian of mean anomaly, by the chain rule: d/dE * dE/dM.</summary>
    public static Expr VelocityX { get; } = (PlaneX.Differentiate("E") * AnomalyRate).Simplify();
    public static Expr VelocityY { get; } = (PlaneY.Differentiate("E") * AnomalyRate).Simplify();

    public OrbitalElements ElementsAt(double centuries) => new(
        atJ2000.SemiMajorAxis + perCentury.SemiMajorAxis * centuries,
        atJ2000.Eccentricity + perCentury.Eccentricity * centuries,
        atJ2000.Inclination + perCentury.Inclination * centuries,
        atJ2000.MeanLongitude + perCentury.MeanLongitude * centuries,
        atJ2000.PerihelionLongitude + perCentury.PerihelionLongitude * centuries,
        atJ2000.NodeLongitude + perCentury.NodeLongitude * centuries);

    /// <summary>
    /// The position relative to the parent, the velocity relative to it in km/s, and the rotation that takes the
    /// plane of the orbit (the perihelion on +x, the motion counterclockwise around +z) to its place in the ecliptic.
    /// The slow turning of the orbit itself is left out of the velocity.
    /// </summary>
    public (Vector3<double> Position, Vector3<double> Velocity, Quaternion<double> Orientation) At(double centuries)
    {
        var (a, e, inclination, meanLongitude, perihelion, node) = ElementsAt(centuries);
        var meanAnomaly = Math.IEEERemainder(double.DegreesToRadians(meanLongitude - perihelion), 2 * Math.PI);
        var values = new Dictionary<string, double> { ["a"] = a, ["e"] = e, ["M"] = meanAnomaly };
        values["E"] = EccentricAnomaly(values);
        var inPlane = new Vector3<double>(PlaneX.Evaluate(values), PlaneY.Evaluate(values), 0);
        // The mean anomaly grows by the rate of the mean longitude less that of the perihelion.
        var radiansPerSecond = double.DegreesToRadians(perCentury.MeanLongitude - perCentury.PerihelionLongitude) / SecondsPerCentury;
        var velocity = new Vector3<double>(VelocityX.Evaluate(values), VelocityY.Evaluate(values), 0) * radiansPerSecond;

        // Turn the perihelion away from the node, tilt the plane about the line of nodes, turn the node into place.
        var orientation =
            Quaternion<double>.RotationZ(double.DegreesToRadians(node)) *
            Quaternion<double>.RotationX(double.DegreesToRadians(inclination)) *
            Quaternion<double>.RotationZ(double.DegreesToRadians(perihelion - node));
        return (orientation.Rotate(inPlane), orientation.Rotate(velocity), orientation);
    }

    // Solves Kepler's equation for E, given e and M, by Newton's method with the symbolic derivative; a few steps
    // reach full precision for e < 0.3.
    static double EccentricAnomaly(Dictionary<string, double> values)
    {
        values["E"] = FirstGuess.Evaluate(values);
        for (var i = 0; i < 6; i++)
            values["E"] -= Kepler.Evaluate(values) / KeplerSlope.Evaluate(values);
        return values["E"];
    }
}
