using GeoSit.Data.BusinessEntities.DeclaracionesJuradas;
using System.Linq;

namespace GeoSit.Data.DAL.Valuaciones.Validators
{
    public class DDJJValidator : IDDJJValidator
    {
        public DDJJ DDJJ { get; private set; }

        public string[] Errors { get; private set; }

        public DDJJValidator(DDJJ ddjj)
            : this(ddjj, new string[0]) { }

        public DDJJValidator(DDJJ ddjj, string[] errors)
        {
            DDJJ = ddjj;
            Errors = errors;
        }

        public IDDJJValidator Validate(IDDJJValidation validation)
        {
            var result = validation.Validate(DDJJ);
            return new DDJJValidator(result.Item1, Errors.Concat(result.Item2).ToArray());
        }
    }
}
