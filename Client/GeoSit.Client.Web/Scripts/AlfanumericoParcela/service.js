const service = function () {
    function loadDatosOperacion(operacion) {
        return new Promise((ok, err) => $.getJSON(`${BASE_URL}AlfanumericoParcela/GetOperacionInitializationData?operacion=${operacion}`, ok).fail(err));
    }
    function loadBuscador(data) {
        return new Promise((ok, err) => $.post(`${BASE_URL}BuscadorGenerico`, data, ok, "html").fail(err));
    }
    function loadDatosDepartamentos(departamento) {
        return Promise.all([loadCircunscripciones(departamento), loadSecciones(0)]);
    }
    function loadCircunscripciones(departamento) {
        return new Promise((ok, err) => $.getJSON(`${BASE_URL}AlfanumericoParcela/GetCircunscripciones?departamento=${departamento}`, ok).fail(err));
    }
    function loadSecciones(circunscripcion) {
        return new Promise((ok, err) => $.getJSON(`${BASE_URL}AlfanumericoParcela/GetSecciones?circunscripcion=${circunscripcion}`, ok).fail(err));
    }
    function save(data, origenes, destinos) {
        const operacion = {
            Operacion: data.operacion,
            NumeroPlano: data.numeroPlano,
            FechaOperacion: data.fechaOperacion,
            FechaVigencia: data.fechaVigencia,
            ParcelasOrigen: origenes.map(o => Number(o.id)),
            ParcelasDestino: destinos.map(d => ({
                    IdTipoParcela: d.tipo,
                    IdEstadoParcela: d.estado,
                    Nomenclatura: d.nomenclatura,
                    Partida: d.partida
            }))
        };

        return new Promise((ok, err) => $.ajax({
            type: "POST",
            url: `${BASE_URL}AlfanumericoParcela/Save`,
            data: JSON.stringify({ operacion }),
            contentType: "application/json",
            success: ok,
            error: err
        }));
    }
    function validateDestino(form) {
        return new Promise((ok, err) => {
            $.ajax({
                type: "POST",
                url: `${BASE_URL}AlfanumericoParcela/ValidateDestino`,
                data: form,
                contentType: false,
                processData: false,
                success: ok,
                error: err
            });
        });
    }

    return {
        loadDatosDepartamentos,
        loadSecciones,
        loadDatosOperacion,
        validateDestino,
        loadBuscador,
        save
    };
};
function createService() {
    return service();
}