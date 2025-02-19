const store = function (service) {
    const events = {
        DETAIL_LOADED: "detail-loaded",
    }
    let detalle = {}, domicilios = [], profesiones = [], subscriptions = {}, preloaded;
    async function search(pattern) {
        const findings = await service.search(pattern);
        reset();
        return findings;
    }
    async function preload(id) {
        ({ detalle, domicilios, profesiones } = await service.loadDetail({ PersonaId: id }));
        preloaded = !!detalle
        return detalle;
    }
    async function loadDetail(persona) {
        if (!preloaded) {
            ({ detalle, domicilios, profesiones } = await service.loadDetail(persona));
        }
        preloaded = false;
        notifyDetailLoaded();
    }
    async function loadDomicilio(domicilio) {
        return await service.loadDomicilio(domicilio);
    }
    async function loadProfesion(profesion) {
        return await service.loadProfesion(profesion);
    }
    async function remove(persona) {
        await service.remove(persona);
        reset();
    }
    function save(persona) {
        return service.save(persona, domicilios, profesiones);
    }
    function addAsociados(listado, objeto) {
        listado.push(objeto);
    }
    function removeAsociados(listado, objeto) {
        listado.splice(listado.indexOf(objeto), 1);
    }
    function updateAsociados(listado, objeto, reemplazo) {
        listado.splice(listado.indexOf(objeto), 1, reemplazo);
    }

    async function validate(persona) {
        const { errors } = await new ValidationLogger(persona)
            .validate(areRequiredFieldsOk)
            .validate(isValidCUIL);

        return [!errors.length, errors];
    }
    function reset() {
        detalle = {};
        domicilios = [];
        profesiones = [];
        notifyDetailLoaded();
    }
    function notifyDetailLoaded() {
        for (let sub of subscriptions[events.DETAIL_LOADED]) {
            sub({ detalle, domicilios, profesiones });
        }
    }
    function on(notification, fn) {
        const subscription = Object.assign(Object.defineProperty({}, notification, { value: [], writable: true }), subscriptions);
        subscriptions[notification] = subscription[notification].concat(fn);
    }
    function areRequiredFieldsOk(persona) {
        let errors = [];
        if (!persona.TipoPersonaId) {
            errors.push("El campo TIPO DE PERSONA es obligatorio.");
        }
        if (!persona.Nombre) {
            errors.push("El campo NOMBRE es obligatorio.");
        }
        if (isPerson(persona) && !persona.Apellido) {
            errors.push("El campo APELLIDO es obligatorio.");
        }
        if (!persona.TipoDocId) {
            errors.push("El campo TIPO DE DOCUMENTO es obligatorio.");
        }
        if (isPerson(persona) && !persona.NroDocumento) {
            errors.push("El campo NRO DE DOCUMENTO es obligatorio.");
        }
        if (!persona.Nacionalidad) {
            errors.push("El campo NACIONALIDAD es obligatoria.");
        }
        if (!persona.Sexo) {
            errors.push("El campo SEXO es obligatorio.");
        }
        return new ValidationLogger(persona, errors);
    }
    function isValidCUIL(persona) {
        persona.CUIL = persona.CUIL.replace(/-/g, '');
        let error = [];
        if (isPerson(persona) && persona.CUIL && (persona.CUIL.length != 11 || isNaN(persona.CUIL) || !/^[0-9]{11}$/.test(persona.CUIL))) {
            error = ["El CUIL no tiene el formato correcto."];
        }
        return new ValidationLogger(persona, error);
    }
    function isPerson(persona) {
        return Number(persona.TipoPersonaId) === 1;
    }
    return {
        search,
        preload,
        reset,
        save,
        loadDetail,
        loadDomicilio,
        loadProfesion,
        addDomicilio: (domicilio) => addAsociados(domicilios, domicilio),
        addProfesion: (profesion) => addAsociados(profesiones, profesion),
        remove,
        removeDomicilio: (domicilio) => removeAsociados(domicilios, domicilio),
        removeProfesion: (profesion) => removeAsociados(profesiones, profesion),
        updateDomicilio: (domicilio, actualizado) => updateAsociados(domicilios, domicilio, actualizado),
        updateProfesion: (profesion, actualizado) => updateAsociados(profesiones, profesion, actualizado),
        validate,
        onDetailLoaded: on.bind(null, events.DETAIL_LOADED)
    }
};
function createStore(service) {
    return store(service);
};