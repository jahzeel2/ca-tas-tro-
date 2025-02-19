
using GeoSit.Data.BusinessEntities.Inmuebles;
using System;

namespace GeoSit.Data.BusinessEntities.LogicalTransactionUnits
{
    public class UnidadAlfanumericoParcela
    {
        public long Operacion { get; set; }
        public string NumeroPlano { get; set; }
        public DateTime? FechaOperacion { get; set; }
        public DateTime? FechaVigencia { get; set; }
        public long IdUsuario { get; set; }

        public Operaciones<long> OperacionesParcelasOrigenes { get; set; }

        public Operaciones<Parcela> OperacionesParcelasDestinos { get; set; }

        public UnidadAlfanumericoParcela()
        {
            OperacionesParcelasOrigenes = new Operaciones<long>();
            OperacionesParcelasDestinos = new Operaciones<Parcela>();
        }

        public void Clear()
        {
            OperacionesParcelasOrigenes.Clear();
            OperacionesParcelasDestinos.Clear();
        }
    }
}
