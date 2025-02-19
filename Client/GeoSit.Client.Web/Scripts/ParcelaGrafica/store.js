const store = function (service) {
    const events = {
        NO_SELECTED_DATA: "no-selected-data",
        SELECTED_DATA_WITH_GRAPHIC: "selected-data-with-graphic",
        SELECTED_DATA_WITHOUT_GRAPHIC: "selected-data-without-graphic",
    };
    let selectedParcela, subscriptions = {};

    function hasGraphics(parcela) {
        return parcela.featids && parcela.featids.length;
    }
    function link(featid) {
        return service.link(featid, selectedParcela);
    }
    function unlink(featid) {
        return service.unlink(featid, selectedParcela);
    }
    function initializeMap(mapDiv) {
        return new MapaController(3, mapDiv, true, false, true, false, true);
    }
    function selectParcela(parcela) {
        selectedParcela = parcela;
        const evt = hasGraphics(selectedParcela) ? events.SELECTED_DATA_WITH_GRAPHIC : events.SELECTED_DATA_WITHOUT_GRAPHIC;
        for (let sub of subscriptions[evt]) {
            sub(selectedParcela.featids, selectedParcela.capa);
        }
    }
    function unselectParcela() {
        selectedParcela = null;
        for (let sub of subscriptions[events.NO_SELECTED_DATA]) {
            sub();
        }
    }
    function search(form) {
        unselectParcela();
        return service.search(form);
    }
    function locate(name) {
        return new Promise((ok, err) => setTimeout(ok, 2000));
    }
    function loadMantenedor(parcela) {
        return service.loadMantenedor(parcela);
    }
    function on(notification, fn) {
        const subscription = Object.assign(Object.defineProperty({}, notification, { value: [], writable: true }), subscriptions);
        subscriptions[notification] = subscription[notification].concat(fn);
    }

    return {
        link,
        unlink,
        search,
        locate,
        initializeMap,
        selectParcela,
        unselectParcela,
        loadMantenedor,
        hasGraphics,
        onNoSelection: on.bind(null, events.NO_SELECTED_DATA),
        onSelectedWithoutGraphic: on.bind(null, events.SELECTED_DATA_WITHOUT_GRAPHIC),
        onSelectedWithGraphic: on.bind(null, events.SELECTED_DATA_WITH_GRAPHIC),
    }
};
function createStore(service) {
    return store(service);
};