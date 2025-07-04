﻿#if WPF
using System.Windows;
using System.Windows.Media;
#elif UWP
using Windows.UI.Xaml.Media;
#elif WINUI
using Microsoft.UI.Xaml.Media;
#endif

namespace MapControl
{
    using Point = Helix.CoreTypes.Point;

    /// <summary>
    /// Defines the transformation between projected map coordinates in meters
    /// and view coordinates in pixels.
    /// </summary>
    public partial class ViewTransform
    {
        /// <summary>
        /// Gets the scaling factor from projected map coordinates to view coordinates,
        /// as pixels per meter.
        /// </summary>
        public double Scale { get; private set; }

        /// <summary>
        /// Gets the rotation angle of the transform matrix.
        /// </summary>
        public double Rotation { get; private set; }

        /// <summary>
        /// Gets the transform matrix from projected map coordinates to view coordinates.
        /// </summary>
        public Matrix MapToViewMatrix { get; private set; }

        /// <summary>
        /// Gets the transform matrix from view coordinates to projected map coordinates.
        /// </summary>
        public Matrix ViewToMapMatrix { get; private set; }

        /// <summary>
        /// Transforms a Point from projected map coordinates to view coordinates.
        /// </summary>
        public Point MapToView(Point point)
        {
            return MapToViewMatrix.Transform(point.ToSystemPoint()).ToCorePoint();
        }

        /// <summary>
        /// Transforms a Point from view coordinates to projected map coordinates.
        /// </summary>
        public Point ViewToMap(Point point)
        {
            return ViewToMapMatrix.Transform(point.ToSystemPoint()).ToCorePoint();
        }

        /// <summary>
        /// Transform relative to absolute map scale. Returns horizontal and vertical
        /// scaling factors from meters to view coordinates.
        /// </summary>
        public Point GetMapScale(Point relativeScale)
        {
            return new Point(Scale * relativeScale.X, Scale * relativeScale.Y);
        }

#if WPF || UWP || WINUI
        /// <summary>
        /// Initializes a ViewTransform from a map center point in projected coordinates,
        /// a view conter point, a scaling factor from projected coordinates to view coordinates
        /// and a rotation angle in degrees.
        /// </summary>
        public void SetTransform(Point mapCenter, Point viewCenter, double scale, double rotation)
        {
            Scale = scale;
            Rotation = ((rotation % 360d) + 360d) % 360d;

            var transform = new Matrix(scale, 0d, 0d, -scale, -scale * mapCenter.X, scale * mapCenter.Y);
            transform.Rotate(Rotation);
            transform.Translate(viewCenter.X, viewCenter.Y);

            MapToViewMatrix = transform;

            transform.Invert();

            ViewToMapMatrix = transform;
        }

        /// <summary>
        /// Gets a transform Matrix from meters to view coordinates for a relative map scale.
        /// </summary>
        public Matrix GetMapTransform(Point relativeScale)
        {
            var scale = GetMapScale(relativeScale);

            var transform = new Matrix(scale.X, 0d, 0d, scale.Y, 0d, 0d);
            transform.Rotate(Rotation);

            return transform;
        }

        /// <summary>
        /// Gets the transform Matrix for the RenderTranform of a MapTileLayer.
        /// </summary>
        public Matrix GetTileLayerTransform(double tileMatrixScale, Point tileMatrixTopLeft, Point tileMatrixOrigin)
        {
            // Tile matrix origin in map coordinates.
            //
            var mapOrigin = new Point(
                tileMatrixTopLeft.X + tileMatrixOrigin.X / tileMatrixScale,
                tileMatrixTopLeft.Y - tileMatrixOrigin.Y / tileMatrixScale);

            // Tile matrix origin in view coordinates.
            //
            var viewOrigin = MapToView(mapOrigin);

            var transformScale = Scale / tileMatrixScale;

            var transform = new Matrix(transformScale, 0d, 0d, transformScale, 0d, 0d);
            transform.Rotate(Rotation);
            transform.Translate(viewOrigin.X, viewOrigin.Y);

            return transform;
        }

        /// <summary>
        /// Gets the index bounds of a tile matrix.
        /// </summary>
        public Rect GetTileMatrixBounds(double tileMatrixScale, Point tileMatrixTopLeft, double viewWidth, double viewHeight)
        {
            // View origin in map coordinates.
            //
            var origin = ViewToMap(new Point());

            var transformScale = tileMatrixScale / Scale;

            var transform = new Matrix(transformScale, 0d, 0d, transformScale, 0d, 0d);
            transform.Rotate(-Rotation);

            // Translate origin to tile matrix origin in pixels.
            //
            transform.Translate(
                tileMatrixScale * (origin.X - tileMatrixTopLeft.X),
                tileMatrixScale * (tileMatrixTopLeft.Y - origin.Y));

            // Transform view bounds to tile pixel bounds.
            //
            return new MatrixTransform { Matrix = transform }
                .TransformBounds(new Rect(0d, 0d, viewWidth, viewHeight));
        }

        internal static Matrix CreateTransformMatrix(
            double translation1X, double translation1Y,
            double rotation,
            double translation2X, double translation2Y)
        {
            var transform = new Matrix(1d, 0d, 0d, 1d, translation1X, translation1Y);
            transform.Rotate(rotation);
            transform.Translate(translation2X, translation2Y);

            return transform;
        }
#endif
    }
}
