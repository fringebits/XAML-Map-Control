﻿using System;
using Helix.MapCore;

namespace MapControl
{
    /// <summary>
    /// Spherical Gnomonic Projection - AUTO2:97001.
    /// See "Map Projections - A Working Manual" (https://pubs.usgs.gov/publication/pp1395), p.165-167.
    /// </summary>
    public class GnomonicProjection : AzimuthalProjection
    {
        public const string DefaultCrsId = "AUTO2:97001"; // GeoServer non-standard CRS ID

        public GnomonicProjection()
            : this(DefaultCrsId)
        {
            // XAML needs parameterless constructor
        }

        public GnomonicProjection(string crsId)
        {
            CrsId = crsId;
        }

        public override Point? LocationToMap(Location location)
        {
            if (location.Equals(Center))
            {
                return new Point();
            }

            Center.GetAzimuthDistance(location, out double azimuth, out double distance);

            var mapDistance = distance < Math.PI / 2d
                ? Math.Tan(distance) * Wgs84EquatorialRadius
                : double.PositiveInfinity;

            return new Point(mapDistance * Math.Sin(azimuth), mapDistance * Math.Cos(azimuth));
        }

        public override Location MapToLocation(Point point)
        {
            if (point.X == 0d && point.Y == 0d)
            {
                return new Location(Center.Latitude, Center.Longitude);
            }

            var azimuth = Math.Atan2(point.X, point.Y);
            var mapDistance = Math.Sqrt(point.X * point.X + point.Y * point.Y);

            var distance = Math.Atan(mapDistance / Wgs84EquatorialRadius);

            return Center.GetLocation(azimuth, distance);
        }
    }
}
