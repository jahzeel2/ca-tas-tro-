using System;
using System.Collections.Generic;
using GeoSit.Data.BusinessEntities.DeclaracionesJuradas;
using GeoSit.Data.BusinessEntities.Inmuebles;
using GeoSit.Data.BusinessEntities.Inmuebles.DTO;
using GeoSit.Data.BusinessEntities.ObjetosAdministrativos;
using GeoSit.Data.DAL.Common.CustomErrors;

namespace GeoSit.Data.DAL.Interfaces
{
    public interface IParcelaRepository
    {
        string GetNextPartida(long idTipo, long idJurisdiccion);
        Parcela GetParcelaById(long idParcela, bool completa = true, bool utsHistoricas = false);
        Parcela GetParcelaMantenedorById(long idParcela);
        Parcela GetParcelaByFeatIdDGC(long featId);
        Zonificacion GetZonificacion(long idParcela, bool esHistorico = false);
        List<AtributosZonificacion> GetAtributosZonificacion(long idParcela);
        VALValuacion GetValuacionParcela(long idParcela, bool esHistorico = false);
        Parcela InsertParcela(Parcela parcela);
        void UpdateParcela(Parcela parcela);
        void DeleteParcela(Parcela parcela);
        IEnumerable<Objeto> GetParcelaValuacionZonas();
        Zonificacion GetZonaValuacionByIdParcela(long id);
        List<string> GetPartidabyId(long parcelaId);
        ParcelaSuperficies GetSuperficiesByIdParcela(long id, bool esHistorico = false);
        Dictionary<string, double> GetSuperficiesRuralesByIdParcela(long id);
        void RefreshVistaMaterializadaParcela();
        Dictionary<long, List<Objeto>> GetJurisdiccionesByDepartamentoParcela(long id);
        bool EsVigente(long id);
        Parcela GetParcelaByUt(long idUnidadTributaria);
        ICustomError AddGrafico(long idParcela, ParcelaGrafica grafico);
        ICustomError DeleteGrafico(long idParcela, ParcelaGrafica grafico);
        Tuple<string, ICustomError> ValidateDestino(NomenclaturaValidable nomenclatura);
        ICustomError SaveOperacionAlfanumerica(OperacionAlfanumerica operacion);
        Objeto GetEjido(long id);
        int GetSuperficieGrafica(long parcelaId);
    }
}