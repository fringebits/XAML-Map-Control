using System;

namespace MapControl
{
    public class NOAAMapsTileSource : TileSource
    {
        public override Uri GetUri(int column, int row, int zoomLevel)
        {
            var uri = base.GetUri(column, row, zoomLevel - 2);

            return uri;
        }
    }
}
