
class HullValidator
{
    public static void Run()
    {
        const int trialsPerSize = 100;
        const int maxPoints = 1000;
        const int seed = 4470;

        Random random = new(seed);
        var solver = new ConvexHullSolver();

        int testsPassed = 0;

        for (int n = 4; n <= maxPoints; n++)
        {
            for (int trial = 0; trial < trialsPerSize; trial++)
            {
                List<Point> points = GeneratePoints(random, n);

                List<Point> actual = solver.Solve(points);
                List<Point> expected = ReferenceHull(points);

                if (!SameVertices(actual, expected))
                {
                    Console.WriteLine("FAILED!");
                    Console.WriteLine($"Points: {n}");
                    Console.WriteLine($"Trial: {trial + 1}");
                    Console.WriteLine($"Tests passed: {testsPassed}");
                    Console.WriteLine($"Expected hull size: {expected.Count}");
                    Console.WriteLine($"Actual hull size: {actual.Count}");

                    Console.WriteLine("\nInput points:");
                    foreach (Point p in points)
                        Console.WriteLine($"({p.X}, {p.Y})");

                    Console.WriteLine("\nExpected hull:");
                    foreach (Point p in expected)
                        Console.WriteLine($"({p.X}, {p.Y})");

                    Console.WriteLine("\nActual hull:");
                    foreach (Point p in actual)
                        Console.WriteLine($"({p.X}, {p.Y})");

                    HullVisualizer.Save(
                        points, actual, "failed_hull.png");

                    HullVisualizer.Save(
                        points, expected, "expected_hull.png");

                    Console.WriteLine(
                        "\nSaved failed_hull.png and expected_hull.png");

                    return;
                }

                testsPassed++;
            }

            Console.WriteLine($"Passed: {n} points");
        }

        Console.WriteLine(
            $"\nAll {testsPassed} tests passed!");
    }

    private static List<Point> GeneratePoints(Random random, int count)
    {
        var points = new List<Point>();
        var used = new HashSet<(int, int)>();

        while (points.Count < count)
        {
            int x = random.Next(1000);
            int y = random.Next(1000);

            if (used.Add((x, y)))
                points.Add(new Point { X = x, Y = y });
        }

        return points;
    }

    // Independent reference: Andrew's monotone chain
    private static List<Point> ReferenceHull(List<Point> points)
    {
        var sorted = points
            .OrderBy(p => p.X)
            .ThenBy(p => p.Y)
            .ToList();

        if (sorted.Count <= 1)
            return sorted;

        var lower = new List<Point>();

        foreach (Point p in sorted)
        {
            while (lower.Count >= 2 &&
                Cross(lower[^2], lower[^1], p) <= 0)
            {
                lower.RemoveAt(lower.Count - 1);
            }

            lower.Add(p);
        }

        var upper = new List<Point>();

        for (int i = sorted.Count - 1; i >= 0; i--)
        {
            Point p = sorted[i];

            while (upper.Count >= 2 &&
                Cross(upper[^2], upper[^1], p) <= 0)
            {
                upper.RemoveAt(upper.Count - 1);
            }

            upper.Add(p);
        }

        lower.RemoveAt(lower.Count - 1);
        upper.RemoveAt(upper.Count - 1);

        lower.AddRange(upper);
        return lower;
    }

    private static double Cross(Point a, Point b, Point c)
    {
        return (b.X - a.X) * (c.Y - a.Y)
             - (b.Y - a.Y) * (c.X - a.X);
    }

    // Compare coordinates, regardless of hull traversal order
    
    private static bool SameVertices(
        List<Point> actual, List<Point> expected)
    {
        // Remove redundant collinear vertices before comparing
        var cleanedActual = RemoveCollinear(actual);
        var cleanedExpected = RemoveCollinear(expected);

        var actualSet = cleanedActual
            .Select(p => (p.X, p.Y))
            .ToHashSet();

        var expectedSet = cleanedExpected
            .Select(p => (p.X, p.Y))
            .ToHashSet();

        return cleanedActual.Count == actualSet.Count &&
            expectedSet.SetEquals(actualSet);
    }

    private static List<Point> RemoveCollinear(List<Point> hull)
    {
        if (hull.Count <= 2)
            return new List<Point>(hull);

        var cleaned = new List<Point>();

        for (int i = 0; i < hull.Count; i++)
        {
            Point prev = hull[(i - 1 + hull.Count) % hull.Count];
            Point curr = hull[i];
            Point next = hull[(i + 1) % hull.Count];

            double cross = Cross(prev, curr, next);

            bool between =
                curr.X >= Math.Min(prev.X, next.X) &&
                curr.X <= Math.Max(prev.X, next.X) &&
                curr.Y >= Math.Min(prev.Y, next.Y) &&
                curr.Y <= Math.Max(prev.Y, next.Y);

            // Keep vertices unless they are redundant collinear points
            if (cross != 0 || !between)
                cleaned.Add(curr);
        }

        return cleaned;
    }

}
