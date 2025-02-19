function AdministradorOperacionesAlfanumericasController(container, store) {
    const scrollableContent = $(".alfanumerico-content", container),
        modalInfo = $("#modal-info-alfanumerico"),
        buscadorModal = $("#buscador-container"),
        cboTiposOperacion = $("#TipoOperacionId", scrollableContent),
        cboDeptos = $("#IdDepartamento", scrollableContent),
        cboCircunscripciones = $("#IdCircunscripcion", scrollableContent),
        cboSecciones = $("#IdSeccion", scrollableContent),
        cboEstados = $("#IdEstado", scrollableContent),
        cboTiposParcela = $("#IdTipoParcela", scrollableContent),
        txtNumeroPlano = $("#NumeroPlano", scrollableContent),
        fechaOperacion = $("#FechaOperacion", scrollableContent),
        fechaVigencia = $("#FechaVigencia", scrollableContent),
        componentesNomenclatura = $("input", $("[data-nomenclatura]", scrollableContent));

    const tiposOperaciones = {
        SUBDIVISION: 2,
        UNIFICACION: 3,
        SANEAMIENTO: 9999,
    };
    let prevValue, allowOrigenes = false, allowMultiplesOrigenes = false, allowMultiplesDestinos = false;

    $(document).ready(init);
    $(window).resize(adjustScrollbars);

    function init() {
        $(container)
            .one("shown.bs.modal", () => {
                hideLoading();
                delay(adjustScrollbars, 10);
            }).modal("show");
        scrollableContent.niceScroll(getNiceScrollConfig());
        $("a[data-toggle='collapse']", scrollableContent).on("click", delay.bind(null, adjustScrollbars, 50));
        $(".date > input", scrollableContent).datepicker(getDatePickerConfig());

        cboTiposOperacion
            .on("focus", readPreviousValue)
            .on("change", async evt => {
                const confirma = prevValue === "0" || await mostrarMensajeGeneral(["Si cambia la operación perderá los datos ingresados.", "", "¿Desea contiuar?"], "Cambio de Operación", true);
                if (!confirma) {
                    evt.target.value = prevValue;
                    return;
                }
                prevValue = evt.target.value;
                store.updateOperacion(evt.target.value);
                cboDeptos.trigger("change");
            });
        cboDeptos
            .on("focus", readPreviousValue)
            .on("change", async evt => {
                const confirma = prevValue === "0" || !store.hasDestinos() || await mostrarMensajeGeneral(["Si cambia el departamento perderá las parcelas destino ingresadas.", "", "¿Desea contiuar?"], "Cambio de Departamento", true);
                if (!confirma) {
                    evt.target.value = prevValue;
                    return;
                }
                prevValue = evt.target.value;
                store.updateDepartamento(evt.target.value);
                enableAddDestino();
            });
        cboCircunscripciones
            .on("focus", readPreviousValue)
            .on("change", async evt => {
                const confirma = prevValue === "0" || !store.hasDestinos() || await mostrarMensajeGeneral(["Si cambia la circunscripción perderá las parcelas destino ingresadas.", "", "¿Desea contiuar?"], "Cambio de Circunscripción", true);
                if (!confirma) {
                    evt.target.value = prevValue;
                    return;
                }
                prevValue = evt.target.value;
                store.updateCircunscripcion(evt.target.value);
                enableAddDestino();
            });
        cboSecciones
            .on("focus", readPreviousValue)
            .on("change", async evt => {
                const confirma = prevValue === "0" || !store.hasDestinos() || await mostrarMensajeGeneral(["Si cambia la sección perderá las parcelas destino ingresadas.", "", "¿Desea contiuar?"], "Cambio de Sección", true);
                if (!confirma) {
                    evt.target.value = prevValue;
                    return;
                }
                prevValue = evt.target.value;
                store.updateSeccion(evt.target.value);
                enableAddDestino();
            });
        cboTiposParcela
            .on("change", tipoParcelaChanged);
        cboEstados.on("change", estadoParcelaChanged);
        componentesNomenclatura.on("input", enableAddDestino);
        txtNumeroPlano.on("input", (evt) => debounce(store.updateNumeroPlano, 250, evt.target.value));
        fechaOperacion.on("change", (evt) => store.updateFechaOperacion(evt.target.value));
        fechaVigencia.on("change", (evt) => store.updateFechaVigencia(evt.target.value));
        $("#parcelas-origen-insert", scrollableContent).on("click", buscarParcelas);
        $("#parcelas-origen-delete", scrollableContent).on("click", deleteParcelasOrigen);
        $("#parcelas-destino-delete", scrollableContent).on("click", deleteParcelasDestino);

        $("#datos-parcela-destino", scrollableContent)
            .submit(evt => {
                evt.preventDefault();
                addDestino(evt.target);
            });

        $("#save-all", container).on("click", save);
        store.onOperacionChanged((operacion, estados, clase) => {
            populateCombo(cboEstados, estados);
            $("#Clase", scrollableContent).val(clase.Descripcion);
            setAllowOrigenes(operacion);
            setAllowMultiplesDestinos(operacion);
            enableAddOrigenes();
            enableAddDestino();
        });
        store.onDepartamentoChanged(populateCombo.bind(null, cboCircunscripciones));
        store.onCircunscripcionChanged(populateCombo.bind(null, cboSecciones));
        store.onParcelasDestinoChanged(() => {
            enableDepartamento();
            enableSave();
            tableParcelasDestino.ajax.reload().draw();
        });
        store.onParcelasOrigenChanged(() => {
            enableAddOrigenes();
            enableSave();
            tableParcelasOrigen.ajax.reload().draw();
        });
        store.onResetDatosParcelaDestino(() => cboTiposParcela.trigger("change"));
        store.onSomethingChanged(enableSave);
        store.onSaved(resetAll.bind(null));
        $('#NumeroPlano').on('input', function () {
            let input = $(this),
                value = input.val(),
                cursorPosition = input[0].selectionStart,
                formattedValue = value.replace(/[^a-zA-Z0-9]/g, '');
            if (formattedValue.length > 2) formattedValue = formattedValue.slice(0, 2) + '-' + formattedValue.slice(2);
            if (formattedValue.length > 6) formattedValue = formattedValue.slice(0, 6) + '-' + formattedValue.slice(6);
            let adjustment = (cursorPosition > 2 && value.charAt(2) !== '-') ? 1 : 0;
            adjustment += (cursorPosition > 6 && value.charAt(6) !== '-') ? 1 : 0;
            input.val(formattedValue);
            input[0].setSelectionRange(cursorPosition + adjustment, cursorPosition + adjustment);
        });
    }
    function adjustScrollbars() {
        scrollableContent.getNiceScroll().resize();
        scrollableContent.getNiceScroll().show();
    }
    function populateCombo(cbo, values) {
        const options = values.map(c => `<option value="${c.Value}">${c.Text}</option>`);
        cbo.html(`<options>${options}</options>`);
    }
    function deleteParcelasOrigen() {
        const rows = tableParcelasOrigen.rows(".selected").data();
        store.removeOrigenes(rows.toArray());
    }
    function deleteParcelasDestino() {
        const rows = tableParcelasDestino.rows(".selected").data();
        store.removeDestinos(rows.toArray());
    }
    async function addDestino(form) {
        try {
            showLoading();
            await store.addDestino(new FormData(form));
        } catch (err) {
            mostrarMensajeError([err], "Validación de datos cargados");
        } finally {
            hideLoading();
        }
    }
    function resetAll() {
        prevValue = "0";
        cboTiposOperacion.val("0").trigger("change");
        cboDeptos.val("0").trigger("change");
        cboTiposParcela.val("0");
        fechaOperacion.val("");
        fechaVigencia.val("");
        txtNumeroPlano.val("");

    }
    async function save() {
        try {
            showLoading();
            await store.save();
            mostrarMensajeGeneral(["Se han aplicado los cambios correctamente."], "Operación Alfanumérica de Parcelas");
        } catch (err) {
            mostrarMensajeError([err], "Confirmar Operación", true);
        } finally {
            hideLoading();
        }
    }
    function readPreviousValue(evt) {
        prevValue = evt.target.value;
    }
    function esUrbana(tipo) {
        return tipo === 1;
    }
    function esRural(tipo) {
        return tipo === 2 || tipo === 3;
    }
    function setAllowMultiplesDestinos(value) {
        const tiposDestinoUnico = [tiposOperaciones.UNIFICACION];
        allowMultiplesDestinos = !tiposDestinoUnico.some(op => op === value);
    }
    function setAllowMultiplesOrigenes(value) {
        const tiposMultiplesOrigenes = [tiposOperaciones.SUBDIVISION];
        allowMultiplesOrigenes = !tiposMultiplesOrigenes.some(op => op === value);
    }
    function setAllowOrigenes(value) {
        const tipo = Number(value);
        allowOrigenes = tipo !== 0 && tiposOperaciones.SANEAMIENTO !== tipo;
        setAllowMultiplesOrigenes(tipo);
    }
    function enableAddOrigenes() {
        const btn = $("#parcelas-origen-insert", scrollableContent).addClass("disabled");
        if (allowOrigenes && (allowMultiplesOrigenes || store.getParcelasOrigen().length < 1)) {
            btn.removeClass("disabled");
        }
    }
    function enableAddDestino() {
        const btn = $("#parcelas-destino-insert", scrollableContent).addClass("disabled");
        if ((allowMultiplesDestinos || store.getParcelasDestino().length < 1) &&
            Number(cboTiposOperacion.val()) !== 0 && Number(cboTiposParcela.val()) !== 0 &&
            Number(cboDeptos.val()) !== 0 && Number(cboCircunscripciones.val()) !== 0 && Number(cboSecciones.val()) !== 0 &&
            Number(cboEstados.val()) !== 0 && componentesNomenclatura.not("[readonly]").toArray().every(el => new RegExp(el.getAttribute("pattern")).test(el.value.toUpperCase()))) {
            btn.removeClass("disabled");
        }
    }
    function enableDepartamento() {
        [cboDeptos, cboCircunscripciones, cboSecciones]
            .forEach((cbo) => cbo.prop("disabled", store.getParcelasDestino().length));
    }
    function enableNomenclatura(componentes) {
        const inputs = componentesNomenclatura.attr("readonly", "readonly");
        if (!componentes.length) return;
        for (let elem of inputs.toArray()) {
            if (componentes.includes(elem.id)) {
                elem.removeAttribute("readonly");
            } else {
                elem.value = elem.placeholder;
            }
        }
        inputs.not("[readonly]").first().focus();
    }
    function enableSave() {
        const btn = $("#save-all", container).addClass("disabled");
        if (store.getParcelasDestino().length && (!allowOrigenes || store.getParcelasOrigen().length)
            && (Number(cboTiposOperacion.val()) === tiposOperaciones.SANEAMIENTO || txtNumeroPlano.val()) && fechaOperacion.val() && fechaVigencia.val()) {
            btn.removeClass("disabled");
        }
    }
    function tipoParcelaChanged(evt) {
        const tipo = Number(evt.target.value),
            tipoDescripcion = $(":selected", evt.target).text(),
            componentesRurales = ["Chacra", "Quinta", "Fraccion", "Parcela", "Partida"],
            componentesUrbanos = ["Manzana", "Parcela"];

        store.updateTipoParcela(tipo, tipoDescripcion);
        componentesNomenclatura.val("");
        if (esRural(tipo)) {
            enableNomenclatura(componentesRurales);
        } else if (esUrbana(tipo)) {
            enableNomenclatura(componentesUrbanos);
        } else {
            enableNomenclatura([]);
        }
    }
    function estadoParcelaChanged(evt) {
        const estado = Number(cboEstados.val()),
            estadoDescripcion = $(":selected", evt.target).text();
        store.updateEstadoParcela(estado, estadoDescripcion);
        enableAddDestino();
    }
    function initGrid(grid, options) {
        const gridOptions = {
            ...{
                destroy: true,
                pageLength: 5,
                dom: "tp",
                processing: true,
                paging: true,
                language: { url: `${BASE_URL}Scripts/dataTables.spanish.txt` },
                order: [0, 'asc'],
            }, ...options
        };
        return $(`#${grid}`, container).DataTable(gridOptions);
    }
    function rowParcelaClicked(evt) {
        $(evt.currentTarget).toggleClass("selected");
        toggleActionButtons(evt.currentTarget);
    }
    function toggleActionButtons(row) {
        const controls = $("dt > span[on-off]", $(row).parents(".tabla-con-botones"));

        if (!!row && $(row).hasClass("selected")) {
            controls.removeClass("disabled");
        } else {
            controls.addClass("disabled");
        }
    }
    /*
    function mostrarMensaje(mensajes, titulo, tipo) {
        $(".modal-title", modalInfo).html(titulo);
        $("[role='alert'] > p", modalInfo).html(mensajes.join("<br/>"));
        $("[role='alert']", modalInfo)
            .removeClass("alert-danger alert-success alert-info alert-warning")
            .addClass(tipo);
        $(".modal-footer", modalInfo).hide();
        $("[role='button']", modalInfo).off("click");
        $(modalInfo).modal("show");
    }
    function mostrarMensajeError(mensajes, titulo, error) {
        return mostrarMensaje(mensajes, titulo, (error || false ? "alert-danger" : "alert-warning"));
    }
    function mostrarMensajeGeneral(mensajes, titulo, confirmacion) {
        mostrarMensaje(mensajes, titulo, (confirmacion ? "alert-warning" : "alert-success"));
        if (confirmacion) {
            $(".modal-footer", modalInfo).show();
            return new Promise(ok => {
                $(modalInfo).off("hidden.bs.modal").one("hidden.bs.modal", ok.bind(null, false))
                $("[role='button']", modalInfo).on("click", ok.bind(null, true));
            });
        }
        return Promise.resolve(true);
    }
    */
    async function buscarParcelas() {
        const data = {
            tipos: BuscadorTipos.Parcelas,
            multiSelect: allowMultiplesOrigenes,
            verAgregar: false,
            titulo: 'Buscar Parcelas',
            campos: ["Nomenclatura", "partida:Partida"],
            readonlyText: false
        };
        buscadorModal.html(await store.loadBuscador(data))
            .one("hidden.bs.modal", () => {
                $(window).off("seleccionAceptada");
                buscadorModal.empty();
            });

        $(window).one("seleccionAceptada", (evt) => {
            if (evt.seleccion) {
                let results = evt.seleccion;
                if (!allowMultiplesOrigenes) {
                    results = [evt.seleccion];
                }
                const parcelas = results.map(r => ({
                    nomenclatura: r[1],
                    id: r[2]
                }));
                store.addOrigenes(parcelas);
            }
        });
    }

    const optsParcelasOrigen = {
        ajax: (_, callback) => {
            callback({ data: store.getParcelasOrigen() });
            delay(adjustScrollbars, 50);
        },
        columns: [{ data: "nomenclatura", title: "Nomenclatura", width: "100%" }],
        createdRow: function (row) {
            $(row).on("click", rowParcelaClicked);
        },
        drawCallback: function () {
            tableParcelasOrigen.columns.adjust();
        },
    };
    const tableParcelasOrigen = initGrid("parcelas-origen", optsParcelasOrigen);

    const optsParcelasDestino = {
        ajax: (_, callback) => {
            callback({ data: store.getParcelasDestino() });
            delay(adjustScrollbars, 50);
        },
        autoWidth: false,
        columns: [
            { data: "tipoDescripcion", title: "Tipo", width: "20%" },
            { data: "estadoDescripcion", title: "Estado", width: "20%" },
            { data: "nomenclatura", title: "Nomenclatura", width: "35%" },
            { data: "partida", title: "Partida", width: "25%" },
        ],
        createdRow: function (row) {
            $(row).on("click", rowParcelaClicked);
        },
        drawCallback: function () {
            tableParcelasDestino.columns.adjust();
        },
    }
    const tableParcelasDestino = initGrid("parcelas-destino", optsParcelasDestino);

}
//@ sourceURL=alfanumerico.js
