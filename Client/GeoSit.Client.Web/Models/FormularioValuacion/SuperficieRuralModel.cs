using System;

namespace GeoSit.Client.Web.Models.FormularioValuacion
{
    public struct SuperficieRuralModel
    {
        public decimal SuperficieTotalM2 { get; set; }
        public short Hectarea { get; set; } // 10_000 mts2
        public short Area { get; set; } // 100 mts2
        public short M2 { get; set; }
        public short Dm2 { get; set; } // 0.01 mts2
        public short Cm2 { get; set; } // 0.0001 mts2

        private const short EQUIVALENCIA_HECTAREA_M2 = 10_000;
        private const short EQUIVALENCIA_AREA_M2 = 100;
        private const short EQUIVALENCIA_M2_DM2 = 100;
        private const short EQUIVALENCIA_M2_CM2 = 10_000;

        private SuperficieRuralModel(decimal superficieM2, short ha, short area, short m2, short dm2, short cm2)
        {
            SuperficieTotalM2 = superficieM2;
            Hectarea = ha;
            Area = area;
            M2 = m2;
            Dm2 = dm2;
            Cm2 = cm2;
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

        public static SuperficieRuralModel Create(double? superficie)
            => Create(Convert.ToDecimal(superficie ?? 0));
        public static SuperficieRuralModel Create(decimal superficie)
        {
            decimal sanitizedSuperficie = Math.Max(superficie, 0);
            return new SuperficieRuralModel(sanitizedSuperficie,
                ExtractHa(sanitizedSuperficie),
                ExtractArea(sanitizedSuperficie),
                ExtractM2(sanitizedSuperficie),
                ExtractDm2(sanitizedSuperficie),
                ExtractCm2(sanitizedSuperficie));
        }

        public static SuperficieRuralModel operator +(SuperficieRuralModel a, SuperficieRuralModel b)
            => Create(a.SuperficieTotalM2 + b.SuperficieTotalM2);

        public static Tuple<SuperficieRuralModel, int> operator -(SuperficieRuralModel a, SuperficieRuralModel b)
        {
            int signo = Math.Sign(a.SuperficieTotalM2 - b.SuperficieTotalM2);
            var diferencia = Create(Math.Max(a.SuperficieTotalM2, b.SuperficieTotalM2) - Math.Min(a.SuperficieTotalM2, b.SuperficieTotalM2));

            return Tuple.Create(diferencia, signo);
        }
    }
}