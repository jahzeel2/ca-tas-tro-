namespace GeoSit.Client.Web.ViewModels
{
    public class TramiteMesaEntradasViewModel
    {
        public enum MODO
        {
            Consulta,
            Edicion
        }

        public MODO Modo { get; set; }
        public string Numero { get; set; }

        public TramiteMesaEntradasViewModel(MODO modo)
        {
            Modo = modo; 
        }
    }
}