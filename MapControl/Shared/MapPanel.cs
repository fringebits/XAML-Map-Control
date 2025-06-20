﻿#if WPF
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
#elif UWP
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
#elif WINUI
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
#endif

/// <summary>
/// Arranges child elements on a Map at positions specified by the attached property Location,
/// or in rectangles specified by the attached property BoundingBox.
/// </summary>
namespace MapControl
{
    using Helix.MapCore;
    using Point = Helix.CoreTypes.Point;
    using Rect = Helix.CoreTypes.Rect;

    /// <summary>
    /// Optional interface to hold the value of the attached property MapPanel.ParentMap.
    /// </summary>
    public interface IMapElement
    {
        MapBase ParentMap { get; set; }
    }

    public partial class MapPanel : Panel, IMapElement
    {
        private static readonly DependencyProperty ViewPositionProperty =
            DependencyPropertyHelper.RegisterAttached<Point?>("ViewPosition", typeof(MapPanel));

        private static readonly DependencyProperty ParentMapProperty =
            DependencyPropertyHelper.RegisterAttached<MapBase>("ParentMap", typeof(MapPanel), null,
                (element, oldValue, newValue) =>
                {
                    if (element is IMapElement mapElement)
                    {
                        mapElement.ParentMap = newValue;
                    }
                },
                true); // inherits

        private MapBase parentMap;

        /// <summary>
        /// Implements IMapElement.ParentMap.
        /// </summary>
        public MapBase ParentMap
        {
            get => parentMap;
            set => SetParentMap(value);
        }

        /// <summary>
        /// Gets a value that controls whether an element's Visibility is automatically
        /// set to Collapsed when it is located outside the visible viewport area.
        /// </summary>
        public static bool GetAutoCollapse(FrameworkElement element)
        {
            return (bool)element.GetValue(AutoCollapseProperty);
        }

        /// <summary>
        /// Sets the AutoCollapse property.
        /// </summary>
        public static void SetAutoCollapse(FrameworkElement element, bool value)
        {
            element.SetValue(AutoCollapseProperty, value);
        }

        /// <summary>
        /// Gets the Location of an element.
        /// </summary>
        public static Location GetLocation(FrameworkElement element)
        {
            return (Location)element.GetValue(LocationProperty);
        }

        /// <summary>
        /// Sets the Location of an element.
        /// </summary>
        public static void SetLocation(FrameworkElement element, Location value)
        {
            element.SetValue(LocationProperty, value);
        }

        /// <summary>
        /// Gets the BoundingBox of an element.
        /// </summary>
        public static BoundingBox GetBoundingBox(FrameworkElement element)
        {
            return (BoundingBox)element.GetValue(BoundingBoxProperty);
        }

        /// <summary>
        /// Sets the BoundingBox of an element.
        /// </summary>
        public static void SetBoundingBox(FrameworkElement element, BoundingBox value)
        {
            element.SetValue(BoundingBoxProperty, value);
        }

        /// <summary>
        /// Gets the view position of an element with Location.
        /// </summary>
        public static Point? GetViewPosition(FrameworkElement element)
        {
            return (Point?)element.GetValue(ViewPositionProperty);
        }

        /// <summary>
        /// Sets the attached ViewPosition property of an element. The method is called during
        /// ArrangeOverride and may be overridden to modify the actual view position value.
        /// An overridden method should call this method to set the attached property.
        /// </summary>
        protected virtual void SetViewPosition(FrameworkElement element, ref Point? position)
        {
            element.SetValue(ViewPositionProperty, position);
        }

        protected virtual void SetParentMap(MapBase map)
        {
            if (parentMap != null && parentMap != this)
            {
                parentMap.ViewportChanged -= OnViewportChanged;
            }

            parentMap = map;

            if (parentMap != null && parentMap != this)
            {
                parentMap.ViewportChanged += OnViewportChanged;

                OnViewportChanged(new ViewportChangedEventArgs());
            }
        }

        private void OnViewportChanged(object sender, ViewportChangedEventArgs e)
        {
            OnViewportChanged(e);
        }

        protected virtual void OnViewportChanged(ViewportChangedEventArgs e)
        {
            InvalidateArrange();
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            availableSize = new Size(double.PositiveInfinity, double.PositiveInfinity);

            foreach (var element in ChildElements)
            {
                element.Measure(availableSize);
            }

            return new Size();
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            if (parentMap != null)
            {
                foreach (var element in ChildElements)
                {
                    ArrangeChildElement(element, finalSize);
                }
            }

            return finalSize;
        }

        private Point? GetViewPosition(Location location)
        {
            var position = parentMap.LocationToView(location);

            if (parentMap.MapProjection.Type <= MapProjectionType.NormalCylindrical &&
                position.HasValue && !parentMap.InsideViewBounds(position.Value))
            {
                var coercedPosition = parentMap.LocationToView(
                    new Location(location.Latitude, parentMap.CoerceLongitude(location.Longitude)));

                if (coercedPosition.HasValue)
                {
                    position = coercedPosition;
                }
            }

            return position;
        }

        private Rect GetViewRect(Rect mapRect)
        {
            var center = new Point(mapRect.X + mapRect.Width / 2d, mapRect.Y + mapRect.Height / 2d);
            var position = parentMap.ViewTransform.MapToView(center);

            if (parentMap.MapProjection.Type <= MapProjectionType.NormalCylindrical &&
                !parentMap.InsideViewBounds(position))
            {
                var location = parentMap.MapProjection.MapToLocation(center);

                if (location != null)
                {
                    var coercedPosition = parentMap.LocationToView(
                        new Location(location.Latitude, parentMap.CoerceLongitude(location.Longitude)));

                    if (coercedPosition.HasValue)
                    {
                        position = coercedPosition.Value;
                    }
                }
            }

            var width = mapRect.Width * parentMap.ViewTransform.Scale;
            var height = mapRect.Height * parentMap.ViewTransform.Scale;
            var x = position.X - width / 2d;
            var y = position.Y - height / 2d;

            return new Rect(x, y, width, height);
        }

        private void ArrangeChildElement(FrameworkElement element, Size panelSize)
        {
            var location = GetLocation(element);

            if (location != null)
            {
                var position = GetViewPosition(location);

                SetViewPosition(element, ref position);

                if (GetAutoCollapse(element))
                {
                    SetVisible(element, position.HasValue && parentMap.InsideViewBounds(position.Value));
                }

                if (position.HasValue)
                {
                    ArrangeElement(element, position.Value);
                }
            }
            else
            {
                element.ClearValue(ViewPositionProperty);

                var boundingBox = GetBoundingBox(element);

                if (boundingBox != null)
                {
                    ArrangeElement(element, boundingBox);
                }
                else
                {
                    ArrangeElement(element, panelSize);
                }
            }
        }

        private void ArrangeElement(FrameworkElement element, BoundingBox boundingBox)
        {
            Rect? mapRect = null;
            var rotation = 0d;

            if (boundingBox is LatLonBox latLonBox)
            {
                var rotatedRect = parentMap.MapProjection.LatLonBoxToMap(latLonBox);

                if (rotatedRect != null)
                {
                    mapRect = rotatedRect.Item1;
                    rotation = -rotatedRect.Item2;
                }
            }
            else
            {
                mapRect = parentMap.MapProjection.BoundingBoxToMap(boundingBox);
            }

            if (mapRect.HasValue)
            {
                var viewRect = GetViewRect(mapRect.Value);

                element.Width = viewRect.Width;
                element.Height = viewRect.Height;
                element.Arrange(viewRect.ToSystemRect());

                rotation += parentMap.ViewTransform.Rotation;

                if (element.RenderTransform is RotateTransform rotateTransform)
                {
                    rotateTransform.Angle = rotation;
                }
                else if (rotation != 0d)
                {
                    SetRenderTransform(element, new RotateTransform { Angle = rotation }, 0.5, 0.5);
                }
            }
        }

        private static void ArrangeElement(FrameworkElement element, Point position)
        {
            var size = GetDesiredSize(element);
            var x = position.X;
            var y = position.Y;

            switch (element.HorizontalAlignment)
            {
                case HorizontalAlignment.Center:
                    x -= size.Width / 2d;
                    break;

                case HorizontalAlignment.Right:
                    x -= size.Width;
                    break;

                default:
                    break;
            }

            switch (element.VerticalAlignment)
            {
                case VerticalAlignment.Center:
                    y -= size.Height / 2d;
                    break;

                case VerticalAlignment.Bottom:
                    y -= size.Height;
                    break;

                default:
                    break;
            }

            element.Arrange(new System.Windows.Rect(x, y, size.Width, size.Height));
        }

        private static void ArrangeElement(FrameworkElement element, Size panelSize)
        {
            var size = GetDesiredSize(element);
            var x = 0d;
            var y = 0d;
            var width = size.Width;
            var height = size.Height;

            switch (element.HorizontalAlignment)
            {
                case HorizontalAlignment.Center:
                    x = (panelSize.Width - size.Width) / 2d;
                    break;

                case HorizontalAlignment.Right:
                    x = panelSize.Width - size.Width;
                    break;

                case HorizontalAlignment.Stretch:
                    width = panelSize.Width;
                    break;

                default:
                    break;
            }

            switch (element.VerticalAlignment)
            {
                case VerticalAlignment.Center:
                    y = (panelSize.Height - size.Height) / 2d;
                    break;

                case VerticalAlignment.Bottom:
                    y = panelSize.Height - size.Height;
                    break;

                case VerticalAlignment.Stretch:
                    height = panelSize.Height;
                    break;

                default:
                    break;
            }

            element.Arrange(new System.Windows.Rect(x, y, width, height));
        }

        internal static Size GetDesiredSize(FrameworkElement element)
        {
            var width = element.DesiredSize.Width;
            var height = element.DesiredSize.Height;

            if (width < 0d || width == double.PositiveInfinity)
            {
                width = 0d;
            }

            if (height < 0d || height == double.PositiveInfinity)
            {
                height = 0d;
            }

            return new Size(width, height);
        }
    }
}
