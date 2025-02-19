using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace GeoSit.Reportes.Api.Models
{
    public class SuperficieModel
    {
        private const short EQUIVALENCIA_HECTAREA_M2 = 10_000;
        private const short EQUIVALENCIA_AREA_M2 = 100;
        private const short EQUIVALENCIA_M2_DM2 = 100;
        private const short EQUIVALENCIA_M2_CM2 = 10_000;

        public static (short Hectarea, short Area, short M2, short Dm2, short Cm2) DescomponerSuperficie(decimal superficieM2)
        {
            short hectareas = ExtractHa(superficieM2);
            short areas = ExtractArea(superficieM2);
            short metrosCuadrados = ExtractM2(superficieM2);
            short decimetrosCuadrados = ExtractDm2(superficieM2);
            short centimetrosCuadrados = ExtractCm2(superficieM2);

            return (hectareas, areas, metrosCuadrados, decimetrosCuadrados, centimetrosCuadrados);
        }

        private static short ExtractHa(decimal superficieM2)
        {
            return Convert.ToInt16(Math.Floor(superficieM2 / EQUIVALENCIA_HECTAREA_M2));
        }

        private static short ExtractArea(decimal superficieM2)
        {
            return Convert.ToInt16(Math.Floor(superficieM2 % EQUIVALENCIA_HECTAREA_M2 / EQUIVALENCIA_AREA_M2));
        }

        private static short ExtractM2(decimal superficieM2)
        {
            return Convert.ToInt16(Math.Floor(superficieM2 % EQUIVALENCIA_HECTAREA_M2 % EQUIVALENCIA_AREA_M2));
        }

        private static short ExtractDm2(decimal superficieM2)
        {
            return Convert.ToInt16(Math.Floor(superficieM2 % 1 * EQUIVALENCIA_M2_DM2));
        }

        private static short ExtractCm2(decimal superficieM2)
        {
            return Convert.ToInt16(Math.Floor((superficieM2 % 1 * EQUIVALENCIA_M2_CM2) % EQUIVALENCIA_M2_DM2));
        }
    }
}