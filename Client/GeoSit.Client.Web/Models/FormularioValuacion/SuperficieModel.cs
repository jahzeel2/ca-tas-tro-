using GeoSit.Client.Web.Helpers.ExtensionMethods.FormularioValuacion;
using GeoSit.Data.BusinessEntities.DeclaracionesJuradas;
using GeoSit.Data.BusinessEntities.Temporal;
using System.Collections.Generic;
using System.Linq;

namespace GeoSit.Client.Web.Models.FormularioValuacion
{
    public struct SuperficieModel
    {
        private const int M2_TO_HECTAREA = 10_000;

        public AptitudModel Aptitud { get; set; }
        public long IdSuperficie { get; set; }
        public decimal SuperficieHa { get; set; }
        public short? TrazaDepreciable { get; set; }
        public int Puntaje { get; set; }
        public decimal PuntajeSuperficie { get; set; }
        public List<long> Caracteristicas { get; set; }


        private SuperficieModel(AptitudModel aptitud, long idSuperficie, decimal superficieHa, short? trazaDepreciable, 
                                int puntaje, decimal puntajeSuperficie, IEnumerable<long> caracteristicas)
        {
            Aptitud = aptitud;
            IdSuperficie = idSuperficie;
            SuperficieHa = superficieHa;
            TrazaDepreciable = trazaDepreciable; 
            Puntaje = puntaje;
            PuntajeSuperficie = puntajeSuperficie;
            Caracteristicas = caracteristicas.ToList();
        }

        internal static SuperficieModel FromEntity(VALSuperficie superficie, bool copia)
        {
            decimal superficieTotal = (decimal?)superficie.Superficie ?? 0m;
            decimal superficieHa = superficieTotal / M2_TO_HECTAREA;
            var caracteristicas = superficie.Caracteristicas;

            return new SuperficieModel(superficie.Aptitud.ToAptitudModel(),
                                       superficie.IdSuperficie * (copia ? -1 : 1),
                                       superficieHa,
                                       superficie.TrazaDepreciable,
                                       superficie.Puntaje,
                                       superficie.PuntajeSuperficie,
                                       caracteristicas.Select(c => c.Caracteristica.IdSorCaracteristica));
        }

        internal static SuperficieModel FromTemporal(VALSuperficieTemporal superficie)
        {
            decimal superficieTotal = (decimal?)superficie.Superficie ?? 0m;
            decimal superficieHa = superficieTotal / M2_TO_HECTAREA;
            var caracteristicas = superficie.Caracteristicas;

            return new SuperficieModel(superficie.Aptitud.ToAptitudModel(),
                                       superficie.IdSuperficie,
                                       superficieHa,
                                       superficie.TrazaDepreciable,
                                       superficie.Puntaje,
                                       superficie.PuntajeSuperficie,
                                       caracteristicas.Select(c => c.Caracteristica.IdSorCaracteristica));
        }
    }
}