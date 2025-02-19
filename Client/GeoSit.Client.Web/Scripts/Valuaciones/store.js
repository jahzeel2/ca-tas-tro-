const tempIdGenerator = function* (startFrom) {
    let id = startFrom || 0;
    while (true) {
        yield id--;
    }
}

const storeSuperficiesFormularioValuacion = function (service, idUnidadTributaria, idDDJJ) {
    let superficies = [],
        aptitudes = [],
        subscriptions = {};
    const generadorIds = tempIdGenerator(-Date.now()),
        events = {
            CARACTERISTICAS_UPDATED: "caracteristicas-updated",
            INITIALIZED: "initialized",
            SUPERFICIE_UPDATED: "superficies-updated",
            PUNTAJE_UPDATED: "puntajes-updated"
        };

    function add() {
        const superficie = {
            IdSuperficie: generadorIds.next().value,
            Aptitud: { IdAptitud: aptitudes[0].IdAptitud },
            SuperficieHa: 0,
            Caracteristicas: [],
            Puntaje: 0,
            PuntajeSuperficie: 0,
            TrazaDepreciable: null,
        };
        superficies = superficies.concat(superficie);
        return superficie;
    }
    async function initializeCaracteristicas(superficie, comunes, propias) {
        let emptyArray = !superficie.Caracteristicas.length,
            updated = superficie;
        if (emptyArray) {
            const defaults = superficies.indexOf(superficie) === 0 && comunes || superficies[0].Caracteristicas.slice(0, comunes.length);
            updated = await updateCaracteristicas(superficie, defaults, propias, false)
        }
        return [updated.Caracteristicas, emptyArray];
    }
    function remove(superficie) {
        superficies = superficies.filter(s => s.IdSuperficie !== superficie.IdSuperficie);
        notifyChangeOnPuntajes();
        notifyChangeOnSuperficies();
    }
    function reset(superficie, aptitud) {
        return update(superficie, { Aptitud: { IdAptitud: aptitud }, Caracteristicas: [] });
    }
    async function updateCaracteristicas(superficie, comunes, propias, modificaMaestra = false, notify = true) {
        const caracteristicas = comunes.concat(propias);
        const { puntaje, puntajeSuperficie } = await service.calcPuntaje(superficie, caracteristicas);
        const updated = update(superficie, { Caracteristicas: caracteristicas, Puntaje: puntaje, PuntajeSuperficie: puntajeSuperficie });
        let rows = [updated];
        if (modificaMaestra) {
            for (let sup of superficies.filter(s => s.IdSuperficie !== updated.IdSuperficie)) {
                const propiasSuperficie = sup.Caracteristicas.slice(comunes.length),
                    row = await updateCaracteristicas(sup, comunes, propiasSuperficie, false, false);

                rows.push(row);
            }
        }
     
        if (notify) {
            notifyChangeOnCaracteristicas(rows);
            notifyChangeOnPuntajes();
        }
        return updated;
    }
    async function updateDepreciacion(superficie, valor) {
        const { puntaje, puntajeSuperficie } = await service.calcPuntaje(superficie, superficie.Caracteristicas, null, valor);
        const updated = update(superficie, { TrazaDepreciable: valor, Puntaje: puntaje, PuntajeSuperficie: puntajeSuperficie });
        notifyChangeOnCaracteristicas(updated);
        notifyChangeOnPuntajes();
        return updated;
    }
    async function updateSuperficie(superficie, valor) {
        const { _, puntajeSuperficie } = await service.calcPuntaje(superficie, superficie.Caracteristicas, valor);
        const updated = update(superficie, { SuperficieHa: valor, PuntajeSuperficie: puntajeSuperficie });
        notifyChangeOnSuperficies();
        notifyChangeOnPuntajes();
        return updated;
    }
    function update(superficie, actualizacion) {
        const current = superficies.find(s => s.IdSuperficie === superficie.IdSuperficie),
            updated = Object.assign({}, current, actualizacion);
        superficies.splice(superficies.indexOf(current), 1, updated);
        return updated;
    }
    function isFormularioNuevo(superficies) {
        let error = [];
        if (idDDJJ > 0 && !service.esTemporal) {
            error = ["Un formulario ya guardado no puede modificarse."];
        }
        return new ValidationLogger(superficies, error);
    }
    function noMissingSuperficiesAptitudes(superficies) {
        let error = [];
        if (superficies.some(x => x.SuperficieHa <= 0)) {
            error = ["Las superficies deben ser mayor a cero."];
        }
        return new ValidationLogger(superficies, error);
    }
    function notifyChangeOnCaracteristicas(superficies) {
        const arr = Array.isArray(superficies) && superficies || [superficies];
        for (let sub of subscriptions[events.CARACTERISTICAS_UPDATED]) {
            sub(arr);
        }
    }
    function notifyInitialized(superficies, aptitudes) {
        for (let sub of subscriptions[events.INITIALIZED]) {
            sub(superficies, aptitudes);
        }
    }
    function notifyChangeOnPuntajes() {
        const maxPrecision = Math.max(...superficies.map(sup => sup.PuntajeSuperficie.countDecimals())),
            puntajeTotal = superficies.reduce((accum, sup) => accum + sup.PuntajeSuperficie, 0).round(maxPrecision);
        for (let sub of subscriptions[events.PUNTAJE_UPDATED]) {
            sub(puntajeTotal);
        }
    }
    function notifyChangeOnSuperficies() {
        const superficieTotal = superficies.reduce((accum, sup) => accum + sup.SuperficieHa, 0);
        for (let sub of subscriptions[events.SUPERFICIE_UPDATED]) {
            sub((superficieTotal * 10_000).round(4));
        }
    }
    function on(notification, fn) {
        const subscription = Object.assign(Object.defineProperty({}, notification, { value: [], writable: true }), subscriptions);
        subscriptions[notification] = subscription[notification].concat(fn);
    }
    async function noMissingCaracteristicasByAptitud(superficies) {
        let error = [];
        try {
            const proms = superficies.map(s => service.evalCaracteristicasByAptitud(s.Aptitud, s.Caracteristicas)),
                results = await Promise.all(proms);
            if (results.some(r => !r.valid)) {
                error = ["Hay aptitudes que no tienen todas las características seleccionadas."];
            }
        } catch {
            error = ["Ha ocurrido un error al validar las características para las aptitudes."];
        }
        return new ValidationLogger(superficies, error);
    }
    async function validate() {
        const { errors } = await new ValidationLogger(superficies)
            .validate(isFormularioNuevo)
            .validate(noMissingSuperficiesAptitudes)
            .validateAsync(noMissingCaracteristicasByAptitud);

        return [!errors.length, errors];
    }
    async function initialize() {
        ({ superficies, aptitudes } = await service.loadInitializationData(idDDJJ));
        notifyInitialized(superficies, aptitudes);
        notifyChangeOnPuntajes();
        notifyChangeOnSuperficies();
    }
    function loadDetail(superficie, editable) {
        return service.loadDetail(superficie, editable, superficies[0] === superficie);
    }
    function loadPreview() {
        return service.loadPreview(idUnidadTributaria, superficies);
    }
    function save() {
        return service.save(idUnidadTributaria, superficies);
    }
    function refreshResumenSuperficies(superficieParcela, superficieValuada) {
        return service.refreshFormattedSuperficies(superficieParcela, superficieValuada)
    }

    return {
        add,
        remove,
        reset,
        onCaracteristicasUpdated: on.bind(null, events.CARACTERISTICAS_UPDATED),
        onInitialized: on.bind(null, events.INITIALIZED),
        onPuntajeUpdated: on.bind(null, events.PUNTAJE_UPDATED),
        onSuperficieUpdated: on.bind(null, events.SUPERFICIE_UPDATED),
        initializeCaracteristicas,
        updateCaracteristicas,
        updateDepreciacion,
        updateSuperficie,
        validate,
        initialize,
        loadDetail,
        loadPreview,
        refreshResumenSuperficies,
        save
    };
};

function createStore(service, idUnidadTributaria, idSor) {
    return storeSuperficiesFormularioValuacion(service, idUnidadTributaria, idSor);
}

//# sourceURL=storeFormulario.js