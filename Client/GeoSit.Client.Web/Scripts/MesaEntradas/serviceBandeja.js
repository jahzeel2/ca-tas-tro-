const service = function () {
    function search(role, filters) {
        return new Promise((ok, err) => $.post(`${BASE_URL}MesaEntradas/SearchTramites`, { role, filters }, ok).fail(err));
        //return new Promise((ok, err) => $.post(`${BASE_URL}MesaEntradas/SearchTramites`, { role, filters }, (result) => {
        //    const rows = [];
        //    for (let i = 1; i <= filters.length; i++) {
        //        rows.push({
        //            IdTramite: filters.start + i,
        //            Numero: `trámite ${i}`,
        //            Profesional: "Ernesto Salas",
        //            Asunto: "Registro de Mensuras Catastrales",
        //            Causa: "Mensura de Subdivisión para somerter al derecho real de Propiedad Horizontal",
        //            Estado: "Estado",
        //            Prioridad: "Prioridad",
        //            SectorActual: "Sector",
        //            FechaUltimaActualizacion: "07/03/2024 16:55"
        //        });
        //    }
        //    result.data = rows;
        //    result.recordsTotal = 114;
        //    result.recordsFiltered = 114;
        //    ok(result);
        //}).fail(err));
    }
    function getCausas(asunto) {
        return new Promise((ok, err) => $.getJSON(`${BASE_URL}MesaEntradas/GetCausas?idAsunto=${asunto}&bandeja=true`, ok).fail(err));
    }
    function getAvailableActions(role, selection) {
        return new Promise((ok, err) => $.post(`${BASE_URL}MesaEntradas/GetAvailableActions`, { role, selection }, ok).fail(err));
    }
    function executableAction(role, action, selection) {
        return new Promise((ok, err) => $.post(`${BASE_URL}MesaEntradas/ExecutableAction`, { role, action, selection },
            (result, _, response) => {
                let type = response.getResponseHeader("content-type");
                if (type) {
                    type = type.split(";")[0].split("/")[1];
                } else {
                    type = "none";
                }
                ok({ type, data: result });
            }).fail(err));
    }

    return {
        search,
        executableAction,
        getCausas,
        getAvailableActions,
    };
}
function createServiceBandeja() {
    return service();
}
