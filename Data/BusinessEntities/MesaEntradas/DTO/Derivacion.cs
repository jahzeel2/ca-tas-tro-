namespace GeoSit.Data.BusinessEntities.MesaEntradas.DTO
{
    public class Derivacion
    {
        public int[] IdTramitesSeleccionados { get; set; }
        public int IdSector { get; set; }
        public string Observacion { get; set; }
        public long IdUsuarioOperacion { get; set; }
        public string Ip { get; set; }
        public string MachineName { get; set; }
    }
}
