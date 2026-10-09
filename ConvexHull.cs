class ConvexHullSolver
{
    public List<Point> Solve(List<Point> points)
    {
        points = points.OrderBy(p => p.X).ThenBy(p => p.Y).ToList(); // Ensure points are sorted as required by the algorithm
        Console.WriteLine("Solving convex hull...");
        return ConvexHullDC(points);
    }

    public List<Point> SolveParallel(List<Point> points)
    {
        points = points.OrderBy(p => p.X).ThenBy(p => p.Y).ToList(); // Ensure points are sorted as required by the algorithm
        var AvailableThreads = Environment.ProcessorCount;
        Console.WriteLine("Solving convex hull in parallel with {0} threads...", AvailableThreads);
        int maxDepth = (int)Math.Ceiling(Math.Log2(AvailableThreads)); // Calculate the maximum depth for parallel processing based on our available threads
        return ConvexHullParallelDC(points, 0, maxDepth).GetAwaiter().GetResult(); // Call parallel divide-and-conquer, setting the max depth to the number of logical processors
    }

    private List<Point> ConvexHullDC(List<Point> points)
    {
        // Handle empty input
        if (points.Count == 0)
            return new List<Point>();

        // If every point has the same X coordinate,
        // the hull consists of the highest and lowest points.
        if (points[0].X == points[^1].X)
        {
            Point bottom = points.MinBy(p => p.Y);
            Point top = points.MaxBy(p => p.Y);

            if (bottom.Y == top.Y)
                return new List<Point> { bottom };

            return new List<Point> { bottom, top };
        }

        // Base case: 3 or fewer points
        if (points.Count <= 3)
        {
            return BaseHull(points);
        }

        // Divide the points into two halves
        int mid = points.Count / 2;
        double splitX = points[mid].X;

        // Move the split backward to avoid dividing
        // points that share the same X coordinate.
        while (mid > 0 && points[mid - 1].X == splitX)
        {
            mid--;
        }

        // If moving backward would create an empty left half,
        // move forward past all points sharing splitX instead.
        if (mid == 0)
        {
            mid = points.FindIndex(p => p.X > splitX);
        }

        // Create the left and right subsets
        List<Point> leftHalf = points.GetRange(0, mid);
        List<Point> rightHalf = points.GetRange(mid, points.Count - mid);

        // Recursively calculate the hulls
        List<Point> leftHull = ConvexHullDC(leftHalf);
        List<Point> rightHull = ConvexHullDC(rightHalf);

        // Find upper and lower tangents
        var (leftUpper, rightUpper) =
            FindUpperTangents(leftHull, rightHull);

        var (leftLower, rightLower) =
            FindLowerTangents(leftHull, rightHull);

        // Merge the hulls
        return MergeHulls(
            leftHull,
            rightHull,
            leftUpper,
            rightUpper,
            leftLower,
            rightLower
        );
    }

    
    private Task<List<Point>> ConvexHullParallelDC(List<Point> points, int depth, int maxDepth)
    {
        // At maximum depth, schedule the remaining
        // divide-and-conquer work as a sequential task.
        if (depth >= maxDepth ||
            points.Count <= 3 ||
            points[0].X == points[^1].X)
        {
            return Task.Run(() => ConvexHullDC(points));
        }

        // Divide the points into two halves
        int mid = points.Count / 2;
        double splitX = points[mid].X;

        // Keep points sharing the same X coordinate together
        while (mid > 0 && points[mid - 1].X == splitX)
        {
            mid--;
        }

        // Prevent an empty left partition
        if (mid == 0)
        {
            mid = points.FindIndex(p => p.X > splitX);
        }

        List<Point> leftHalf = points.GetRange(0, mid);
        List<Point> rightHalf = points.GetRange(
            mid, points.Count - mid);

        // Build both branches of the task tree
        Task<List<Point>> leftTask =
            ConvexHullParallelDC(leftHalf, depth + 1, maxDepth);

        Task<List<Point>> rightTask =
            ConvexHullParallelDC(rightHalf, depth + 1, maxDepth);

        // Merge when both dependencies finish
        return MergeWhenReady(leftTask, rightTask);
    }

    private async Task<List<Point>> MergeWhenReady(
        Task<List<Point>> leftTask,
        Task<List<Point>> rightTask)
    {
        // Wait for both child tasks without blocking
        // a ThreadPool worker during the wait.
        await Task.WhenAll(leftTask, rightTask)
            .ConfigureAwait(false);

        List<Point> leftHull = await leftTask;
        List<Point> rightHull = await rightTask;

        // Find upper and lower tangents
        var (leftUpper, rightUpper) =
            FindUpperTangents(leftHull, rightHull);

        var (leftLower, rightLower) =
            FindLowerTangents(leftHull, rightHull);

        // Merge the completed child hulls
        return MergeHulls(
            leftHull,
            rightHull,
            leftUpper,
            rightUpper,
            leftLower,
            rightLower
        );
    }

    private List<Point> BaseHull(List<Point> points)
    {
        // points.Count == 1 will never happen with a point set with size > 1
        if (points.Count == 1)
            return new List<Point> { points[0] };
        if (points.Count == 2)
            return new List<Point> { points[0], points[1] };
        // else, n == 3:
        // Compute the convex hull for 3 points, pick the leftmost as anchor
        int iA = leftmostIndex(points);
        Point A = points[iA];
        var others = points.Where((p, i) => i != iA).ToList();
        // The remaining two points are B and C
        Point B = others[0];
        Point C = others[1];
        // Order the hull clockwise
        if (Slope(A, B) > Slope(A, C))
            (B, C) = (C, B);
        return new List<Point> { A, B, C };
    }

    private double Cross(Point a, Point b, Point c)
    {
        return (b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X);
    }

    private (int leftUpper, int rightUpper) FindUpperTangents(List<Point> leftHull, List<Point> rightHull)
    {
        int leftUpper = rightmostIndex(leftHull);
        int rightUpper = leftmostIndex(rightHull);

        
        bool changed = true;
        while (changed) // Keep adjusting the tangents until no changes occur
        {
            changed = false;
            while (leftHull.Count >= 2 &&     // If current slope < next slope, step CCW
                Slope(leftHull[leftUpper], rightHull[rightUpper]) <
                Slope(leftHull[nextCCW(leftUpper, leftHull.Count)], rightHull[rightUpper])
            ){
                leftUpper = nextCCW(leftUpper, leftHull.Count);
                changed = true;
            }

            while (rightHull.Count >= 2 &&    // If current slope > next slope, step CW
                Slope(leftHull[leftUpper], rightHull[rightUpper]) >
                Slope(leftHull[leftUpper], rightHull[nextCW(rightUpper, rightHull.Count)])
            ){
                rightUpper = nextCW(rightUpper, rightHull.Count);
                changed = true;
            }
        }
        return (leftUpper, rightUpper);
    }

    private (int leftLower, int rightLower) FindLowerTangents(List<Point> leftHull, List<Point> rightHull)
    {
        int leftLower = rightmostIndex(leftHull);
        int rightLower = leftmostIndex(rightHull);

        bool changed = true;
        while (changed) // Keep adjusting the tangents until no changes occur
        {
            changed = false;
            while (leftHull.Count >= 2 &&     // If current slope > next slope, step CW
                Slope(leftHull[leftLower], rightHull[rightLower]) >
                Slope(leftHull[nextCW(leftLower, leftHull.Count)], rightHull[rightLower])
            ){
                leftLower = nextCW(leftLower, leftHull.Count);
                changed = true;
            }

            while (rightHull.Count >= 2 &&    // If current slope < next slope, step CCW
                Slope(leftHull[leftLower], rightHull[rightLower]) <
                Slope(leftHull[leftLower], rightHull[nextCCW(rightLower, rightHull.Count)])
            ){
                rightLower = nextCCW(rightLower, rightHull.Count);
                changed = true;
            }
        }
        return (leftLower, rightLower);
    }

    private List<Point> MergeHulls(List<Point> leftHull, List<Point> rightHull, int leftUpper, int rightUpper, int leftLower, int rightLower)
    {
        var newHull = new List<Point>();
        // Go around the right hull CW from rightUpper to rightLower
        var k = rightUpper;
        newHull.Add(rightHull[k]);
        while (k != rightLower)
        {
            k = nextCW(k, rightHull.Count);
            newHull.Add(rightHull[k]);
        }
        // Next, go around the left hull CW from leftLower to leftUpper
        k = leftLower;
        newHull.Add(leftHull[k]);
        while (k != leftUpper)
        {
            k = nextCW(k, leftHull.Count);
            newHull.Add(leftHull[k]);
        }
        return newHull;
    }

    private double Slope(Point p1, Point p2)
    {
        double dx = p2.X - p1.X;
        double dy = p2.Y - p1.Y;
        if (dx == 0) // Avoid division by zero for vertical lines
            if (dy > 0)
                return double.PositiveInfinity; // Vertical line with positive direction
            else
                return double.NegativeInfinity; // Vertical line with negative direction
        return dy / dx;
    }

    private int leftmostIndex(List<Point> hull)
    {
        int bestIndex = 0;
        for (int i = 1; i < hull.Count; i++)
        {
            if (hull[i].X < hull[bestIndex].X)
                bestIndex = i;
        }
        return bestIndex;
    }

    private int rightmostIndex(List<Point> hull)
    {
        int bestIndex = 0;
        for (int i = 1; i < hull.Count; i++)
        {
            if (hull[i].X > hull[bestIndex].X)
                bestIndex = i;
        }
        return bestIndex;
    }

    private int nextCW(int current, int count)
    {
        return (current + 1) % count; // Move to the next point in clockwise direction, use % to keep cyclical
    }

    private int nextCCW(int current, int count)
    {
        return (current - 1 + count) % count; // Move to the next point in counter-clockwise direction, use % to keep cyclical
    }
}