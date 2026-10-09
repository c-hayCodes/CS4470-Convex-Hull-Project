
using ScottPlot;

class HullVisualizer
{
    public static void Save(
        List<Point> points,
        List<Point> hull,
        string filename = "hull.png")
    {
        Plot plot = new();

        // Draw all the original points
        double[] pointXs = points.Select(p => p.X).ToArray();
        double[] pointYs = points.Select(p => p.Y).ToArray();

        var inputPoints = plot.Add.ScatterPoints(pointXs, pointYs);
        inputPoints.Color = Colors.Blue;
        inputPoints.MarkerSize = 4;
        inputPoints.LegendText = "Input Points";

        if (hull.Count > 0)
        {
            // Close the polygon by repeating the first hull point
            List<Point> closedHull = new(hull);

            if (hull.Count > 2)
                closedHull.Add(hull[0]);

            double[] hullXs = closedHull.Select(p => p.X).ToArray();
            double[] hullYs = closedHull.Select(p => p.Y).ToArray();

            // Draw the convex hull boundary
            var boundary = plot.Add.ScatterLine(hullXs, hullYs);
            boundary.Color = Colors.Red;
            boundary.LineWidth = 3;
            boundary.LegendText = "Convex Hull";

            // Highlight the hull vertices
            double[] vertexXs = hull.Select(p => p.X).ToArray();
            double[] vertexYs = hull.Select(p => p.Y).ToArray();

            var vertices = plot.Add.ScatterPoints(vertexXs, vertexYs);
            vertices.Color = Colors.Orange;
            vertices.MarkerSize = 8;
            vertices.LegendText = "Hull Vertices";
        }

        plot.Title($"Convex Hull ({points.Count} Points)");
        plot.XLabel("X");
        plot.YLabel("Y");
        plot.ShowLegend();

        plot.SavePng(filename, 1200, 800);
    }
}
