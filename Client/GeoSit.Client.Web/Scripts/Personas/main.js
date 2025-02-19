function AdministradorPersonasController(container, store, fromBuscador) {
    let resultadosBusqueda = [], domicilios = [], profesiones = [];
    const scrollableContent = $(".administrador-content", container),
        modalInfo = $("#modal-info-persona"),
        externalForm = $("#contenido-formulario");
    $(document).ready(() => init()); //wrapper de función porque no admite una función async directamente
    $(window).resize(adjustScrollbars);

    async function init() {
        await preload();
        $(container)
            .one("shown.bs.modal", () => {
                hideLoading();
                tableResultado.columns.adjust();
                delay(adjustScrollbars, 10);
            }).modal("show");
        scrollableContent.niceScroll(getNiceScrollConfig());
        $("a[data-toggle='tab']", scrollableContent).on("shown.bs.tab", adjustScrollbars);
        document.querySelector('#CUIL').oninput = function () {
            formatearCUIL(this);
        }
    }
    function formatearCUIL(input) {
        const cursorPos = input.selectionStart;
        const valorOriginal = input.value;
        let valor = input.value.replace(/\D/g, '');
        // Aplica formato del CUIL: 00-00000000-0
        let valorFormateado = applyFormat(valor);
        input.value = valorFormateado;
        let nuevaPosicionCursor = cursorPos;
        if (valorOriginal.length < valorFormateado.length) {
            if (cursorPos === valorOriginal.length) {
                nuevaPosicionCursor++;
            }
        }
        else if (valorOriginal.length > valorFormateado.length) {
            if (cursorPos > valorOriginal.length - 1 && cursorPos <= valorOriginal.length + 1) {
                nuevaPosicionCursor--;
            }
        }
        input.setSelectionRange(nuevaPosicionCursor, nuevaPosicionCursor);
    }
    function applyFormat(valor) {
        let formattedValue = valor || "";
        if (formattedValue.length > 10) {
            formattedValue = valor.substring(0, 2) + '-' + valor.substring(2, 10) + '-' + valor.substring(10, 11);
        } else if(formattedValue > 2) {
            formattedValue = valor.substring(0, 2) + '-' + valor.substring(2);
        }
        return formattedValue;
    }
    function adjustScrollbars() {
        scrollableContent.getNiceScroll().resize();
        scrollableContent.getNiceScroll().show();
    }
    function toggleActionButtons(row) {
        const controls = $("dt > span[on-off]", $(row).parents(".tabla-con-botones"));

        if (!!row && $(row).hasClass("selected")) {
            controls.removeClass("disabled");
        } else {
            controls.addClass("disabled");
        }
    }
    async function preload() {
        const preloadedPersonaId = Number($("#PersonaId", container).val());
        if (preloadedPersonaId !== 0) {
            const persona = await store.preload(preloadedPersonaId);
            if (!!persona) {
                resultadosBusqueda = [persona];
                tableResultado.ajax.reload().draw();
                $(tableResultado.row(0).node()).click();
            } else {
                mostrarMensajeError(["No se encontró la persona buscada.", "El buscador puede estar siendo actualizado. Intente nuevamente en unos minutos."], "Persona Inexistente");
            }
        }
    }
    function enableEdicion() {
        $(".resultados.tabla-con-botones").addClass("disabled");
        $("input", ".buscador").prop("readonly", true);
        $("button", ".buscador").prop("disabled", true);
        $("dt > span", ".resultados.tabla-con-botones").addClass("disabled");
        $("dt > span", $(".tabla-con-botones", container)).removeClass("hidden");
        $(".modal-footer", container).removeClass("hidden");
        $("input", $("#info-persona", container)).removeAttr("readonly");
        $("select", $("#info-persona", container)).removeAttr("disabled");
        $("#btnGrabarPersona", container).on("click", () => $("#form-persona", container).submit());
        $("#btnCancelarPersona", container).on("click", undo);
        $("#btnAgregarDomicilio", container).on("click", loadDomicilio.bind(null, false));
        $("#btnModificarDomicilio", container).on("click", loadDomicilio.bind(null, true));
        $("#btnEliminarDomicilio", container).on("click", removeDomicilio);
        $("#btnAgregarProfesion", container).on("click", loadProfesion.bind(null, false));
        $("#btnModificarProfesion", container).on("click", loadProfesion.bind(null, true));
        $("#btnEliminarProfesion", container).on("click", removeProfesion);
        adjustScrollbars();
    }
    function disableEdicion() {
        $(".resultados.tabla-con-botones").removeClass("disabled");
        $("input", ".buscador").prop("readonly", false);
        $("button", ".buscador").prop("disabled", false);
        $("dt > span:not([on-off])", ".resultados.tabla-con-botones").removeClass("disabled");
        $("dt > span", $(".tabla-con-botones:not(.resultados)", container)).addClass("hidden");
        $(".modal-footer", container).addClass("hidden");
        $("input", $("#info-persona", container)).attr("readonly", "readonly");
        $("select", $("#info-persona", container)).attr("disabled", "disabled");
        $("#btnCancelarPersona,#btnGrabarPersona", container).off("click");
        $("#btnAgregarDomicilio,#btnModificarDomicilio,#btnEliminarDomicilio", container).off("click");
        $("#btnAgregarProfesion,#btnModificarProfesion,#btnEliminarProfesion", container).off("click");
        adjustScrollbars();
    }
    function showData({ detalle, domicilios: domiciliosAux, profesiones: profesionesAux }) {
        hideLoading();
        $("#PersonaId", container).val(detalle.PersonaId);
        $("#Nombre", container).val(detalle.Nombre);
        $("#Apellido", container).val(detalle.Apellido);
        $("#TipoDocId", container).val(detalle.TipoDocId);
        $("#NroDocumento", container).val(detalle.NroDocumento);
        $("#TipoPersonaId", container).val(detalle.TipoPersonaId);
        $("#Nacionalidad", container).val(detalle.Nacionalidad);
        $("#Sexo", container).val(detalle.Sexo);
        $("#EstadoCivil", container).val(detalle.EstadoCivil);
        $("#Telefono", container).val(detalle.Telefono);
        $("#Email", container).val(detalle.Email);
        $("#CUIL", container).val(applyFormat(detalle.CUIL));

        domicilios = domiciliosAux;
        tableDomicilios.ajax.reload().draw();

        profesiones = profesionesAux;
        tableProfesiones.ajax.reload().draw();
        adjustScrollbars();
    }
    async function save(persona) {
        const [ok, errores] = await store.validate(persona);
        if (!ok) {
            hideLoading();
            mostrarMensajeError(errores, "Guardar Persona", true);
            return;
        }
        try {
            showLoading();
            const persisted = await store.save(persona);
            hideLoading();
            await mostrarMensajeGeneral(["Se ha guardado la persona correctamente"], "Guardar Persona");
            if (fromBuscador) {
                triggerEventBuscador(persisted);
                return;
            }
            undo();
        } catch (error) {
            hideLoading();
            let msgs = ["Se ha producido un error y no se han podido guardar los cambios."];
            if (error.status === 400) {
                msgs = msgs.concat(["", "El CUIL no es válido."]);
            }
            mostrarMensajeError(msgs, "Guardar Persona", true);
        }
    }
    function triggerEventBuscador(persona) {
        $(window).trigger({ type: "personaAgregada", persona: persona });
        $(container).modal("hide");
    }
    function undo() {
        $(tableDomicilios.row(".selected").node()).trigger("click");
        $(tableProfesiones.row(".selected").node()).trigger("click");

        disableEdicion();
        try {
            const row = tableResultado.row(".selected");
            if (row.data()) {
                showLoading();
                store.loadDetail(row.data());
                toggleActionButtons(row.node());
            } else {
                store.reset();
            }
        } catch {
            hideLoading();
            alert("error");
        }
    }
    function domicilioSaved(exists, evt) {
        const row = tableDomicilios.row(".selected"),
            domicilio = evt.domicilio;
        if (exists) {
            store.updateDomicilio(row.data(), domicilio);
            row.data(domicilio);
        } else {
            store.addDomicilio(domicilio);
            tableDomicilios.rows.add([domicilio]);
        }
        tableDomicilios.draw();
    }
    function profesionSaved(exists, evt) {
        const row = tableProfesiones.row(".selected"),
            profesion = evt.profesion;
        if (exists) {
            store.updateProfesion(row.data(), profesion);
            row.data(profesion);
        } else {
            store.addProfesion(profesion);
            tableProfesiones.rows.add([profesion]);
        }
        tableProfesiones.draw();
    }
    async function loadDomicilio(exists) {
        showLoading();
        try {
            const data = exists && tableDomicilios.row(".selected").data();
            externalForm.html(await store.loadDomicilio(data));
            $(document).off("domicilioGuardado").one("domicilioGuardado", domicilioSaved.bind(null, exists));
        } catch (error) {
            mostrarMensajeError(["Se produjo un error al abrir el formulario de domicilio.", error.responseText], "Formulario de Domicilio", true);
        } finally {
            hideLoading();
        }
    }
    async function loadProfesion(exists) {
        showLoading();
        try {
            const data = exists && tableProfesiones.row(".selected").data();
            externalForm.html(await store.loadProfesion(data));
            $(document).off("profesionGuardada").one("profesionGuardada", profesionSaved.bind(null, exists));
        } catch (error) {
            mostrarMensajeError(["Se produjo un error al abrir el formulario de domicilio.", error.responseText], "Formulario de Profesion", true);
        } finally {
            hideLoading();
        }
    }
    async function removeProfesion() {
        const row = tableProfesiones.row(".selected");
        if (row.data()) {
            const confirma = await mostrarMensajeGeneral([`¿Está seguro que desea eliminar la profesión seleccionada?`], "Eliminación de Profesión", true);
            if (!confirma) return;
            store.removeProfesion(row.data());
            toggleActionButtons(row.node());
            row.remove().draw();
        }
    }
    async function removeDomicilio() {
        const row = tableDomicilios.row(".selected");
        if (row.data()) {
            const confirma = await mostrarMensajeGeneral([`¿Está seguro que desea eliminar el domicilio seleccionado?`], "Eliminación de Domicilio", true);
            if (!confirma) return;
            store.removeDomicilio(row.data());
            toggleActionButtons(row.node());
            row.remove().draw();
        }
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
    function mostrarMensaje(mensajes, titulo, tipo) {
        $(".modal-title", modalInfo).html(titulo);
        $("[role='alert'] > p", modalInfo).html(mensajes.join("<br/>"));
        $("[role='alert']", modalInfo)
            .removeClass("alert-danger alert-success alert-info alert-warning")
            .addClass(tipo);
        $(".modal-footer", modalInfo).hide();
        $("[role='button']", modalInfo).off("click");
        $(modalInfo).off("hidden.bs.modal").modal("show");
        return new Promise(ok => {
            $(modalInfo).one("hidden.bs.modal", (evt) => {
                evt.stopImmediatePropagation();
                ok(true);
            });
        });
    }
    function mostrarMensajeError(mensajes, titulo, error) {
        return mostrarMensaje(mensajes, titulo, (error || false ? "alert-danger" : "alert-warning"));
    }
    function mostrarMensajeGeneral(mensajes, titulo, confirmacion) {
        const prom = mostrarMensaje(mensajes, titulo, (confirmacion ? "alert-warning" : "alert-success"));
        if (confirmacion) {
            $(modalInfo).off("hidden.bs.modal");
            $(".modal-footer", modalInfo).show();
            prom = new Promise(ok => {
                $(modalInfo)
                    .one("hidden.bs.modal", (evt) => {
                        evt.stopImmediatePropagation();
                        ok(false);
                    });
                $("[role='button']", modalInfo).on("click", ok.bind(null, true));
            });
        }
        return prom;
    }
    async function rowResultadoClicked(data, evt) {
        $(tableDomicilios.row(".selected").node()).trigger("click");
        $(tableProfesiones.row(".selected").node()).trigger("click");

        $(evt.currentTarget).toggleClass("selected")
            .siblings().removeClass("selected");

        try {
            if ($(evt.currentTarget).hasClass("selected")) {
                showLoading();
                store.loadDetail(data);
            } else {
                store.reset();
            }
            toggleActionButtons(evt.currentTarget);
        } catch {
            hideLoading();
            alert("error");
        }
    }
    async function rowClicked(domRow) {
        $(domRow).toggleClass("selected")
            .siblings().removeClass("selected");

        toggleActionButtons(domRow);
    }

    const resultadoBusquedaOpts = {
        ajax: (_, callback) => {
            callback({ data: resultadosBusqueda });
            delay(adjustScrollbars, 10);
        },
        autoWidth: false,
        columns: [
            { title: "Nombre", data: "Nombre", width: "40%" },
            { title: "Apellido", data: "Apellido", width: "40%" },
            { title: "Documento", data: "NroDocumento", width: "20%" },
        ],
        createdRow: function (row, data) {
            $(row).on("click", rowResultadoClicked.bind(null, data));
        },
        drawCallback: function () {
            tableResultado.columns.adjust();
        },
        initComplete: function () {
            $(this)
                .on("page.dt", function () {
                    $(tableResultado.row(".selected").node()).trigger("click");
                    delay(adjustScrollbars, 10);
                })
                .dataTable().api().columns.adjust();
        }
    };
    const tableResultado = initGrid("grillaResultadoBusqueda", resultadoBusquedaOpts);

    const domiciliosOpts = {
        ajax: (_, callback) => {
            callback({ data: domicilios });
            delay(adjustScrollbars, 10);
        },
        autoWidth: false,
        columns: [
            { title: "Tipo", data: "TipoDomicilio.Descripcion", width: "30%" },
            {
                title: "Direcci&oacute;n", render: (_, __, data) => {
                    return `${data.ViaNombre} ${data.numero_puerta}`;

                }, width: "30%"
            },
            { title: "C. P.", data: "codigo_postal", width: "15%" },
            { title: "Localidad", data: "localidad", width: "25%" },
        ],
        createdRow: function (row) {
            $(row).on("click", rowClicked.bind(null, row));
        },
        drawCallback: function () {
            tableDomicilios.columns.adjust();
        },
        initComplete: function () {
            $(this)
                .on("page.dt", function () {
                    $(tableDomicilios.row(".selected").node()).trigger("click");
                    delay(adjustScrollbars, 10);
                });
        }
    };
    const tableDomicilios = initGrid("grillaDomicilios", domiciliosOpts);

    const profesionesOpts = {
        ajax: (_, callback) => {
            callback({ data: profesiones })
            delay(adjustScrollbars, 10);
        },
        autoWidth: false,
        columns: [
            { title: "Profesión", data: "TipoProfesion.Descripcion", width: "45%" },
            { title: "Matrícula", data: "Matricula", width: "55%" },
        ],
        createdRow: function (row) {
            $(row).on("click", rowClicked.bind(null, row));
        },
        drawCallback: function () {
            tableProfesiones.columns.adjust();
        },
        initComplete: function () {
            $(this)
                .on("page.dt", function () {
                    $(tableProfesiones.row(".selected").node()).trigger("click");
                    delay(adjustScrollbars, 10);
                });
        }
    };
    const tableProfesiones = initGrid("grillaProfesiones", profesionesOpts);

    $("#form-buscador", container).on("submit", async (evt) => {
        evt.preventDefault();
        showLoading();
        try {
            resultadosBusqueda = await store.search($("[type='text']", evt.currentTarget).val());
            $(tableResultado.row(".selected").node()).trigger("click");
            tableResultado.ajax.reload().draw();
        } catch {
            mostrarMensajeError(["Ha ocurrido un error al realizar la búsqueda."], "Buscar Personas", true);
        } finally {
            hideLoading();
        }
    });

    store.onDetailLoaded(showData);

    $("#btnAgregarPersona", container).on("click", function () {
        $(tableResultado.row(".selected").node()).trigger("click");
        enableEdicion();
    });
    $("#btnModificarPersona", container).on("click", enableEdicion);
    $("#btnEliminarPersona", container).on("click", async function () {
        const row = tableResultado.row(".selected");
        if (row.data()) {
            const confirma = await mostrarMensajeGeneral([`¿Está seguro que desea eliminar los datos de ${row.data().Nombre} ${row.data().Apellido}?`], "Eliminación de Persona", true);
            if (!confirma) return;
            showLoading();
            try {
                await store.remove(row.data());
                toggleActionButtons(row.node());
                row.remove().draw();
            } catch (error) {
                mostrarMensajeError(["Ha ocurrido un error al eliminar la persona."], "Eliminar Persona", true);
            } finally {
                hideLoading();
            }
        }
    });
    $("#form-persona", container).on("submit", function (evt) {
        evt.stopPropagation();
        const persona = $(this).serializeObject();
        save(persona);
        return false;
    });
}