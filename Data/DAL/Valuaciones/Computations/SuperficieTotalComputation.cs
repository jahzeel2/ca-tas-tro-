using GeoSit.Data.BusinessEntities.DeclaracionesJuradas;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GeoSit.Data.DAL.Valuaciones.Computations
{
    internal class SuperficieTotalComputation
    {
        private readonly Func<VALSuperficie, double> transform;

        public SuperficieTotalComputation() : this(false) { }
        public SuperficieTotalComputation(bool applyTransformationHa)
        {
            transform = applyTransformationHa ? HaToM2Transformation() : NoTransformation();
        }

        public decimal SumHa(IEnumerable<VALSuperficie> superficies)
        {
            return Math.Round(Sum(superficies) / 10_000, 8);
        }
        
        public decimal SumM2(IEnumerable<VALSuperficie> superficies)
        {
            decimal sum = Sum(superficies);
            return Math.Round(sum, 4);
        }

        private decimal Sum(IEnumerable<VALSuperficie> superficies)
        {
            return Convert.ToDecimal(superficies.Sum(transform));
        }
        private Func<VALSuperficie, double> NoTransformation()
        {
            return (superficie) => superficie.Superficie ?? 0;
        }
        private Func<VALSuperficie, double> HaToM2Transformation()
        {
            return (superficie) => (superficie.Superficie ?? 0) * 10000;
        }
    }
}
