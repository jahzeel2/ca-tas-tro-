using GeoSit.Data.BusinessEntities.ILogicInterfaces;
using GeoSit.Data.BusinessEntities.Personas;
using GeoSit.Data.BusinessEntities.Seguridad;
using System;
using System.Collections.Generic;

namespace GeoSit.Data.BusinessEntities.MesaEntradas
{
    public class METramite : IEntity, IBajaLogica
    {
        public int IdTramite { get; set; }
        public string Numero { get; set; }
        public int? IdSGT { get; set; }
        public int IdPrioridad { get; set; }
        public DateTime FechaInicio { get; set; }
        public DateTime? FechaIngreso { get; set; }
        public DateTime? FechaLibro { get; set; }
        public DateTime? FechaVencimiento { get; set; }
        public int IdTipoTramite { get; set; }
        public int IdObjetoTramite { get; set; }
        public string Motivo { get; set; }
        public int IdEstado { get; set; }
        public long IdIniciador { get; set; }
        public string Comprobante { get; set; }
        public decimal Monto { get; set; }
        public string Plano { get; set; }
        public long UsuarioAlta { get; set; }
        public DateTime FechaAlta { get; set; }
        public long UsuarioModif { get; set; }
        public DateTime FechaModif { get; set; }
        public long? UsuarioBaja { get; set; }
        public DateTime? FechaBaja { get; set; }


        public MEPrioridadTramite Prioridad { get; set; }
        public METipoTramite Tipo { get; set; }
        public MEObjetoTramite Objeto { get; set; }
        public MEEstadoTramite Estado { get; set; }
        public Usuarios Profesional { get; set; }
        public Usuarios UltimoOperador { get; set; }
        public Persona Iniciador { get; set; }

        public ICollection<MEMovimiento> Movimientos { get; set; }
        public ICollection<METramiteRequisito> TramiteRequisitos { get; set; }
        public ICollection<METramiteDocumento> TramiteDocumentos { get; set; }
        public ICollection<MEDesglose> Desgloses { get; set; }
        public ICollection<METramiteEntrada> TramiteEntradas { get; set; }
    }
}
