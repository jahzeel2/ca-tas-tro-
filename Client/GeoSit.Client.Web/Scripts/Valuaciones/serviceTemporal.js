const serviceTemporal = function (dlg, { calcPuntaje, evalCaracteristicasByAptitud, loadDetail, refreshFormattedSuperficies }) {
    const loadInitializationData = function () {
        return new Promise((ok, err) => $.getJSON(`${BASE_URL}valuaciones/getcurrentsuperficiestemporales`, ok).fail(err));
    };
    const loadPreview = function (idUnidadTributaria, superficies) {
        return new Promise((ok, err) => $.post(`${BASE_URL}valuaciones/previewtemporal/`, { idUnidadTributaria, superficies }, (resp) => {
            if (typeof resp === "string") {
                ok(resp);
            } else {
                err(resp.error);
            }
        }).fail(err));
    };
    const save = function (idUnidadTributaria, superficies) {
        $(dlg).trigger({
            type: "valuacionGenerada",
            valuacion: {
                idUnidadTributaria,
                superficies
            }
        });
        return Promise.resolve();
    };

    return {
        calcPuntaje,
        esTemporal: true,
        evalCaracteristicasByAptitud,
        loadInitializationData,
        loadDetail,
        loadPreview,
        refreshFormattedSuperficies,
        save,
    };
}
function createServiceTemporal(dlg, baseService) {
    return serviceTemporal(dlg, baseService);
}