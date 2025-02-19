const store = function (service) {
    const events = {
        DETAIL_LOADED: "detail-loaded",
    }
    let detalle = {}, parcelas = [], mensurasRelacionadas = [], documentos = [], subscriptions = {};
    function search(params) {
        const findings = service.search(params);
        reset();
        return findings;
    }
    async function loadDetail(mensura) {
        ({ detalle, parcelas, mensurasRelacionadas, documentos } = await service.loadDetail(mensura));
        notifyDetailLoaded();
    }
    function loadMantenedor(parcela) {
        return service.loadMantenedor(parcela);
    }
    function loadDocumento(documento, readonly) {
        return service.loadDocumento(documento, readonly);
    }
    function loadBuscador(data) {
        return service.loadBuscador(data);
    }
    function showFile(documento) {
        return service.showFile(documento);
    }
    async function remove(mensura) {
        await service.remove(mensura);
        reset();
    }
    function save(mensura) {
        return service.save(mensura, parcelas, mensurasRelacionadas, documentos);
    }
    function addAsociados(listado, objetos) {
        listado.push(...objetos);
        return listado;
    }
    function removeAsociados(listado, objeto) {
        listado.splice(listado.indexOf(objeto), 1);
        return listado;
    }
    function updateAsociados(listado, actual, nuevo) {
        listado.splice(listado.indexOf(actual), 1, nuevo);
        return listado;
    }
    function reset() {
        detalle = {};
        parcelas = [];
        mensurasRelacionadas = [];
        documentos = [];
        notifyDetailLoaded();
    }
    function notifyDetailLoaded() {
        for (let sub of subscriptions[events.DETAIL_LOADED]) {
            sub({ detalle, parcelas, mensurasRelacionadas, documentos });
        }
    }
    function on(notification, fn) {
        const subscription = Object.assign(Object.defineProperty({}, notification, { value: [], writable: true }), subscriptions);
        subscriptions[notification] = subscription[notification].concat(fn);
    }
    return {
        search,
        reset,
        save,
        loadDetail,
        loadMantenedor,
        loadDocumento,
        loadBuscador,
        showFile,
        addParcelas: (nuevas) => addAsociados(parcelas, nuevas),
        addMensurasRelacionadas: (nuevas) => addAsociados(mensurasRelacionadas, nuevas),
        addDocumento: (documento) => addAsociados(documentos, [documento]),
        remove,
        removeParcela: (parcela) => removeAsociados(parcelas, parcela),
        removeMensuraRelacionada: (mensura) => removeAsociados(mensurasRelacionadas, mensura),
        removeDocumento: (documento) => removeAsociados(documentos, documento),
        updateDocumento: (actual, nuevo) => updateAsociados(documentos, actual, nuevo),
        onDetailLoaded: on.bind(null, events.DETAIL_LOADED)
    }
};
function createStore(service) {
    return store(service);
};