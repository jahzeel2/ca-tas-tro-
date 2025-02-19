namespace GeoSit.Data.BusinessEntities.MesaEntradas.DTO
{
    public class Asignacion
    {
        public long IdUsuarioAsignado { get; set; }
        public int[] IdTramitesAsignados { get; set; }
        public long IdUsuarioOperacion { get; set; }
        public bool Reasigna { get;set; }
        public string Observacion { get; set; }
        public string Ip { get; set; }
        public string MachineName { get; set; }
    }
}
