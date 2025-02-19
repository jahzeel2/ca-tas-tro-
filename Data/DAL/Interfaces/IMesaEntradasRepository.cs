using GeoSit.Data.BusinessEntities.Common;
using GeoSit.Data.BusinessEntities.MesaEntradas;
using GeoSit.Data.BusinessEntities.MesaEntradas.DTO;
using GeoSit.Data.BusinessEntities.Seguridad;
using GeoSit.Data.BusinessEntities.Temporal;
using System.Collections.Generic;
using static GeoSit.Data.DAL.Repositories.MesaEntradasRepository;

namespace GeoSit.Data.DAL.Interfaces
{
    public interface IMesaEntradasRepository
    {
        METramite GetTramite(int id, bool incluirEntradas);
        List<METipoTramite> GetTiposTramite();
        List<MEObjetoTramite> GetObjetosTramiteByTipo(long idTipoTramite);
        List<MEEstadoTramite> GetEstadosTramite();
        List<MEPrioridadTramite> GetPrioridadesTramite();
        DataTableResult<GrillaTramite> RecuperarTramites(Grilla grilla, DataTableParameters parametros, long idUsuario);
        AccionesTramites RecuperarAccionesDisponibles(Grilla grilla, int[] tramites, long idUsuario);
        AccionesTramites RecuperarAccionesTramite(int tramite, long idTipoTramite, long idUsuario, bool soloLectura = true);
        List<MEDatosEspecificos> GenerarDatoEspecificoOrigen(short tipo, long[] ids);
        List<MEDatosEspecificos> GenerarDatosEspecificosDestino(GeneradorParcelas generador);
        List<MEDatosEspecificos> GenerarDatosEspecificosDestino(GeneradorPartidas generador);
        List<MEDatosEspecificos> GetDatosDestinoTramite(int tramite);
        List<MEDatosEspecificos> GetDatosOrigenTramite(int tramite);
        bool ValidarDisponibilidadAccion(Grilla grilla, long accion, int[] tramites, long idUsuario);
        int TramiteSave(METramiteParameters tramiteParameters);
        int TramiteSaveInforme(METramiteParameters tramiteParameters);
        int TramiteSaveVersionInforme(bool firmado, METramiteParameters tramiteParameters);
        void AsignarTramites(Asignacion asignacion);
        void DerivarTramites(Derivacion derivacion);
        void ObservarTramite(Observacion observacion);
        int GenerarAntecedentes(METramiteParameters tramiteParameters);
        bool TieneAntecedentesGenerados(int id);
        int SolicitarReservas(METramiteParameters parameters);
        int ConfirmarReservas(METramiteParameters parametros);
        VALValuacionTemporal ObtenerValuacion(int tramite, long ut);
        List<VALSuperficieTemporal> ObtenerSuperficiesValuacion(long idValuacion);
        void ExecuteAction(METramite[] tramites, long action);
        Usuarios GetOperadorTramite(int id);
    }
}
