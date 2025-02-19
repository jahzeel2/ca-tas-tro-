using GeoSit.Data.BusinessEntities.GlobalResources;
using System;
using System.Linq;

namespace GeoSit.Data.BusinessEntities.MesaEntradas.DTO
{
    public class ResumenTramite
    {
        public int IdTramite { get; set; }
        public string Tipo { get; set; }
        public string Objeto { get; set; }
        public string Prioridad { get; set; }
        public string NombreApellidoProfesional { get; set; }
        public string CuilProfesional { get; set; }
        public DateTime FechaIngreso { get; set; }
        public bool TipoPlano { get; set; }
        public string Plano { get; set; }
        public ResumenTramiteOrigen[] Origenes { get; set; }

        internal ResumenTramite() { }

        private ResumenTramite(int idTramite, string tipo, string objeto, string prioridad, string profesional, string cuil,
                                DateTime ingreso, bool esPlano, string plano, ResumenTramiteOrigen[] origenes)
        {
            IdTramite = idTramite;
            Tipo = tipo;
            TipoPlano = esPlano;
            Objeto = objeto;
            Prioridad = prioridad;
            NombreApellidoProfesional = profesional;
            CuilProfesional = cuil;
            FechaIngreso = ingreso;
            Origenes = origenes;
            Plano = plano;
        }

        public static ResumenTramite Create(METramite tramite, MEDatosEspecificos[] datosOrigen)
        {
            bool esPlano = tramite.IdObjetoTramite == Convert.ToInt32(ObjetosTramites.PlanoAprobado) 
                            || tramite.IdObjetoTramite == Convert.ToInt32(ObjetosTramites.EscaneoPlanoMensura);
            var origenes = new ResumenTramiteOrigen[0];
            string plano = string.Empty;

            datosOrigen = datosOrigen ?? new MEDatosEspecificos[0];
            if (esPlano)
            {
                plano = datosOrigen.First().Propiedades.Where(p => p.Id == KeysDatosEspecificos.KeyIdMensura).Single().Text;
            }
            else
            {
                origenes = datosOrigen.Select(ResumenTramiteOrigen.Create).ToArray();
            }
            return new ResumenTramite(tramite.IdTramite, tramite.Tipo.Descripcion, tramite.Objeto.Descripcion,
                                      tramite.Prioridad.Descripcion, tramite.Profesional.NombreApellidoCompleto,
                                      tramite.Profesional.CUIL, tramite.FechaIngreso.Value.Date, esPlano, plano, origenes);
        }
    }
}
