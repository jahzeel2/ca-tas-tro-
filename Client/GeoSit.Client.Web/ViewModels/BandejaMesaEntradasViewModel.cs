using System.Web.Mvc;

namespace GeoSit.Client.Web.ViewModels
{
    public class BandejaMesaEntradasViewModel
    {
        public bool EsProfesional { get; set; }
        public SelectList Asuntos { get; set; }
        public SelectList Causas { get; set; }
        public SelectList Estados { get; set; }
        public SelectList Prioridades { get; set; }
        public SelectList Sectores { get; set; }
    }
}