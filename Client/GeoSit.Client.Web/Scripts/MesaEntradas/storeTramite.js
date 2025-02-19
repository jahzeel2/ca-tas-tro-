const store = function (service) {
    const events = {
        ASUNTO_CHANGED: "asunto-changed",
        CAUSA_CHANGED: "causa-changed",
        DESTINOS_CHANGED: "destinos-changed",
        ORIGENES_CHANGED: "origenes-changed",
        NOTAS_LOADED: "notas-loaded",
        MOVIMIENTOS_LOADED: "movimientos-loaded",
        NOTA_ADDED: "nota-added",
        NOTA_CHANGED: "nota-changed",
        RELOADED: "reloaded",
        RESET: "reset"
    }
    let tramite = {}, informeTramite = null, notas = [], origenes = [], destinos = [], subscriptions = {};
    const idNotaGenerator = tempIdGenerator(0);

    function initialize(data) {
        tramite = {
            IdTramite: parseInt(data.id),
            Numero: data.numero,
            FechaIngreso: data.fechaIngreso,
            IdEstado: parseInt(data.estado),
            IdPrioridad: parseInt(data.prioridad),
            IdIniciador: parseInt(data.iniciador),
            IdTipoTramite: data.asunto,
            IdObjetoTramite: data.causa,
            Comprobante: data.comprobante,
            Monto: Number(data.monto),
            Plano: data.plano,
        };
    }
    function downloadFile(filename) {
        return service.downloadFile(tramite, filename);
    }
    function openHojaRuta(filename) {
        return service.openHojaRuta(tramite, filename);
    }
    function execute(action) {
        return service.execute(tramite, action);
    }
    function getDestinos() {
        return destinos;
    }
    function getOrigenes() {
        return origenes;
    }
    function getNotas() {
        return notas;
    }
    function hasValidInforme() {
        return !!informeTramite;
    }
    function addDestinos(nuevos) {
        destinos = [...destinos, ...nuevos];
        notify(events.DESTINOS_CHANGED, true);
    }
    function loadAdminPersonas() {
        return service.loadAdminPersonas();
    }
    async function loadDatosDestino() {
        destinos = await service.loadDatosDestino();
        notify(events.DESTINOS_CHANGED);
    }
    async function loadDatosOrigen() {
        origenes = await service.loadDatosOrigen();
        notify(events.ORIGENES_CHANGED);
    }
    async function loadMovimientos() {
        const movimientos = await service.loadMovimientos();
        notify(events.MOVIMIENTOS_LOADED, movimientos);
    }
    async function loadNotas() {
        notas = await service.loadNotas();
        notify(events.NOTAS_LOADED);
    }
    function loadFormValuacion(destino) {
        return service.loadFormValuacion(tramite, destino);
    }
    function removeDestinos(seleccion) {
        destinos = destinos.filter(o => !seleccion.some(s => s === o));
        notify(events.DESTINOS_CHANGED, true);
    }
    function removeOrigenes(seleccion) {
        origenes = origenes.filter(o => !seleccion.some(s => s === o));
        notify(events.ORIGENES_CHANGED, true);
    }
    function askReservas() {
        return service.askReservas(tramite, origenes, destinos, notas);
    }
    function save(ingresar) {
        return service.save(ingresar, tramite, origenes, destinos, notas);
    }
    function saveAntecedentes() {
        return service.saveAntecedentes(tramite, origenes, destinos, notas);
    }
    function saveInforme(nuevo, firmado) {
        debugger
        return service.saveInforme(tramite, informeTramite, nuevo, firmado);
    }
    function confirmReservas() {
        return service.confirmReservas(tramite, origenes, destinos, notas);
    }
    function setTramiteCurrentUser() {
        return service.setTramiteCurrentUser(tramite);
    }
    function validate() {
        let mensajes = []

        if (!tramite.IdTipoTramite) {
            mensajes.push("Debe seleccionar un asunto.");
        }
        if (!tramite.IdObjetoTramite) {
            mensajes.push("Debe seleccionar una causa.");
        }
        if (!tramite.IdPrioridad) {
            mensajes.push("Debe seleccionar una prioridad.");
        }
        if (!tramite.IdIniciador) {
            mensajes.push("Debe seleccionar un iniciador.");
        }
        return mensajes;
    }
    async function updateAsunto(asunto = 0) {
        tramite = { ...tramite, ...{ IdTipoTramite: Number(asunto) } };
        const causas = service.getCausas(tramite);
        const acciones = service.getAcciones(tramite);
        updateCausa(0);
        notify(events.ASUNTO_CHANGED, tramite.IdTipoTramite, await causas, await acciones);
    }
    function updateCausa(causa = 0) {
        tramite = { ...tramite, ...{ IdObjetoTramite: Number(causa) } };
        notify(events.CAUSA_CHANGED, tramite.IdObjetoTramite, []);
        reset();
    }
    function updatePlano(plano) {
        tramite = { ...tramite, ...{ Plano: plano } };
    }
    function updatePrioridad(prioridad = 0) {
        tramite = { ...tramite, ...{ IdPrioridad: Number(prioridad) } };
    }
    function updateComprobante(comprobante) {
        tramite = { ...tramite, ...{ Comprobante: comprobante } };
    }
    function updateIniciador(iniciador) {
        tramite = { ...tramite, ...{ IdIniciador: Number(iniciador) } };
    }
    function updateMonto(monto) {
        tramite = { ...tramite, ...{ Monto: Number(monto) } };
    }
    function updateDestinoDataText(key, guid, text) {
        const destino = destinos.find(d => d.Guid === guid);
        destino.Propiedades.find(p => p.Id === key).Text = text;
    }
    function updateDestinoDataValue(key, guid, value) {
        const destino = destinos.find(d => d.Guid === guid);
        let prop = destino.Propiedades.find(p => p.Id === key);
        if (!prop) {
            prop = { Id: key };
            destino.Propiedades.push(prop);
        }
        if (Array.isArray(value) || typeof value === "object") {
            value = JSON.stringify(value);
        } 
        prop.Value = value;
    }
    async function reload(soloLectura) {
        const html = await service.reload(soloLectura);
        notify(events.RELOADED, html);
    }
    function reset() {
        destinos = origenes = [];
        notify(events.RESET, tramite.IdObjetoTramite);
    }
    async function loadDatosEspecificos(tipo, padre, ids) {
        const nuevos = await service.loadDatosEspecificos(tipo, ids)
        for (nuevo of nuevos) {
            nuevo.ParentGuids = padre && [padre] || [];
        }
        origenes = origenes.concat(nuevos);
        notify(events.ORIGENES_CHANGED, true);
    }
    function loadGeneradorDestinos() {
        return service.loadGeneradorDestinos(tramite);
    }
    function loadObservacion() {
        return service.loadObservacion(tramite);
    }
    function loadSelectorSector() {
        return service.loadSelectorSector(tramite);
    }
    function loadSelectorUsuario() {
        return service.loadSelectorUsuario(tramite);
    }
    function loadBuscadorPartidas() {
        let data = {
            tipo: BuscadorTipos.UnidadesTributarias,
            multiple: tramite.IdTipoTramite === 1,
            filters: ["dato_unidadFuncional=0"],
            titulo: "Buscar Partidas",
            campos: ["Nomenclatura", "partida:Partida"]
        };

        switch (tramite.IdObjetoTramite) {
            case 15:
            case 25:
            case 32:
            case 33:
                data.campos.push("unidadFuncional:UF");
                data.filters = tramite.IdObjetoTramite === 15 && ["dato_partida<>0"] || [];
                break;
        }
        return loadBuscador(data);
    }
    function loadBuscador(params) {
        let data = {
            tipos: params.tipo,
            multiSelect: !!params.multiple,
            verAgregar: !!params.agrega,
            titulo: params.titulo,
            campos: params.campos,
            filters: params.filters
        };

        return service.loadBuscador(data);
    }
    function processAddedInforme(nota, file) {
        return new Promise(async (ok, err) => {
            try {
                if (!file) {
                    err("Debe cargar un informe para continuar.");
                    return;
                }
                informeTramite = await processNota(nota, file, false);
                ok();
            } catch {
                err("No se pudo asociar el informe.");
            }
        });
    }
    function processUpdatedInforme(nota, file) {
        return new Promise(async (ok, err) => {
            try {
                const nombreArchivo = await service.uploadFile(tramite, file);
                nota.Documento.nombre_archivo = nombreArchivo;
            } catch {
                err("No se pudo asociar el archivo.");
                return;
            }
            let current = notas.filter(d => d.id_documento === nota.id_documento)[0];
            if (!current) {
                nota.id_documento = idNotaGenerator.next().value;
                current = {};
                notas.push(current);
            }
            informeTramite = Object.assign(current, nota);
            ok();
        });
    }
    function processNota(nota, file, send_notification = true) {
        return new Promise(async (ok, err) => {
            if (isNaN(nota.Documento.id_tipo_documento)) {
                err("Debe ingresar el tipo de nota.");
                return;
            }
            if (file) {
                if (!nota.Documento.extension_archivo) {
                    err("El archivo no tiene extensión.");
                    return;
                }
                try {
                    const nombreArchivo = await service.uploadFile(tramite, file);
                    nota.Documento.nombre_archivo = nombreArchivo;
                } catch {
                    err("No se pudo asociar el archivo.");
                    return;
                }
            }
            let current = notas.filter(d => d.id_documento === nota.id_documento)[0];
            let event = events.NOTA_CHANGED;
            if (!current) {
                event = events.NOTA_ADDED;
                nota.id_documento = idNotaGenerator.next().value;
                current = {};
                notas.push(current);
            }
            Object.assign(current, nota);
            send_notification && notify(event, current);
            ok(current);
        });
    }
    function removeNota(nota) {
        return new Promise((ok) => {
            notas = notas.filter(d => d.id_documento !== nota.id_documento);
            ok();
        });
    }
    function notify(event, ...params) {
        for (let sub of (subscriptions[event] || [])) {
            sub(...params);
        }
    }
    function on(notification, fn) {
        const subscription = Object.assign(Object.defineProperty({}, notification, { value: [], writable: true }), subscriptions);
        subscriptions[notification] = subscription[notification].concat(fn);
    }

    return {
        addDestinos,
        askReservas,
        confirmReservas,
        downloadFile,
        execute,
        getNotas,
        getDestinos,
        getOrigenes,
        hasValidInforme,
        initialize,
        loadAdminPersonas,
        loadDatosDestino,
        loadDatosOrigen,
        loadNotas,
        loadMovimientos,
        loadDatosEspecificos,
        loadBuscadorMensuras: loadBuscador.bind(null, { tipo: BuscadorTipos.Mensuras, multiple: false, titulo: "Buscar Mensura", campos: ["Mensura"] }),
        loadBuscadorPartidas,
        loadBuscadorPersonas: loadBuscador.bind(null, { tipo: BuscadorTipos.Personas, multiple: false, agrega: true, titulo: "Buscar Iniciador", campos: ["Nombre", "dni:DNI"] }),
        loadBuscadorManzanas: loadBuscador.bind(null, { tipo: BuscadorTipos.Manzanas, multiple: true, titulo: "Buscar Manzanas", campos: ["Manzana"] }),
        loadFormValuacion,
        loadGeneradorDestinos,
        loadObservacion,
        loadSelectorSector,
        loadSelectorUsuario,
        openHojaRuta,

        processAddedInforme,
        processNota,
        processUpdatedInforme,
        reload,
        removeDestinos,
        removeNota,
        removeOrigenes,

        updateAsunto,
        updateCausa,
        updateComprobante,
        updateIniciador,
        updateMonto,
        updatePlano,
        updatePrioridad,
        updateNomenclaturaDestino: updateDestinoDataText.bind(null, "KeyIdParcela"),
        updatePartidaDestino: updateDestinoDataText.bind(null, "KeyIdUnidadTributaria"),
        updateSuperficieParcelaDestino: updateDestinoDataValue.bind(null, "KeySuperficieParcela"),
        updateValuacionParcela: updateDestinoDataValue.bind(null, "KeyValuacion"),

        save,
        saveAntecedentes,
        saveInforme,
        setTramiteCurrentUser,
        validate,

        onAsuntoChanged: on.bind(null, events.ASUNTO_CHANGED),
        onCausaChanged: on.bind(null, events.CAUSA_CHANGED),
        onDestinosChanged: on.bind(null, events.DESTINOS_CHANGED),
        onOrigenesChanged: on.bind(null, events.ORIGENES_CHANGED),
        onNotasLoaded: on.bind(null, events.NOTAS_LOADED),
        onMovimientosLoaded: on.bind(null, events.MOVIMIENTOS_LOADED),
        onNotaAdded: on.bind(null, events.NOTA_ADDED),
        onNotaChanged: on.bind(null, events.NOTA_CHANGED),
        onReloaded: on.bind(null, events.RELOADED),
        onReset: on.bind(null, events.RESET)
    };
}
function createStoreTramite(service) {
    return store(service);
}
