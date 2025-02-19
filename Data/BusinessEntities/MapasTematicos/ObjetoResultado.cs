﻿using System;
using System.Data.Spatial;

namespace GeoSit.Data.BusinessEntities.MapasTematicos
{
    public class ObjetoResultado : IEntity
    {
        public long FeatId { get; set; }
        public long ClassId { get; set; }
        public int RevisionNumber { get; set; }
        public int? GeometryType { get; set; }
        public string GUID { get; set; }
        public string IdOrigin { get; set; }
        public string Descripcion { get; set; }
        public string Valor { get; set; }
        public int? Rango { get; set; }
        public DateTime FechaAlta { get; set; }
        public DbGeometry Geom { get; set; }
        public string WKT { get; set; }
    }
}
