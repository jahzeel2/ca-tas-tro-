const service = function () {
    function toggle(operation, featid, parcela) {
        return new Promise((ok, err) => $.post(`${BASE_URL}ParcelaGrafica/Save`, { operation, idparcela: parcela.id, featid }, ok).fail(err));
    }
    function search(form) {
        return new Promise((ok, err) => {
            $.ajax({
                type: "POST",
                url: `${BASE_URL}ParcelaGrafica/Search`,
                data: form,
                contentType: false,
                processData: false,
                success: ok,
                error: err
            });
        });
    }
    function locate(name) {

    }
    function loadMantenedor(parcela) {
        //return new Promise((ok, err) => $.get(`${BASE_URL}MantenimientoParcelario/Get/${parcela.id}`, ok, "html").fail(err));
        return new Promise((ok, err) => { $.get(`${BASE_URL}MantenimientoParcelario/GetMantenedorParcelarioView/${parcela.id}?UnidadTributariaId=0`, ok, "html").fail(err);});
    }
    return {
        link: toggle.bind(null, "A"),
        unlink: toggle.bind(null, "D"),
        loadMantenedor,
        search,
        locate
    }
};
function createService() {
    return service();
}

