namespace GeoSit.Data.BusinessEntities.DeclaracionesJuradas
{
    public class DatosFormulario
    {
        public long IdUsuarioOperacion { get; set; }
        public string IP { get; set; }
        public string MachineName { get; set; }
        public VALSuperficie[] Superficies { get; set; }

        public DatosFormulario(VALSuperficie[] superficies, long idUsuarioOperacion, string ip, string machineName)
        {
            IdUsuarioOperacion = idUsuarioOperacion;
            IP = ip;
            MachineName = machineName;
            Superficies = superficies;
        }
    }
}
