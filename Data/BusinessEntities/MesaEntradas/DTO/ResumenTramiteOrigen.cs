using GeoSit.Data.BusinessEntities.GlobalResources;
using System.Collections.Generic;
using System.Linq;

namespace GeoSit.Data.BusinessEntities.MesaEntradas.DTO
{
    public class ResumenTramiteOrigen
    {
        public string Nomenclatura { get; set; }
        public string Tipo { get; set; }
        public string Partida { get; set; }

        internal ResumenTramiteOrigen() { }

        private ResumenTramiteOrigen(string nomenclatura, string partida, string tipo)
        {
            Nomenclatura = nomenclatura;
            Tipo = tipo;
            Partida = partida;
        }
        internal static ResumenTramiteOrigen Create(MEDatosEspecificos dato)
        {
            var props = dato.Propiedades;
            return new ResumenTramiteOrigen(GetTextFromProp(props, KeysDatosEspecificos.KeyIdParcela),
                                            GetTextFromProp(props, KeysDatosEspecificos.KeyIdUnidadTributaria),
                                            GetTextFromProp(props, KeysDatosEspecificos.KeyTipoParcela));
                                            
        }
        private static string GetTextFromProp(IEnumerable<Propiedad> props, string key)
        {
            return props.SingleOrDefault(prop => prop.Id == key)?.Text ?? string.Empty;
        }
    }
}
