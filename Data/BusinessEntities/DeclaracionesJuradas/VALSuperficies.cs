using System;
using System.Collections.Generic;

namespace GeoSit.Data.BusinessEntities.DeclaracionesJuradas
{
    public class VALSuperficie
    {
        public long IdSuperficie { get; set; }
        public long IdAptitud { get; set; }
        public long IdSor { get; set; }
        public double? Superficie { get; set; }      
        public short? TrazaDepreciable { get; set; }
        public int Puntaje { get; set; }
        public decimal PuntajeSuperficie 
        { 
            get 
            {
                decimal superficieHA = Convert.ToDecimal(Superficie ?? 0) / 10_000;
                return Puntaje / 100m * (Math.Truncate(superficieHA * 100) / 100); //sólo tengo en cuenta hasta "area" sin ningún tipo de redondeo
            } 
        }

        public long IdUsuarioAlta { get; set; }
        public DateTime FechaAlta { get; set; }
        public long IdUsuarioModif { get; set; }
        public DateTime FechaModif { get; set; }
        public long? IdUsuarioBaja { get; set; }
        public DateTime? FechaBaja { get; set; }

        public DDJJSor Sor { get; set; }
        public VALAptitudes Aptitud { get; set; }
        public ICollection<DDJJSorCar> Caracteristicas { get; set; }
    }
}
