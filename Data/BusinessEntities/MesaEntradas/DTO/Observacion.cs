namespace GeoSit.Data.BusinessEntities.MesaEntradas.DTO
{
    public class Observacion
    {
        public int IdTramite { get; set; }
        public string Motivo { get; set; }
        public long IdUsuarioOperacion { get; set; }
        public string Ip { get; set; }
        public string MachineName { get; set; }
    }
}
