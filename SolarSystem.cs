namespace EpsilonSolarSystem;

/// <summary>
/// The Sun, the eight planets and the Moon at their real sizes and distances, in kilometers, in the
/// frame of the ecliptic and equinox of J2000 (z toward the north pole of the ecliptic).
/// </summary>
sealed class SolarSystem
{
    public const double Au = 149_597_870.7;
    const double J2000 = 2451545.0;
    // The tilt of Earth's axis at J2000, which turns equatorial coordinates into ecliptic ones.
    const double Obliquity = 23.4392911;
    // The Moon's share of the mass of the Earth-Moon pair.
    const double MoonMassShare = 0.0121505856;

    public SolarSystem()
    {
        Sun = new("Sun", 695_700, 609.12, Pole(286.13, 63.87), new(SurfacePattern.Glow, (1f, 0.85f, 0.45f), (1f, 0.85f, 0.45f)));

        // Planets: Table 1 of https://ssd.jpl.nasa.gov/planets/approx_pos.html (1800 AD - 2050 AD), the row of
        // elements over the row of rates per century; the semi-major axis in au. Poles: IAU WGCCRE, J2000.
        var mercury = Planet("Mercury", 2439.7, 1407.6, Pole(281.01, 61.42), new(SurfacePattern.Rocky, (0.6f, 0.58f, 0.55f), (0.38f, 0.37f, 0.36f)),
            new(0.38709927, 0.20563593, 7.00497902, 252.25032350, 77.45779628, 48.33076593),
            new(0.00000037, 0.00001906, -0.00594749, 149472.67411175, 0.16047689, -0.12534081));
        var venus = Planet("Venus", 6051.8, -5832.5, Pole(272.76, 67.16), new(SurfacePattern.Bands, (0.92f, 0.84f, 0.64f), (0.84f, 0.74f, 0.54f)),
            new(0.72333566, 0.00677672, 3.39467605, 181.97909950, 131.60246718, 76.67984255),
            new(0.00000390, -0.00004107, -0.00078890, 58517.81538729, 0.00268329, -0.27769418));
        // This orbit is that of the Earth-Moon barycenter; Update moves Earth off it, opposite the Moon.
        Earth = Planet("Earth", 6371.0, 23.9345, Pole(0, 90), new(SurfacePattern.Continents, (0.1f, 0.25f, 0.6f), (0.3f, 0.45f, 0.2f), PolarCaps: 0.88f),
            new(1.00000261, 0.01671123, -0.00001531, 100.46457166, 102.93768193, 0.0),
            new(0.00000562, -0.00004392, -0.01294668, 35999.37244981, 0.32327364, 0.0));
        var mars = Planet("Mars", 3389.5, 24.6229, Pole(317.68, 52.89), new(SurfacePattern.Rocky, (0.78f, 0.42f, 0.22f), (0.52f, 0.27f, 0.16f), PolarCaps: 0.92f),
            new(1.52371034, 0.09339410, 1.84969142, -4.55343205, -23.94362959, 49.55953891),
            new(0.00001847, 0.00007882, -0.00813131, 19140.30268499, 0.44441088, -0.29257343));
        var jupiter = Planet("Jupiter", 69_911, 9.925, Pole(268.06, 64.50), new(SurfacePattern.Bands, (0.88f, 0.8f, 0.68f), (0.66f, 0.5f, 0.36f)),
            new(5.20288700, 0.04838624, 1.30439695, 34.39644051, 14.72847983, 100.47390909),
            new(-0.00011607, -0.00013253, -0.00183714, 3034.74612775, 0.21252668, 0.20469106));
        var saturn = Planet("Saturn", 58_232, 10.656, Pole(40.59, 83.54), new(SurfacePattern.Bands, (0.92f, 0.85f, 0.64f), (0.8f, 0.7f, 0.5f)),
            new(9.53667594, 0.05386179, 2.48599187, 49.95424423, 92.59887831, 113.66242448),
            new(-0.00125060, -0.00050991, 0.00193609, 1222.49362201, -0.41897216, -0.28867794));
        var uranus = Planet("Uranus", 25_362, -17.24, Pole(257.31, -15.18), new(SurfacePattern.Bands, (0.68f, 0.88f, 0.92f), (0.48f, 0.72f, 0.8f)),
            new(19.18916464, 0.04725744, 0.77263783, 313.23810451, 170.95427630, 74.01692503),
            new(-0.00196176, -0.00004397, -0.00242939, 428.48202785, 0.40805281, 0.04240589));
        var neptune = Planet("Neptune", 24_622, 16.11, Pole(299.36, 43.46), new(SurfacePattern.Bands, (0.32f, 0.48f, 0.92f), (0.24f, 0.36f, 0.8f)),
            new(30.06992276, 0.00859048, 1.77004347, -55.12002969, 44.96476227, 131.78422574),
            new(0.00026291, 0.00005105, 0.00035372, 218.45945325, -0.32241464, -0.00508664));

        // The Moon around Earth: mean elements from Meeus, Astronomical Algorithms, ch. 47, without the periodic
        // terms (off by up to a few degrees). It spins once per orbit, so it shows Earth the same side.
        Moon = new("Moon", 1737.4, 655.72, Pole(269.99, 66.54), new(SurfacePattern.Rocky, (0.62f, 0.62f, 0.6f), (0.36f, 0.36f, 0.38f)), Earth,
            new(new(384_400, 0.0549, 5.145396, 218.3164477, 83.3532465, 125.0445479),
                new(0, 0, 0, 481267.88123421, 4069.0137287, -1934.1362891)));

        // Parents before their satellites; the index is the number key that picks the body.
        Bodies = [Sun, mercury, venus, Earth, mars, jupiter, saturn, uranus, neptune, Moon];
    }

    public Body Sun { get; }
    public Body Earth { get; }
    public Body Moon { get; }
    public IReadOnlyList<Body> Bodies { get; }

    // The Julian date of 1970-01-01 00:00 UTC; the difference between UTC and the time scale of the
    // ephemerides (about a minute) does not matter here.
    const double UnixEpochJulianDate = 2440587.5;

    public static double JulianDate(DateTime utc) => UnixEpochJulianDate + (utc - DateTime.UnixEpoch).TotalDays;

    public static DateTime ToDateTime(double julianDate) => DateTime.UnixEpoch.AddDays(julianDate - UnixEpochJulianDate);

    /// <summary>Puts every body where it is at the given Julian date.</summary>
    public void Update(double julianDate)
    {
        foreach (var body in Bodies)
            body.Update(julianDate - J2000);

        var moonOffset = Moon.Position - Earth.Position;
        Earth.Position -= moonOffset * MoonMassShare;
        Moon.Position = Earth.Position + moonOffset;
    }

    Body Planet(string name, double radius, double rotationHours, Vector3<double> pole, Surface surface,
        OrbitalElements atJ2000, OrbitalElements perCentury) =>
        new(name, radius, rotationHours, pole, surface, Sun,
            new(atJ2000 with { SemiMajorAxis = atJ2000.SemiMajorAxis * Au },
                perCentury with { SemiMajorAxis = perCentury.SemiMajorAxis * Au }));

    /// <summary>
    /// A north pole (of a body, or of the Milky Way) given by right ascension and declination in degrees,
    /// turned from the equator to the ecliptic.
    /// </summary>
    public static Vector3<double> Pole(double rightAscension, double declination)
    {
        var alpha = double.DegreesToRadians(rightAscension);
        var delta = double.DegreesToRadians(declination);
        var equatorial = new Vector3<double>(Math.Cos(delta) * Math.Cos(alpha), Math.Cos(delta) * Math.Sin(alpha), Math.Sin(delta));
        return Quaternion<double>.RotationX(-double.DegreesToRadians(Obliquity)).Rotate(equatorial);
    }
}
