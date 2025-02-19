using GeoSit.Data.BusinessEntities.Inmuebles;
using System;
using System.Collections.Generic;

namespace GeoSit.Data.BusinessEntities.DeclaracionesJuradas
{
    public class DDJJDominio
    {
        public long IdDominio { get; set; }
        public long IdTipoInscripcion { get; set; }
        public string Inscripcion { get; set; }
        public DateTime Fecha { get; set; }
        public long IdUsuarioAlta { get; set; }
        public DateTime FechaAlta { get; set; }
        public long IdUsuarioModif { get; set; }
        public DateTime FechaModif { get; set; }
        public long? IdUsuarioBaja { get; set; }
        public DateTime? FechaBaja { get; set; }

        public TipoInscripcion TipoInscripcion { get; set; }
        public ICollection<DDJJDominioTitular> Titulares { get; set; }

        public string NombreTipoInscripcion { get { return TipoInscripcion?.Descripcion ?? string.Empty; } }
    }
}
