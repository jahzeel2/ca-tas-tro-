using System.Data.Spatial;
using System;

namespace GeoSit.Data.BusinessEntities.ModuloPloteo
{
    [Serializable]
    public class Manzana
    {
        public long FeatId { get; set; }
        public string Nomenclatura { get; set; }
        public DbGeometry Geom { get; set; }
    }
}
