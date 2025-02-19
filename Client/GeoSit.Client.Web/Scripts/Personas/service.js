const service = function () {
    function search(pattern) {
        return new Promise((ok, err) => {
            $.post(`${BASE_URL}Persona/SearchPersonas`, { pattern }, ok).fail(err);
        });
    }
    function remove(persona) {
        return new Promise((ok, err) => {
            $.post(`${BASE_URL}Persona/DeletePersona/`, persona, ok).fail(err);
        });
    }
    function save(persona, domicilios, profesiones) {
        return new Promise((ok, err) => {
            $.post(`${BASE_URL}Persona/Save_DatosPersona/`, { persona, domicilios, profesiones }, ok).fail(err);
        });
    }
    function loadDetail(persona) {
        return new Promise((ok, err) => {
            $.getJSON(`${BASE_URL}Persona/GetDatosPersonaJson/${persona.PersonaId}`, ok).fail(err);
        });
    }
    function loadDomicilio(domicilio) {
        return new Promise((ok, err) => $.post(`${BASE_URL}Domicilio/LoadDatosDomicilio/`, domicilio, ok, "html").fail(err));
    }
    function loadProfesion(profesion) {
        return new Promise((ok, err) => $.post(`${BASE_URL}Profesiones/DatosProfesion/`, profesion, ok, "html").fail(err));
    }
    return {
        loadDetail,
        loadDomicilio,
        loadProfesion,
        remove,
        search,
        save,
    }
};
function createService() {
    return service();
}
