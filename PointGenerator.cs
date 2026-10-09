class PointGenerator
{
    public static List<Point> Generate(int count, int maxCoordinate = 1000)
    {
        Random rand = new Random();
        var points = new List<Point>();

        for (int i = 0; i < count; i++)
        {
            points.Add(new Point
            {
                X = rand.Next(maxCoordinate),
                Y = rand.Next(maxCoordinate)
            });
        }

        return points;
    }
}