namespace EpsilonSolarSystem;

/// <summary>
/// Made-up stars at infinity: a direction and a brightness each. Half are spread over the whole sky,
/// the rest crowd along the plane of the Milky Way, which lies where it does in the real sky.
/// </summary>
static class StarField
{
    /// <summary>The north pole of the galaxy (J2000) in the frame of the ecliptic.</summary>
    public static Vector3<double> GalacticPole { get; } = SolarSystem.Pole(192.85948, 27.12825);

    // How far the Milky Way stars stray from its plane: the standard deviation of their latitude.
    static readonly double BandWidth = double.DegreesToRadians(7);

    public static Mesh Create(GL gl, int count = 50_000)
    {
        // A fixed seed: the same sky at every start.
        var random = new Random(2026);
        // Turns the frame of the galaxy (its plane as the xy plane) into the frame of the ecliptic.
        var galacticToEcliptic = Quaternion<double>.FromToRotation(Vector3<double>.UnitZ, GalacticPole);

        var stars = new Vector4<float>[count];
        for (var i = 0; i < count; i++)
        {
            Vector3<double> direction;
            double brightness;
            if (i % 2 == 0)
            {
                // Uniform on the sphere: the height is uniform in [-1, 1] (Archimedes), and so is the angle around.
                var height = 2 * random.NextDouble() - 1;
                var angle = 2 * Math.PI * random.NextDouble();
                direction = Direction(angle, Math.Asin(height));
                // Few bright stars, many faint ones.
                brightness = 0.25 + 0.75 * Math.Pow(random.NextDouble(), 5);
            }
            else
            {
                var longitude = 2 * Math.PI * random.NextDouble();
                var latitude = BandWidth * Gaussian(random);
                direction = galacticToEcliptic.Rotate(Direction(longitude, latitude));
                brightness = 0.12 + 0.55 * Math.Pow(random.NextDouble(), 5);
            }
            stars[i] = new(direction.Map(float.CreateTruncating), (float)brightness);
        }
        return new(gl, PrimitiveType.Points, stars);
    }

    // The direction at a longitude and a latitude: +x turned up by the latitude, then around +z by the longitude.
    static Vector3<double> Direction(double longitude, double latitude) =>
        Quaternion<double>.FromEulerAngles(new(0, -latitude, longitude), EulerOrder.XYZ).Rotate(Vector3<double>.UnitX);

    // A standard normal number by the Box-Muller transform.
    static double Gaussian(Random random) =>
        Math.Sqrt(-2 * Math.Log(1 - random.NextDouble())) * Math.Cos(2 * Math.PI * random.NextDouble());
}
