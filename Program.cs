
class Program
{
    static void Main()
    {
        bool validate = false;
        if (validate)
        {
            HullValidator.Run();
            return;
        }

        int n = 1000; // Number of points to generate

        List<Point> points = PointGenerator.Generate(n);

        var solver = new ConvexHullSolver();

        List<Point> sequentialHull = solver.Solve(points);
        List<Point> parallelHull = solver.Solve(points);

        Console.WriteLine($"Input points: {points.Count}");
        Console.WriteLine($"Sequential hull points: {sequentialHull.Count}");
        Console.WriteLine($"Parallel hull points: {parallelHull.Count}");

        // Generate a visualization of the convex hull
        HullVisualizer.Save(points, sequentialHull, "sequential_hull.png");
        HullVisualizer.Save(points, parallelHull, "parallel_hull.png");

        Console.WriteLine("Saved visualizations to sequential_hull.png and parallel_hull.png");
    }
}
