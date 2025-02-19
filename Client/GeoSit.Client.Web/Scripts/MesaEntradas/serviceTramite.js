const service = function () {
    function askReservas(tramite, origenes, destinos, documentos) {
        return new Promise((ok, err) => {
            $.post(`${BASE_URL}mesaentradas/SolicitarReservas`, {
                tramite,
                documentos,
                datosOrigen: origenes,
                datosDestino: destinos,
            }, ok).fail(err);
        });
    }
    function confirmReservas(tramite, origenes, destinos, documentos) {
        return new Promise((ok, err) => {
            $.post(`${BASE_URL}mesaentradas/confirmarReservas`, {
                tramite,
                documentos,
                datosOrigen: origenes,
                datosDestino: destinos,
            }, ok).fail(err);
        });
    }
    function loadAdminPersonas() {
        return new Promise((ok, err) => $.get(`${BASE_URL}Persona/BuscadorPersona`, ok, "html").fail(err));
    }
    function loadBuscador(data) {
        return new Promise((ok, err) => $.post(`${BASE_URL}BuscadorGenerico`, data, ok, "html").fail(err));
    }
    function loadDatosDestino() {
        return new Promise((ok, err) => $.getJSON(`${BASE_URL}MesaEntradas/LoadDatosDestino`, ok).fail(err));
    }
    function loadDatosOrigen() {
        return new Promise((ok, err) => $.getJSON(`${BASE_URL}MesaEntradas/LoadDatosOrigen`, ok).fail(err));
    }
    function loadDatosEspecificos(tipo, ids) {
        return new Promise((ok, err) => $.post(`${BASE_URL}MesaEntradas/DatosEspecificosOrigen`, { tipo, ids }, ok).fail(err));
    }
    function loadFormValuacion({ IdTramite }, { Propiedades }) {
        const id = Propiedades.find(p => p.Id === "KeyIdUnidadTributaria").Value,
            superficies = Propiedades.find(p => p.Id === "KeyValuacion") || null;

        return new Promise((ok, err) => $.post(`${BASE_URL}MesaEntradas/LoadFormularioValuacion`, { idTramite: IdTramite, idUT: id, superficies }, ok, "html").fail(err));
    }
    function loadGeneradorDestinos({ IdTramite, IdObjetoTramite }) {
        return new Promise((ok, err) => $.post(`${BASE_URL}MesaEntradas/LoadGenerador`, { idTramite: IdTramite, idObjetoTramite: IdObjetoTramite }, ok, "html").fail(err));
    }
    function loadNotas() {
        return new Promise((ok, err) => $.getJSON(`${BASE_URL}MesaEntradas/LoadNotas`, ok).fail(err));
    }
    function loadMovimientos() {
        return new Promise((ok, err) => $.getJSON(`${BASE_URL}MesaEntradas/LoadMovimientos`, ok).fail(err));
    }
    function loadObservacion({ IdTramite }) {
        return new Promise((ok, err) => $.post(`${BASE_URL}MesaEntradas/LoadObservacionForm`, { tramite: IdTramite }, ok, "html").fail(err));
    }
    function loadSelectorSector({ IdTramite }) {
        return new Promise((ok, err) => $.post(`${BASE_URL}MesaEntradas/LoadDerivacionForm`, { tramites: [IdTramite] }, ok, "html").fail(err));
    }
    function loadSelectorUsuario({ IdTramite }) {
        return new Promise((ok, err) => $.post(`${BASE_URL}MesaEntradas/LoadReasignacionForm`, { tramite: IdTramite }, ok, "html").fail(err));
    }
    function downloadFile({ IdTramite }, filename) {
        return new Promise((ok, err) => {
            $.get(`${BASE_URL}MesaEntradas/ExisteArchivo`, { tramite: IdTramite, archivo: btoa(filename) }, () => {
                window.location = `${BASE_URL}MesaEntradas/DownloadArchivo`;
                ok();
            }).fail(err)
        });
    }
    function openHojaRuta({ IdTramite }) {
        return new Promise((ok, err) => {
            $.get(`${BASE_URL}MesaEntradas/GetInformeHojaDeRuta/${IdTramite}`, () => {
                window.open(`${BASE_URL}MesaEntradas/DownloadArchivo`, "_blank");
                ok();
            }).fail(err)
        });
    }
    function execute({ IdTramite }, action) {
        return new Promise((ok, err) => $.post(`${BASE_URL}MesaEntradas/ExecuteDirectAction`, { idTramite: IdTramite, idAction: action }, ok).fail(err));
    }
    function getAcciones({ IdTramite, IdTipoTramite }) {
        return new Promise((ok, err) => $.get(`${BASE_URL}MesaEntradas/GetTramiteActions?idTramite=${IdTramite}&idAsunto=${IdTipoTramite}`, ok, "html").fail(err));
    }
    function getCausas({ IdTipoTramite }) {
        return new Promise((ok, err) => $.getJSON(`${BASE_URL}MesaEntradas/GetCausas?idAsunto=${IdTipoTramite}`, ok).fail(err));
    }
    function reload(soloLectura = true) {
        return new Promise((ok, err) => $.post(`${BASE_URL}MesaEntradas/ReloadTramite`, { soloLectura }, ok).fail(err));
    }
    function save(ingresar, tramite, origenes, destinos, documentos) {
        return new Promise((ok, err) => {
            $.post(`${BASE_URL}mesaentradas/tramitesave`, {
                tramite,
                documentos,
                datosOrigen: origenes,
                datosDestino: destinos,
                ingresar,
            }, ok).fail(err);
        });
    }
    function saveAntecedentes(tramite, origenes, destinos, documentos) {
        return new Promise((ok, err) => {
            $.post(`${BASE_URL}mesaentradas/generarantecedentes`, {
                tramite,
                documentos,
                datosOrigen: origenes,
                datosDestino: destinos,
            }, ok).fail(err);
        });
    }
    function saveInforme({ IdTramite }, informe, nuevo, firmado) {
        return new Promise((ok, err) => {
            debugger
            const ep = nuevo && "tramitesaveinforme" || "tramiteupdateinforme",
                firmaParam = !nuevo && { firmado: firmado } || {};
            
            $.post(`${BASE_URL}mesaentradas/${ep}`,{
                idTramite: IdTramite,
                informe,
                ...firmaParam
            } , ok).fail(err);
        });
    }
    function setTramiteCurrentUser({ IdTramite }) {
        return new Promise((ok, err) => {
            $.post(`${BASE_URL}MesaEntradas/Recibir`, { tramites: [IdTramite] })
                .done(({ error, mensajes }) => {
                    ((error, mensajes) => {
                        if (error) {
                            err(mensajes);
                            return;
                        }
                        ok();
                    })(error, mensajes);
                }).fail(err.bind(null, ["Ha ocurrido un error al recepcionar el trámite."]));
        });
    }
    function uploadFile(tramite, file) {
        return new Promise((ok, error) => {
            let data = new FormData();
            data.append("idTramite", tramite.IdTramite);
            data.append("file", file);
            $.ajax({
                type: "POST",
                url: `${BASE_URL}MesaEntradas/UploadDocumento`,
                processData: false,
                contentType: false,
                data,
                success: data => ok(data.nombreArchivo),
                error
            });
        });
    }
    return {
        askReservas,
        confirmReservas,
        downloadFile,
        execute,
        getAcciones,
        getCausas,
        loadAdminPersonas,
        loadBuscador,
        loadDatosDestino,
        loadDatosEspecificos,
        loadDatosOrigen,
        loadFormValuacion,
        loadGeneradorDestinos,
        loadNotas,
        loadMovimientos,
        loadObservacion,
        loadSelectorSector,
        loadSelectorUsuario,
        openHojaRuta,

        reload,
        save,
        saveAntecedentes,
        saveInforme,
        setTramiteCurrentUser,
        uploadFile,
    };
}
function createServiceTramite() {
    return service();
}
