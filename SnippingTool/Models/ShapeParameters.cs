using Color = System.Windows.Media.Color;
using Point = System.Windows.Point;

namespace SnippingTool.Models;

public abstract record ShapeParameters;

public record ArrowShapeParameters(
    Point P1,
    Point P2,
    Color Color,
    double Thickness,
    Point[] ArrowHead) : ShapeParameters;

public record RectShapeParameters(
    double Left,
    double Top,
    double Width,
    double Height,
    Color Color,
    double Thickness,
    bool IsHighlight) : ShapeParameters;

public record EllipseShapeParameters(
    double Left,
    double Top,
    double Width,
    double Height,
    Color Color,
    double Thickness) : ShapeParameters;

public record LineShapeParameters(
    Point P1,
    Point P2,
    Color Color,
    double Thickness) : ShapeParameters;

public record PenShapeParameters(
    Point StartPoint,
    Color Color,
    double Thickness) : ShapeParameters;
