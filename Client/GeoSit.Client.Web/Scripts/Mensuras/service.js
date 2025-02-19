const service = function () {
    function search(params) {
        return new Promise((ok, err) => {
            $.ajax({
                type: 'POST',
                url: `${BASE_URL}Mensura/GetMensurasJson`,
                data: JSON.stringify(params),
                success: ok,
                error: err,
                contentType: "application/json",
                dataType: 'json'
            });
        });
    }
    function remove(mensura) {
        return new Promise((ok, err) => {
            $.post(`${BASE_URL}Mensura/DeleteMensuraJson/`, { id: mensura.IdMensura }, ok).fail(err);
        });
    }
    function save(mensura, parcelas, mensurasRelacionadas, documentos) {
        return new Promise((ok, err) => {
            const parcelasId = parcelas.map(p => p.IdParcela),
                documentosId = documentos.map(d => d.IdDocumento),
                mensurasOrigenId = mensurasRelacionadas.filter(m => m.IdMensuraOrigen).map(m => m.IdMensuraOrigen),
                mensurasDestinoId = mensurasRelacionadas.filter(m => m.IdMensuraDestino).map(m => m.IdMensuraDestino);

            $.post(`${BASE_URL}Mensura/Save_DatosMensura/`, {
                mensura,
                parcelas: parcelasId,
                mensurasOrigen: mensurasOrigenId,
                mensurasDestino: mensurasDestinoId,
                documentos: documentosId
            }, ok).fail(err);
        });
    }
    function loadDetail(mensura) {
        return new Promise((ok, err) => {
            $.getJSON(`${BASE_URL}Mensura/GetDatosMensuraJson/${mensura.IdMensura}`, ok).fail(err);
        });
    }
    function loadMantenedor(parcela) {
        //return new Promise((ok, err) => $.get(`${BASE_URL}MantenimientoParcelario/Get/${parcela.IdParcela}`, ok, "html").fail(err));
        return new Promise((ok, err) => {$.get(`${BASE_URL}MantenimientoParcelario/GetMantenedorParcelarioView/${parcela.IdParcela}?UnidadTributariaId=0`, ok, "html").fail(err);});
    }
    async function loadDocumento(documento, readonly) {
        if (!readonly) {
            await new Promise((ok, err) => $.post(`${BASE_URL}Documento/Editable`, null, ok).fail(err));
        }
        return new Promise((ok, err) => {
            const id = Object.assign({ IdDocumento: 0 }, documento).IdDocumento;
            $.get(`${BASE_URL}Documento/DatosDocumento/${id}`, ok, "html").fail(err);
        });
    }
    function showFile(documento) {
        return new Promise((ok, err) => {
            $.get(`${BASE_URL}PdfInternalViewer/View/${documento.IdDocumento}`, ok, "html").fail(err);
        });
    }
    function loadBuscador(data) {
        return new Promise((ok, err) => $.post(`${BASE_URL}BuscadorGenerico`, data, ok, "html").fail(err));
    }
    return {
        loadDetail,
        loadMantenedor,
        loadDocumento,
        loadBuscador,
        remove,
        save,
        search,
        showFile,
    }
};
function createService() {
    return service();
}
