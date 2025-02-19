using GeoSit.Data.BusinessEntities.DeclaracionesJuradas;
using System.Linq;

namespace GeoSit.Data.DAL.Valuaciones.Validators
{
    public class DDJJTemporalValidator : IDDJJValidator
    {
        public DDJJ DDJJ { get; private set; }

        public string[] Errors { get; private set; }

        public DDJJTemporalValidator(DDJJ ddjj)
            : this(ddjj, new string[0]) { }

        public DDJJTemporalValidator(DDJJ ddjj, string[] errors)
        {
            DDJJ = ddjj;
            Errors = errors;
        }

        public IDDJJValidator Validate(IDDJJValidation validation)
        {
            var result = validation.Validate(DDJJ);
            return new DDJJTemporalValidator(result.Item1, Errors.Concat(result.Item2).ToArray());
        }
    }
}
