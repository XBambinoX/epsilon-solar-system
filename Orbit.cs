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
    public OrbitalElements ElementsAt(double centuries) => new(
        atJ2000.SemiMajorAxis + perCentury.SemiMajorAxis * centuries,
        atJ2000.Eccentricity + perCentury.Eccentricity * centuries,
        atJ2000.Inclination + perCentury.Inclination * centuries,
        atJ2000.MeanLongitude + perCentury.MeanLongitude * centuries,
        atJ2000.PerihelionLongitude + perCentury.PerihelionLongitude * centuries,
        atJ2000.NodeLongitude + perCentury.NodeLongitude * centuries);

    /// <summary>
    /// The position relative to the parent, and the rotation that takes the plane of the orbit
    /// (the perihelion on +x, the motion counterclockwise around +z) to its place in the ecliptic.
    /// </summary>
    public (Vector3<double> Position, Quaternion<double> Orientation) At(double centuries)
    {
        var (a, e, inclination, meanLongitude, perihelion, node) = ElementsAt(centuries);
        var eccentricAnomaly = EccentricAnomaly(double.DegreesToRadians(meanLongitude - perihelion), e);
        var inPlane = new Vector3<double>(
            a * (Math.Cos(eccentricAnomaly) - e),
            a * Math.Sqrt(1 - e * e) * Math.Sin(eccentricAnomaly),
            0);

        // Turn the perihelion away from the node, tilt the plane about the line of nodes, turn the node into place.
        var orientation =
            Quaternion<double>.RotationZ(double.DegreesToRadians(node)) *
            Quaternion<double>.RotationX(double.DegreesToRadians(inclination)) *
            Quaternion<double>.RotationZ(double.DegreesToRadians(perihelion - node));
        return (orientation.Rotate(inPlane), orientation);
    }

    /// <summary>
    /// Kepler's equation M = E - e sin E, written to be zero at the answer, as an Epsilon expression: the eccentric
    /// anomaly E, the eccentricity e and the mean anomaly M are its variables. (Parsed from text, an "e" would be
    /// Euler's number, so it is built with the C# operators.)
    /// </summary>
    public static Expr Kepler { get; } = new Variable("E") - new Variable("e") * new Sin(new Variable("E")) - new Variable("M");

    /// <summary>The derivative of <see cref="Kepler"/> in E, found by Epsilon symbolically: 1 - e cos E.</summary>
    public static Expr KeplerSlope { get; } = Kepler.Differentiate("E");

    // Solves Kepler's equation by Newton's method with the symbolic derivative; a few steps reach full precision
    // for e < 0.3.
    static double EccentricAnomaly(double meanAnomaly, double e)
    {
        meanAnomaly = Math.IEEERemainder(meanAnomaly, 2 * Math.PI);
        var values = new Dictionary<string, double> { ["E"] = meanAnomaly + e * Math.Sin(meanAnomaly), ["e"] = e, ["M"] = meanAnomaly };
        for (var i = 0; i < 6; i++)
            values["E"] -= Kepler.Evaluate(values) / KeplerSlope.Evaluate(values);
        return values["E"];
    }
}
