using GeoSit.Data.BusinessEntities.DeclaracionesJuradas;
using System;

namespace GeoSit.Client.Web.Models.FormularioValuacion
{
    public struct ValuacionModel
    {
        public long IdValuacion { get; set; }
        public DateTime FechaDesde { get; set; }
        public DateTime? FechaHasta { get; set; }
        public decimal ValorTierra { get; set; }
        public double? Superficie { get; set; }
        public decimal ValorTotal { get; set; }
        public bool Vigente { get; set; }

        private ValuacionModel(long idValuacion, DateTime fechaDesde, DateTime? fechaHasta, decimal valorTierra, double? superficie, decimal valorTotal, bool vigente)
        {
            IdValuacion = idValuacion;
            FechaDesde = fechaDesde;
            FechaHasta = fechaHasta;
            ValorTierra = valorTierra;
            Superficie = superficie;
            ValorTotal = valorTotal;
            Vigente = vigente;
        }

        internal static ValuacionModel FromEntity(VALValuacion entity)
            => new ValuacionModel(entity.IdValuacion,
                                  entity.FechaDesde,
                                  entity.FechaHasta,
                                  entity.ValorTierra,
                                  entity.Superficie,
                                  entity.ValorTotal,
                                  entity.FechaHasta == null);
    }
}