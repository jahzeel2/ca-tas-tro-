const store = function (service) {
    const events = {
        ORIGENES_CHANGED: "origenes-changed",
        DESTINOS_CHANGED: "destinos-changed",
        OPERACION_CHANGED: "operacion-changed",
        DEPARAMENTO_CHANGED: "depto-changed",
        CIRCUNSCRIPCION_CHANGED: "circunscripcion-changed",
        RESET_DATOS_DESTINO: "reset-datos-destino",
        SOMETHING_CHANGED: "something",
        OPERACION_SAVED: "saved",
    },
        nextIdGenerator = tempIdGenerator(-1);


    let data, parcelasOrigen = [], parcelasDestino = [], subscriptions = {};
    function initData() {
        data = {
            operacion: 0,
            numeroPlano: "",
            fechaOperacion: "",
            departamento: 0,
            circunscripcion: 0,
            seccion: 0,
            fechaVigencia: "",
            tipo: 0,
            estado: 0,
        };
    }

    const notifySomethingChanged = notifyChanges.bind(null, events.SOMETHING_CHANGED);
    const notifyOrigenesChanged = notifyChanges.bind(null, events.ORIGENES_CHANGED);
    const notifyDestinosChanged = notifyChanges.bind(null, events.DESTINOS_CHANGED);
    const notifyDepartamentoChanged = notifyChanges.bind(null, events.DEPARAMENTO_CHANGED);
    const notifyCircunscripcionChanged = notifyChanges.bind(null, events.CIRCUNSCRIPCION_CHANGED);
    const notifyOperacionChanged = notifyChanges.bind(null, events.OPERACION_CHANGED);
    const notifyOperacionSaved = notifyChanges.bind(null, events.OPERACION_SAVED);

    async function updateOperacion(operacion) {
        resetParcelas();
        const { estados, claseParcela } = await service.loadDatosOperacion(operacion);
        updateData({ operacion });
        notifyOperacionChanged(operacion, estados, claseParcela);
    }
    async function updateDepartamento(departamento) {
        const [circunscripciones, secciones] = await service.loadDatosDepartamentos(departamento);

        updateData({ departamento, circunscripciones: 0, seccion: 0 });
        cleanDestinos();
        notifyDepartamentoChanged(circunscripciones);
        notifyCircunscripcionChanged(secciones);
    }
    async function updateCircunscripcion(circunscripcion) {
        const secciones = await service.loadSecciones(circunscripcion);
        updateData({ circunscripcion });
        cleanDestinos();
        notifyCircunscripcionChanged(secciones);
    }
    function updateSeccion(seccion) {
        cleanDestinos();
        updateData({ seccion });
    }
    function updateTipoParcela(tipo, tipoDescripcion) {
        updateData({ tipo, tipoDescripcion });
    }
    function updateEstadoParcela(estado, estadoDescripcion) {
        updateData({ estado, estadoDescripcion });
    }
    function cleanOrigenes() {
        parcelasOrigen.splice(0, parcelasOrigen.length);
        notifyOrigenesChanged();
    }
    function cleanDestinos() {
        parcelasDestino.splice(0, parcelasDestino.length);
        notifyDestinosChanged();
    }
    function addOrigenes(parcelas) {
        const deduplicated = parcelas.filter(p => !parcelasOrigen.some(o => o.id === p.id));
        addAsociados(parcelasOrigen, deduplicated, notifyOrigenesChanged);
    }
    async function addDestino(form) {
        form.append("IdDepartamento", data.departamento);
        form.append("IdCircunscripcion", data.circunscripcion);
        form.append("IdSeccion", data.seccion);
        const nomenclatura = await validate(form);
        if (parcelasDestino.some(p => p.nomenclatura === nomenclatura)) {
            throw "La nomenclatura ya se ha incluído en la operación.";
        }
        const parcela = {
            id: nextIdGenerator.next().value,
            tipo: data.tipo,
            tipoDescripcion: data.tipoDescripcion,
            estado: data.estado,
            estadoDescripcion: data.estadoDescripcion,
            nomenclatura,
            partida: form.get("Partida"),
        }
        notifyChanges(events.RESET_DATOS_DESTINO);
        addAsociados(parcelasDestino, [parcela], notifyDestinosChanged);
    }
    async function validate(form) {
        const { nomenclatura, error } = await service.validateDestino(form);
        if (error) {
            throw error;
        }
        return nomenclatura;
    }
    function resetParcelas() {
        cleanOrigenes();
        cleanDestinos();
    }
    async function save() {
        const { error } = await service.save(data, parcelasOrigen, parcelasDestino);
        if (error) {
            throw error;
        }
        initData();
        resetParcelas();
        notifyOperacionSaved();
        return error;
    }
    function loadBuscador(data) {
        return service.loadBuscador(data);
    }
    function updateData(update) {
        data = { ...data, ...update };
        notifySomethingChanged();
    }
    function addAsociados(listado, objetos, notify) {
        listado.push(...objetos);
        notify();
    }
    function removeAsociados(listado, objetos, notify) {
        objetos.forEach(obj => listado.splice(listado.indexOf(obj), 1));
        notify();
    }
    function notifyChanges(event, ...params) {
        for (let sub of (subscriptions[event] || [])) {
            sub(...params);
        }
    }

    function on(notification, fn) {
        const subscription = Object.assign(Object.defineProperty({}, notification, { value: [], writable: true }), subscriptions);
        subscriptions[notification] = subscription[notification].concat(fn);
    }

    initData();

    return {
        loadBuscador,
        getParcelasOrigen: () => parcelasOrigen,
        getParcelasDestino: () => parcelasDestino,
        hasDestinos: () => parcelasDestino.length,
        save,
        updateOperacion,
        updateNumeroPlano: (numeroPlano) => updateData({ numeroPlano }),
        updateFechaOperacion: (fechaOperacion) => updateData({ fechaOperacion }),
        updateDepartamento,
        updateCircunscripcion,
        updateSeccion,
        updateFechaVigencia: (fechaVigencia) => updateData({ fechaVigencia }),
        updateTipoParcela,
        updateEstadoParcela,

        addOrigenes,
        addDestino,
        removeOrigenes: (parcelas) => removeAsociados(parcelasOrigen, parcelas, notifyOrigenesChanged),
        removeDestinos: (parcelas) => removeAsociados(parcelasDestino, parcelas, notifyDestinosChanged),

        onOperacionChanged: on.bind(null, events.OPERACION_CHANGED),
        onDepartamentoChanged: on.bind(null, events.DEPARAMENTO_CHANGED),
        onCircunscripcionChanged: on.bind(null, events.CIRCUNSCRIPCION_CHANGED),
        onParcelasOrigenChanged: on.bind(null, events.ORIGENES_CHANGED),
        onParcelasDestinoChanged: on.bind(null, events.DESTINOS_CHANGED),
        onResetDatosParcelaDestino: on.bind(null, events.RESET_DATOS_DESTINO),
        onSomethingChanged: on.bind(null, events.SOMETHING_CHANGED),
        onSaved: on.bind(null, events.OPERACION_SAVED)

    };
};
function createStore(service) {
    return store(service);
}
