const calcPuntaje = function ({ Aptitud, SuperficieHa, TrazaDepreciable }, caracteristicas, nuevaSuperficieHa, nuevaTrazaDepreciable) {
    const data = {
        aptitud: Aptitud.IdAptitud,
        superficie: nuevaSuperficieHa || SuperficieHa,
        trazaDepreciable: nuevaTrazaDepreciable || TrazaDepreciable,
        caracteristicas,
    };
    return new Promise((ok, err) => $.post(`${BASE_URL}valuaciones/calcularpuntaje`, data, ok).fail(err));
}
const loadInitializationData = function (idDDJJ) {
    return new Promise((ok, err) => $.getJSON(`${BASE_URL}valuaciones/GetSuperficies/${idDDJJ}`, ok).fail(err));
};
const loadDetail = function (superficie, editable, editaMaestras) {
    const data = { idAptitud: superficie.Aptitud.IdAptitud, editable, editaMaestras };
    return new Promise((ok, err) => $.post(`${BASE_URL}valuaciones/getcaracteristicas/`, data, ok).fail(err));
};
const refreshFormattedSuperficies = function (superficieParcela, superficieValuada) {
    const data = { superficieParcela, superficieValuada };
    return new Promise((ok, err) => $.get(`${BASE_URL}valuaciones/getformattedsuperficies/`, data, ok).fail(err));
};
const evalCaracteristicasByAptitud = function ({ IdAptitud }, caracteristicas) {
    return new Promise((ok, err) => $.post(`${BASE_URL}valuaciones/evalCaracteristicasByAptitud/`, { aptitud: IdAptitud, caracteristicas }, ok, "json").fail(err));
};
const loadPreview = function (idUnidadTributaria, superficies) {
    return new Promise((ok, err) => $.post(`${BASE_URL}valuaciones/preview/`, { idUnidadTributaria, superficies }, (resp) => {
        if (typeof resp === "string") {
            ok(resp);
        } else {
            err(resp.error);
        }
    }).fail(err));
};
const save = function (idUnidadTributaria, superficies) {
    return new Promise((ok, err) => $.post(`${BASE_URL}valuaciones/save/`, { idUnidadTributaria, superficies }, ok).fail(err));
};

function createService() {
    return {
        calcPuntaje,
        evalCaracteristicasByAptitud,
        loadInitializationData,
        loadDetail,
        loadPreview,
        refreshFormattedSuperficies,
        save,
    };
}