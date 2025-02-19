namespace GeoSit.Data.BusinessEntities.MesaEntradas.DTO
{
    public class GrillaTramite
    {
        public int IdTramite { get; set; }
        public string Iniciador { get; set; }
        public string Profesional { get; set; }
        public string Numero { get; set; }
        public string Asunto { get; set; }
        public string Causa { get; set; }
        public string SectorActual { get; set; }
        public string UsuarioSectorActual { get; set; }
        public string Estado { get; set; }
        public string Prioridad { get; set; }
        public string FechaUltimaActualizacion { get; set; }
    }
}
