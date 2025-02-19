using GeoSit.Data.BusinessEntities.LogicalTransactionUnits;
using GeoSit.Data.DAL.Common.CustomErrors;
using GeoSit.Data.DAL.Common.CustomErrors.OperacionesParcelarias;
using GeoSit.Data.DAL.OperacionesParcelarias.Generators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeoSit.Data.DAL.OperacionesParcelarias
{
    public class Generator
    {
        public static Tuple<IOperacionParcelaria, ICustomError> Create(UnidadAlfanumericoParcela uap)
        {
            var tiposOperacion = new IOperacionParcelaria[]
            {
                OperacionCreacion.Create(uap),
                OperacionRedistribucion.Create(uap),
                OperacionSubdivision.Create(uap),
                OperacionUnificacion.Create(uap),
            };
            IOperacionParcelaria operacion = tiposOperacion.SingleOrDefault(op => op != null);
            ICustomError err = operacion == null ? new OperacionParcelariaInvalida() : null;
            return new Tuple<IOperacionParcelaria, ICustomError>(operacion, err);
        }
    }
}
