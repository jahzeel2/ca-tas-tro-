using System;

namespace GeoSit.Data.BusinessEntities.MesaEntradas.DTO
{
    public class GrillaPartida
    {
        public long  IdUt { get; set; }
        public long IdParcela { get; set; }
        public string Partida { get; set; }
        public string Tipo { get; set; }
        public string Inscripcion { get; set; }
        public string DominioTipo { get; set; }
        public string DominioInscripcion { get; set; }
        public string UnidadFuncional { get; set; }
        public DateTime? FechaVigenciaDesde { get; set; }
        public DateTime? FechaVigenciaHasta { get; set; }
    }
}
