using GeoSit.Data.BusinessEntities.DeclaracionesJuradas;
using GeoSit.Data.DAL.Contexts;
using GeoSit.Data.DAL.Valuaciones.Computations;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GeoSit.Data.DAL.Valuaciones.Validators.Validations
{
    public class SuperficieParcelaRuralTemporalValidation : IDDJJValidation
    { 
        private readonly GeoSITMContext _context;
        public SuperficieParcelaRuralTemporalValidation(GeoSITMContext ctx)
        {
            _context = ctx;
        }
        public Tuple<DDJJ,IEnumerable<string>> Validate(DDJJ ddjj)
        {
            var errores = new string[0];
            var parcela = (from p in _context.ParcelasTemporal
                           where (from ut in _context.UnidadesTributariasTemporal
                                  where ut.UnidadTributariaId == ddjj.IdUnidadTributaria
                                           && p.ParcelaID == ut.ParcelaID
                                  select 1).Any()
                           select p).Single();

            if (CalcSuperficie(ddjj.Sor.Superficies) != parcela.Superficie)
            {
                errores = new[] { "La superficie valuada no es igual a la superficie de la parcela." };
            }
            return Tuple.Create(ddjj, errores.AsEnumerable());
        }

        private decimal CalcSuperficie(IEnumerable<VALSuperficie> superficies)
        {
            return new SuperficieTotalComputation(true).SumM2(superficies);
        }
    }
}