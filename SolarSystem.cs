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
        // Frames, prime meridians and spins: the IAU WGCCRE reports (2009; Mercury 2015), J2000, without their
        // small periodic terms. With them the maps sit right: Earth's day side is where the Sun is at the
        // current time, and the Moon turns its near side to Earth.
        Sun = new("Sun", 695_700, Frame(286.13, 63.87), 84.176, 14.1844, "2k_sun.jpg", (1f, 0.85f, 0.45f));

        // Orbits: Table 1 of https://ssd.jpl.nasa.gov/planets/approx_pos.html (1800 AD - 2050 AD), the row of
        // elements over the row of rates per century; the semi-major axis in au.
        var mercury = Planet("Mercury", 2439.7, Frame(281.0103, 61.4155), 329.5988, 6.1385108, "2k_mercury.jpg", (0.6f, 0.58f, 0.55f),
            new(0.38709927, 0.20563593, 7.00497902, 252.25032350, 77.45779628, 48.33076593),
            new(0.00000037, 0.00001906, -0.00594749, 149472.67411175, 0.16047689, -0.12534081));
        var venus = Planet("Venus", 6051.8, Frame(272.76, 67.16), 160.20, -1.4813688, "2k_venus_atmosphere.jpg", (0.92f, 0.84f, 0.64f),
            new(0.72333566, 0.00677672, 3.39467605, 181.97909950, 131.60246718, 76.67984255),
            new(0.00000390, -0.00004107, -0.00078890, 58517.81538729, 0.00268329, -0.27769418));
        // This orbit is that of the Earth-Moon barycenter; Update moves Earth off it, opposite the Moon.
        Earth = Planet("Earth", 6371.0, Frame(0, 90), 190.147, 360.9856235, "2k_earth_daymap.jpg", (0.3f, 0.5f, 0.9f),
            new(1.00000261, 0.01671123, -0.00001531, 100.46457166, 102.93768193, 0.0),
            new(0.00000562, -0.00004392, -0.01294668, 35999.37244981, 0.32327364, 0.0));
        var mars = Planet("Mars", 3389.5, Frame(317.68143, 52.88650), 176.630, 350.89198226, "2k_mars.jpg", (0.78f, 0.42f, 0.22f),
            new(1.52371034, 0.09339410, 1.84969142, -4.55343205, -23.94362959, 49.55953891),
            new(0.00001847, 0.00007882, -0.00813131, 19140.30268499, 0.44441088, -0.29257343));
        var jupiter = Planet("Jupiter", 69_911, Frame(268.056595, 64.495303), 284.95, 870.5360000, "2k_jupiter.jpg", (0.88f, 0.8f, 0.68f),
            new(5.20288700, 0.04838624, 1.30439695, 34.39644051, 14.72847983, 100.47390909),
            new(-0.00011607, -0.00013253, -0.00183714, 3034.74612775, 0.21252668, 0.20469106));
        var saturn = Planet("Saturn", 58_232, Frame(40.589, 83.537), 38.90, 810.7939024, "2k_saturn.jpg", (0.92f, 0.85f, 0.64f),
            new(9.53667594, 0.05386179, 2.48599187, 49.95424423, 92.59887831, 113.66242448),
            new(-0.00125060, -0.00050991, 0.00193609, 1222.49362201, -0.41897216, -0.28867794));
        var uranus = Planet("Uranus", 25_362, Frame(257.311, -15.175), 203.81, -501.1600928, "2k_uranus.jpg", (0.6f, 0.85f, 0.9f),
            new(19.18916464, 0.04725744, 0.77263783, 313.23810451, 170.95427630, 74.01692503),
            new(-0.00196176, -0.00004397, -0.00242939, 428.48202785, 0.40805281, 0.04240589));
        var neptune = Planet("Neptune", 24_622, Frame(299.36, 43.46), 253.18, 536.3128492, "2k_neptune.jpg", (0.35f, 0.5f, 0.95f),
            new(30.06992276, 0.00859048, 1.77004347, -55.12002969, 44.96476227, 131.78422574),
            new(0.00026291, 0.00005105, 0.00035372, 218.45945325, -0.32241464, -0.00508664));

        // The Moon around Earth: mean elements from Meeus, Astronomical Algorithms, ch. 47, without the periodic
        // terms (off by up to a few degrees). It spins once per orbit, so it shows Earth the same side.
        Moon = new("Moon", 1737.4, Frame(269.9949, 66.5392), 38.3213, 13.17635815, "2k_moon.jpg", (0.62f, 0.62f, 0.6f), Earth,
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

    Body Planet(string name, double radius, Quaternion<double> frame, double primeMeridian, double degreesPerDay,
        string texture, Vector3<float> color, OrbitalElements atJ2000, OrbitalElements perCentury) =>
        new(name, radius, frame, primeMeridian, degreesPerDay, texture, color, Sun,
            new(atJ2000 with { SemiMajorAxis = atJ2000.SemiMajorAxis * Au },
                perCentury with { SemiMajorAxis = perCentury.SemiMajorAxis * Au }));

    /// <summary>
    /// The frame of a body's equator, by the IAU: z on its north pole, given by right ascension and declination
    /// in degrees, and x on the ascending node of its equator on Earth's equator, at right ascension + 90
    /// degrees, where the prime meridian is at W = 0. Turned from Earth's equator to the ecliptic.
    /// </summary>
    static Quaternion<double> Frame(double rightAscension, double declination) =>
        Quaternion<double>.RotationX(-double.DegreesToRadians(Obliquity)) *
        Quaternion<double>.RotationZ(double.DegreesToRadians(rightAscension + 90)) *
        Quaternion<double>.RotationX(double.DegreesToRadians(90 - declination));

    /// <summary>
    /// A north pole (of a body, or of the Milky Way) given by right ascension and declination in degrees,
    /// turned from the equator to the ecliptic.
    /// </summary>
    public static Vector3<double> Pole(double rightAscension, double declination) =>
        Frame(rightAscension, declination).Rotate(Vector3<double>.UnitZ);
}
