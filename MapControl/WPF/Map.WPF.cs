using System;
using System.Windows;
using System.Windows.Input;

namespace MapControl
{
    using Point = Helix.CoreTypes.Point;

    /// <summary>
    /// MapBase with default input event handling.
    /// </summary>
    public class Map : MapBase
    {
        public static readonly DependencyProperty MouseWheelZoomDeltaProperty =
            DependencyPropertyHelper.Register<Map, double>(nameof(MouseWheelZoomDelta), 0.25);

        public static readonly DependencyProperty ManipulationModeProperty =
            DependencyPropertyHelper.Register<Map, ManipulationModes>(nameof(ManipulationMode), ManipulationModes.Translate | ManipulationModes.Scale);

        private Point? mousePosition;
        private double mouseWheelDelta;

        static Map()
        {
            IsManipulationEnabledProperty.OverrideMetadata(typeof(Map), new FrameworkPropertyMetadata(true));
        }

        /// <summary>
        /// Gets or sets the amount by which the ZoomLevel property changes by a MouseWheel event.
        /// The default value is 0.25.
        /// </summary>
        public double MouseWheelZoomDelta
        {
            get => (double)GetValue(MouseWheelZoomDeltaProperty);
            set => SetValue(MouseWheelZoomDeltaProperty, value);
        }

        /// <summary>
        /// Gets or sets a value that specifies how the map control handles manipulations.
        /// </summary>
        public ManipulationModes ManipulationMode
        {
            get => (ManipulationModes)GetValue(ManipulationModeProperty);
            set => SetValue(ManipulationModeProperty, value);
        }

        protected override void OnManipulationStarted(ManipulationStartedEventArgs e)
        {
            base.OnManipulationStarted(e);

            Manipulation.SetManipulationMode(this, ManipulationMode);
        }

        protected override void OnManipulationDelta(ManipulationDeltaEventArgs e)
        {
            base.OnManipulationDelta(e);

            TransformMap(e.ManipulationOrigin.ToCorePoint(),
                e.DeltaManipulation.Translation.ToCorePoint(),
                e.DeltaManipulation.Rotation,
                e.DeltaManipulation.Scale.LengthSquared / 2d);
        }

        // NUTRON_BEGIN - @mikeg: move responsibility of panning
        public void MousePanBegin(MouseEventArgs e)
        {
            if (this.CaptureMouse())
            {
                this.mousePosition = e.GetPosition(this).ToCorePoint();
            }
        }

        public void MousePanEnd(MouseEventArgs e)
        {
            if (this.mousePosition.HasValue)
            {
                this.mousePosition = null;
                this.ReleaseMouseCapture();
            }
        }

        public void MousePanMove(MouseEventArgs e)
        {
            if (mousePosition.HasValue)
            {
                var p = e.GetPosition(this);
                TranslateMap(new Point(p.X - mousePosition.Value.X, p.Y - mousePosition.Value.Y));
                mousePosition = p.ToCorePoint();
            }
        }
        // NUTRON_BEGIN - @mikeg: move responsibility of panning


        //protected override void OnMouseMove(MouseEventArgs e)
        //{
        //    base.OnMouseMove(e);

        //    //// #mikeg: need to fix this based on "mouse button mode" to avoid assumptions about left/right 

        //    //if (mousePosition.HasValue)
        //    //{
        //    //    var p = e.GetPosition(this);
        //    //    TranslateMap(new Point(p.X - mousePosition.Value.X, p.Y - mousePosition.Value.Y));
        //    //    mousePosition = p.ToCorePoint();
        //    //}
        //    //else if (e.LeftButton == MouseButtonState.Pressed &&
        //    //    Keyboard.Modifiers == ModifierKeys.None &&
        //    //    CaptureMouse())
        //    //{
        //    //    // Set mousePosition when no MouseLeftButtonDown event was received.
        //    //    //
        //    //    mousePosition = e.GetPosition(this).ToCorePoint();
        //    //}
        //}

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            base.OnMouseWheel(e);

            // Standard mouse wheel delta value is 120.
            //
            mouseWheelDelta += e.Delta / 120d;

            if (Math.Abs(mouseWheelDelta) >= 1d)
            {
                // Zoom to integer multiple of MouseWheelZoomDelta.
                //
                ZoomMap(e.GetPosition(this).ToCorePoint(),
                    MouseWheelZoomDelta * Math.Round(TargetZoomLevel / MouseWheelZoomDelta + mouseWheelDelta));

                mouseWheelDelta = 0d;
            }
        }
    }
}
