using GeoSit.Data.BusinessEntities.DeclaracionesJuradas;
using System;
using System.Linq;

namespace GeoSit.Client.Web.Models.FormularioValuacion
{
    public struct DDJJModel
    {
        private static DDJJModel Empty = new DDJJModel();
        public long IdDeclaracionJurada { get; set; }
        public long IdSor { get; set; }
        public long IdValuacion { get; set; }
        public string Tipo { get; set; }
        public object VigenciaDesde { get; set; }
        public object VigenciaHasta { get; set; }
        public string Version { get; set; }
        public string Valor { get; set; }
        public string Origen { get; set; }

        private DDJJModel(long idDDJJ, long idSor, string tipo, DateTime? fechaDesde, DateTime? fechaHasta, decimal? total, long idValuacion, string version, string origen)
        {
            IdDeclaracionJurada = idDDJJ;
            IdValuacion = idValuacion;
            IdSor = idSor;
            Tipo = tipo;
            VigenciaDesde = new { display = fechaDesde?.ToShortDateString() ?? "-", timestamp = fechaDesde?.Ticks };
            VigenciaHasta = new { display = fechaHasta?.ToShortDateString() ?? "-", timestamp = fechaHasta?.Ticks };
            Version = version;
            Valor = total?.ToString();
            Origen = origen;
        }
        
        internal bool IsEmpty() => Equals(Empty);

        internal static DDJJModel FromEntityVigente(DDJJ ddjj)
        {
            return FromEntity(ddjj, dj => dj.FechaHasta == null);
        }

        internal static DDJJModel FromEntityHistorica(DDJJ ddjj)
        {
            return FromEntity(ddjj, dj => dj.FechaHasta != null);
        }

        static DDJJModel FromEntity(DDJJ ddjj, Func<VALValuacion, bool> filter)
        {
            var valuaciones = ddjj.Valuaciones?.Where(filter).OrderByDescending(v => v.FechaDesde);
            if (!valuaciones?.Any() ?? true)
            {
                return Empty;
            }
            DateTime? fechaDesde = valuaciones.Min(v => v.FechaDesde);
            DateTime? fechaHasta = valuaciones.Max(v => v.FechaHasta);
            decimal? total = valuaciones?.FirstOrDefault()?.ValorTotal;
            long idValuacion = valuaciones?.FirstOrDefault()?.IdValuacion ?? 0;

            return new DDJJModel(ddjj.IdDeclaracionJurada,
                                 ddjj.Sor.IdSor,
                                 ddjj.Version.TipoDeclaracionJurada,
                                 fechaDesde,
                                 fechaHasta,
                                 total,
                                 idValuacion,
                                 ddjj.Version.VersionDeclaracionJurada.ToString(),
                                 ddjj.Origen.Descripcion);
        }
    }
}