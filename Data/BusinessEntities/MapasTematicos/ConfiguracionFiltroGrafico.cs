﻿using System.Data.Spatial;
using System.Text;

namespace GeoSit.Data.BusinessEntities.MapasTematicos
{
    public class ConfiguracionFiltroGrafico : IEntity
    {
        public long ConfiguracionFiltroGraficoId { get; set; }
        public long FiltroId { get; set; }
        public byte[] Geometry { get; set; }
        public string sGeometry
        {
            get
            {
                if (Geometry == null) return null;
                return Encoding.UTF8.GetString(Geometry);
            }
            set
            {
                if (value == null) return;
                Geometry = Encoding.UTF8.GetBytes(value);
            }
        }
        public string Coordenadas { get; set; }
        public DbGeometry Geom { get; set; }
        public string WKT { get; set; }

        public ConfiguracionFiltro ConfiguracionFiltro { get; set; }

    }
}
