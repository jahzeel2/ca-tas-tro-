using GeoSit.Data.BusinessEntities.DeclaracionesJuradas;

namespace GeoSit.Client.Web.Models.FormularioValuacion
{
    public struct CaracteristicaModel
    {
        public long Id { get; set; }
        public string Nombre { get; set; }
        public int Puntaje { get; set; }

        public string Descripcion { get { return $"{Puntaje} - {Nombre}"; } }

        private CaracteristicaModel(long id, string nombre, int puntaje)
        {
            Id = id;
            Nombre = nombre;
            Puntaje = puntaje;
        }

        public static CaracteristicaModel FromEntity(DDJJSorCaracteristicas caracteristica)
            => new CaracteristicaModel(caracteristica.IdSorCaracteristica, caracteristica.Descripcion, caracteristica.Puntaje);
    }
}