using GeoSit.Data.BusinessEntities.DeclaracionesJuradas;
using GeoSit.Data.BusinessEntities.Inmuebles;
using GeoSit.Data.BusinessEntities.MesaEntradas;
using System.Collections.Generic;
using Geosit.Data.DAL.DDJJyValuaciones.Enums;
using GeoSit.Data.BusinessEntities.Designaciones;
using GeoSit.Data.BusinessEntities.ObjetosAdministrativos;
using System.Threading.Tasks;
using GeoSit.Data.BusinessEntities.Temporal;

namespace GeoSit.Data.DAL.Interfaces
{
    public interface IDeclaracionJuradaRepository
    {
        IEnumerable<DDJJVersion> GetVersiones();

        DDJJVersion GetVersion(int idVersion);
        
        Designacion GetDesignacionByUt(long idUnidadTributaria);

        IEnumerable<DDJJSorTipoCaracteristica> GetSorTipoCaracteristicas(long idAptitud);

        IEnumerable<DDJJDominio> GetDominios(long idDeclaracionJurada);
        
        IEnumerable<DDJJDominio> GetDominiosByIdUnidadTributaria(long idUT);

        IEnumerable<TipoTitularidad> GetTiposTitularidad();

        IEnumerable<DDJJSorOtrasCar> GetSorOtrasCar(int idVersion);

        DDJJ GetDeclaracionJurada(long idDeclaracionJurada);

        DDJJDesignacion GetDDJJDesignacion(long idDeclaracionJurada);

        bool SaveDDJJSor(DDJJ ddjj, DDJJSor ddjjSor, DDJJDesignacion ddjjDesignacion, List<DDJJDominio> dominios, List<DDJJSorCar> sorCar, List<VALSuperficie> superficies, long idUsuario, string machineName, string ip);
        bool AptitudCompleta(long idAptitud, long[] caracteristicas);
        VALSuperficie AptitudPuntaje(long idAptitud, VALSuperficie superficie);

        Mensura GetMensura(int idMensura);

        List<DDJJ> GetDeclaracionesJuradas(long idUnidadTributaria);

        List<DDJJ> GetDeclaracionesJuradasNoVigentes(long idUnidadTributaria);

        Task<DatosComputo> CalcularValuacionAsync(long idUnidadTributaria, VALSuperficie[] superficies);
        
        DDJJ GetDeclaracionJuradaVigenteSoR(long idUnidadTributaria);//

        IEnumerable<OCObjeto> GetOCObjetos(int idSubtipoObjeto);

        List<VALAptitudes> GetAptitudes(int? idVersion = null);
        
        List<DDJJSorCaracteristicas> GetCaracteristicas();

        List<DDJJSorCar> GetSorCar(long idSor);

        List<VALSuperficie> GetValSuperficies(long idDDJJ);

        List<VALValuacion> GetValuaciones(long idUnidadTributaria);

        List<VALValuacion> GetValuacionesHistoricas(long idUnidadTributaria);

        VALValuacion GetValuacion(long idValuacion);

        bool DeleteValuacion(long idValuacion, long idUsuario);

        VALValuacion GetValuacionVigente(long idUnidadTributaria);

        bool SaveValuacion(VALValuacion valuacion, long idUsuario);

        VALDecreto GetDecretoByNumero(long nroDecreto);

        List<DDJJPersonaDomicilio> GetPersonaDomicilios(long idPersona);

        bool GenerarValuacion(DDJJ ddjj, long idUnidadTributaria, TipoValuacionEnum tipoValuacion, long idUsuario, string machineName, string ip);

        Objeto GetOAObjetoPorIdLocalidad(long idLocalidad);

        DDJJ GetDeclaracionJuradaCompleta(long idDeclaracionJurada);

        VALValuacion GetValuacionVigenteConsolidada(long idParcela, bool esHistorico = false);

        bool Revaluar(long idUnidadTributaria, long idUsuario, string machineName, string ip);

        long GetIdDepartamentoByCodigo(string codigo);

        Task<bool> SaveFormularioAsync(long idUnidadTributaria, DatosFormulario formulario);

        METramite GetTramite(long idDeclaracionJurada);

        IEnumerable<VALValuacionTempCorrida> GetValuacionesTmpCorrida();

        List<VALValuacionTempDepto> GetValuacionesTmpDepto(int corrida);

        bool EliminarCorridaTemporal(int corrida, long usuarioModificacionID);

        bool GenerarValuacionTemporal(long usuario);

        bool PasarValuacionTmpProduccion(int corrida, long usuario);
    }
}
