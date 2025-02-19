function AdministradorMensurasController(container, store) {
    let parcelas = [], mensuras = [], documentos = [];
    const idParcelaMensuraGenerator = tempIdGenerator(0),
        idMensuraRelacionadaGenerator = tempIdGenerator(0),
        idMensuraDocumentoGenerator = tempIdGenerator(0);
    const scrollableContent = $(".mensura-content", container),
        modalInfo = $("#modal-info-mensura"),
        externalForm = $("#contenedor-forms-externos");
    $(document).ready(init);
    $(window).resize(adjustScrollbars);

    function init() {
        $(container)
            .one("shown.bs.modal", () => {
                hideLoading();
                tableResultado.columns.adjust();
                delay(adjustScrollbars, 10);
            }).modal("show");
        scrollableContent.niceScroll(getNiceScrollConfig());
        $(".date > input", scrollableContent).datepicker(getDatePickerConfig({enableOnReadonly:false}));
        $("a[data-toggle='tab']", scrollableContent).on("shown.bs.tab", adjustScrollbars);
        $("#Departamento").on("change", actualizarDescripcion);
        $("#Numero, #Anio").on("input blur", actualizarDescripcion);
    }
    function actualizarDescripcion() {
        $("#Descripcion").val(`${$("#Departamento").val()}-${$("#Numero").val() || ''}-${$("#Anio").val() || ''}`);
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
    function enableEdicion() {
        $(".resultados.tabla-con-botones").addClass("disabled");
        $("input", ".buscador").prop("readonly", true);
        $("button", ".buscador").prop("disabled", true);
        $("dt > span", ".resultados.tabla-con-botones").addClass("disabled");
        $("dt > span", $(".tabla-con-botones", container)).removeClass("hidden");
        $(".modal-footer", container).removeClass("hidden");
        $("input, textarea", $("#info-mensura", container)).removeAttr("readonly");
        $("select", $("#info-mensura", container)).removeAttr("disabled");
        $("#btnGrabar", container).on("click", () => $("#form-mensura", container).submit());
        $("#btnCancelar", container).on("click", undo);
        $("#btnAgregarParcela", container).on("click", addParcelas);
        $("#btnEliminarParcela", container).on("click", removeParcela);
        $("#btnAgregarMensuraAsociada", container).on("click", addMensurasRelacionadas);
        $("#btnEliminarMensuraAsociada", container).on("click", removeMensuraRelacionada);
        $("#btnAgregarDocumento", container).on("click", addDocumento);
        $("#btnModificarDocumento", container).on("click", editDocumento);
        $("#btnEliminarDocumento", container).on("click", removeDocumento);

        delay(adjustScrollbars, 10);
    }
    function disableEdicion() {
        $(".resultados.tabla-con-botones").removeClass("disabled");
        $("input", ".buscador").prop("readonly", false);
        $("button", ".buscador").prop("disabled", false);
        $("dt > span:not([on-off])", ".resultados.tabla-con-botones").removeClass("disabled");
        $("dt > span", $(".tabla-con-botones:not(.resultados)", container)).addClass("hidden");
        $(".modal-footer", container).addClass("hidden");
        $("input, textarea", $("#info-mensura", container)).attr("readonly", "readonly");
        $("select", $("#info-mensura", container)).attr("disabled", "disabled");
        $("#btnCancelar,#btnGrabar", container).off("click");
        $("#btnAgregarParcela,#btnEliminarParcela,#btnAgregarMensuraAsociada,#btnEliminarMensuraAsociada", container).off("click");
        $("#btnAgregarDocumento,#btnModificarDocumento,#btnEliminarDocumento", container).off("click");

        delay(adjustScrollbars, 10);
    }
    function showData({ detalle, parcelas: parcelasAux, documentos: documentosAux, mensurasRelacionadas: mensurasAux }) {
        hideLoading();

        $("#IdMensura", container).val(detalle.IdMensura);
        $("#Departamento", container).val(detalle.Departamento);
        $("#Numero", container).val(detalle.Numero);
        $("#Anio", container).val(detalle.Anio);
        $("#Descripcion", container).val(detalle.Descripcion);
        $("#IdTipoMensura", container).val(detalle.IdTipoMensura);
        $("#IdEstadoMensura", container).val(detalle.IdEstadoMensura);
        $("#FechaPresentacion", container).val(FormatFechaHora(detalle.FechaPresentacion));
        $("#FechaAprobacion", container).val(FormatFechaHora(detalle.FechaAprobacion));
        $("#Observaciones", container).val(detalle.Observaciones);

        parcelas = parcelasAux;
        tableParcelas.ajax.reload().draw();

        documentos = documentosAux;
        tableDocumentos.ajax.reload().draw();

        mensuras = mensurasAux;
        tableMensuras.ajax.reload().draw();

        delay(adjustScrollbars, 10);
    }
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
   
    async function addDocumento() {
        try {
            const data = await loadDocumento(),
                documento = {
                    IdDocumento: data.id_documento,
                    Tipo: data.Tipo.Descripcion,
                    Fecha: data.fecha,
                    Nombre: data.nombre_archivo,
                    Extension: data.extension_archivo,
                    idMensuraDocumento: idMensuraDocumentoGenerator.next().value,
                    Descripcion: data.descripcion
                };

            documentos = store.addDocumento(documento);
            tableDocumentos.ajax.reload().draw();
        } catch {
            mostrarMensajeError(["Ha ocurrido un error al agregar el documento."], "Agregar Documento", true);
        }
    }

    async function editDocumento() {
        try {
            const current = tableDocumentos.row(".selected").data(),
                data = await loadDocumento(current),
                documento = {
                    IdDocumento: data.id_documento,
                    Tipo: data.Tipo.Descripcion,
                    Fecha: data.fecha,
                    Nombre: data.nombre_archivo,
                    Extension: data.extension_archivo,
                    IdMensuraDocumento: current.IdMensuraDocumento,
                    Descripcion: data.descripcion
                };

            documentos = store.updateDocumento(current, documento);
            tableDocumentos.ajax.reload().draw();
        } catch {
            mostrarMensajeError(["Ha ocurrido un error al agregar el documento."], "Agregar Documento", true);
        }
    }
    async function removeParcela() {
        try {
            const row = tableParcelas.row(".selected"),
                parcela = row.data();
            if (parcela) {
                parcelas = store.removeParcela(parcela);
            }
            $(row.node()).removeClass("selected");
            toggleActionButtons(row.node());
            tableParcelas.ajax.reload().draw();
        } catch {
            mostrarMensajeError(["Ha ocurrido un error al eliminar la parcela."], "Eliminar Parcela", true);
        }
    }
    async function removeDocumento() {
        try {
            const row = tableDocumentos.row(".selected"),
                documento = row.data();
            if (documento) {
                documentos = store.removeDocumento(documento);
            }
            $(row.node()).removeClass("selected");
            toggleActionButtons(row.node());
            tableDocumentos.ajax.reload().draw();
        } catch {
            mostrarMensajeError(["Ha ocurrido un error al eliminar el documento."], "Eliminar Documento", true);
        }
    }
    async function removeMensuraRelacionada() {
        try {
            const row = tableMensuras.row(".selected"),
                mensura = row.data();
            if (mensura) {
                mensuras = store.removeMensuraRelacionada(mensura);
            }
            $(row.node()).removeClass("selected");
            toggleActionButtons(row.node());
            tableMensuras.ajax.reload().draw();
        } catch {
            mostrarMensajeError(["Ha ocurrido un error al eliminar la relación con la mensura."], "Eliminar Mensura Relacionada", true);
        }
    }
    async function addParcelas() {
        const data = {
            tipos: BuscadorTipos.Parcelas,
            multiSelect: true,
            verAgregar: false,
            titulo: 'Buscar Parcelas',
            campos: ["Nomenclatura"],
            readonlyText: false
        };
        try {
            const parcelasAux = (await buscarObjetos(data))
                .map(p => ({
                    IdParcelaMensura: idParcelaMensuraGenerator.next().value,
                    IdParcela: Number(p[1]),
                    Nomenclatura: p[0]
                }));
            parcelas = store.addParcelas(parcelasAux);
            tableParcelas.ajax.reload().draw();
        } catch {
            mostrarMensajeError(["Ha ocurrido un error al agregar la parcela."], "Agregar Parcela Relacionada", true);
        }
    }
    async function addMensurasRelacionadas() {
        const data = {
            tipos: BuscadorTipos.Mensuras,
            multiSelect: true,
            verAgregar: false,
            titulo: 'Buscar Mensuras',
            campos: ["Descripcion"],
            readonlyText: false
        };
        try {
            const mensurasAux = (await buscarObjetos(data))
                .map(p => ({
                    IdMensuraRelacionada: idMensuraRelacionadaGenerator.next().value,
                    MensuraDescripcion: p[0],
                    IdMensuraOrigen: Number(p[1]),
                }));
            mensuras = store.addMensurasRelacionadas(mensurasAux);
            tableMensuras.ajax.reload().draw();
        } catch {
            mostrarMensajeError(["Ha ocurrido un error al agregar la mensura relacionada."], "Agregar Mensura Relacionada", true);
        }
    }
    async function showFile(domRow, evt) {
        const selectedRow = tableDocumentos.row(".selected").node(),
            documento = tableDocumentos.row(domRow).data();

        if (!documento) return;

        showLoading();
        if (selectedRow === domRow) {
            evt.stopImmediatePropagation();
        }
        try {
            externalForm.html(await store.showFile(documento))
                .one("hidden.bs.modal", () => externalForm.empty());
        } catch (ex) {
            mostrarMensajeError(["Ha ocurrido un error al visualizar el documento."], "Visualizar Documento", true);
        } finally {
            hideLoading();
        }
    }

    async function save(mensura) {
        try {
            if (mensura.Anio == "") {
                mostrarMensajeError(["El campo AÑO es obligatorio."], "Guardar Mensura", true);
                return;
            }
            showLoading();
            await store.save(mensura);
            mostrarMensajeGeneral(["Se ha guardado la mensura correctamente"], "Guardar Mensura");
            undo();
        } catch (error) {
            mostrarMensajeError(["Se ha producido un error y no se han podido guardar los cambios.", error.responseText], "Guardar Mensura", true);
        } finally {
            hideLoading();
        }
    }
    function undo() {
        $(tableParcelas.row(".selected").node()).trigger("click");
        $(tableMensuras.row(".selected").node()).trigger("click");
        $(tableDocumentos.row(".selected").node()).trigger("click");

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

    async function rowResultadoClicked(data, evt) {
        $(tableParcelas.row(".selected").node()).trigger("click");
        $(tableMensuras.row(".selected").node()).trigger("click");
        $(tableDocumentos.row(".selected").node()).trigger("click");


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
            mostrarMensajeError(["Ha ocurrido un error al cargar la mensura."], "Cargar Mensura", true);
        }
    }

    async function rowClicked(domRow) {
        $(domRow).toggleClass("selected")
            .siblings().removeClass("selected");

        toggleActionButtons(domRow);
    }

    async function openMantenedor(domRow, evt) {
        const selectedRow = tableParcelas.row(".selected").node(),
            parcela = tableParcelas.row(domRow).data();
        if (selectedRow === domRow) {
            evt.stopImmediatePropagation();
        }
        showLoading();
        try {
            externalForm.html(await store.loadMantenedor(parcela))
                .find('.modal').first() .one('hidden.bs.modal', () => { externalForm.empty(); });
        } catch (ex) {
            mostrarMensajeError(["Ha ocurrido un error al abrir el mantenedor parcelario."], "Visualizar Documento", true);
        } finally {
            hideLoading();
        }
    }

    async function buscarObjetos(data) {
        externalForm.html(await store.loadBuscador(data))
            .one("hidden.bs.modal", () => {
                $(window).off("seleccionAceptada");
                externalForm.empty();
            });

        return new Promise((ok) => {
            $(window).one("seleccionAceptada", (evt) => {
                let seleccion = evt.seleccion;
                if (!data.multiSelect) {
                    seleccion = [evt.seleccion];
                }

                if (seleccion) {
                    ok(seleccion.map(p => p.slice(1)));
                } else {
                    ok([]);
                }
            });
        });
    }

    async function loadDocumento(documento, readonly) {
        showLoading();
        externalForm.html(await store.loadDocumento(documento, readonly))
            .one("hidden.bs.modal", () => externalForm.empty());

        return new Promise((ok) => {
            $(document).off("documentoGuardado")
                .one("documentoGuardado", (evt) => ok(evt.documento));
        });
    }

    const resultadoBusquedaOpts = {
        ajax: (data, callback) => {
            const fn = async (data, callback) => {
                showLoading();
                try {
                    const params = { ...data, ...{ search: { value: $("[type='text']", $("#form-buscador", container)).val() } } };
                    callback(await store.search(params));
                    delay(adjustScrollbars, 10);
                } catch {
                   mostrarMensajeError(["Ha ocurrido un error al realizar la búsqueda."], "Buscar Mensuras", true);
                } finally {
                    hideLoading();
                }
            };
            debounce(fn.bind(null, data, callback), 250);
        },
        serverSide: true,
        processing: true,
        autoWidth: false,
        columns: [
            { title: "Descripci&oacute;n", data: "Descripcion", width: "40%" },
            { title: "Tipo", data: "Tipo", width: "30%" },
            { title: "Estado", data: "Estado", width: "30%" },
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

    const parcelasOpts = {
        ajax: (_, callback) => {
            callback({ data: parcelas });
            delay(adjustScrollbars, 10);
        },
        autoWidth: false,
        columns: [
            { data: "Nomenclatura", title: "Nomenclatura", width: "90%" },
            {
                width: "10%", className: "text-center",
                render: () => `<span title="Abrir Mantenedor Parcelario" class="fa fa-th mantenedor" aria-hidden="true"></span>`
            }
        ],
        createdRow: function (row) {
            $(row).on("click", rowClicked.bind(null, row));
            $(".mantenedor", row).on("click", openMantenedor.bind(null, row));
        },
        drawCallback: function () {
            tableParcelas.columns.adjust();
        },
        initComplete: function () {
            $(this)
                .on("page.dt", function () {
                    $(tableParcelas.row(".selected").node()).trigger("click");
                    delay(adjustScrollbars, 10);
                });
        }
    };
    const tableParcelas = initGrid("grillaParcelas", parcelasOpts);

    const mensurasOpts = {
        ajax: (_, callback) => {
            callback({ data: mensuras })
            delay(adjustScrollbars, 10);
        },
        autoWidth: false,
        columns: [
            { title: "Descripci&oacute;n", data: "MensuraDescripcion", width: "60%" },
            {
                width: "40%", title: "Tipo",
                render: (_, __, data) => `${(data.IdMensuraOrigen ? "Origen" : "Destino")}`
            }
        ],
        createdRow: function (row) {
            $(row).on("click", rowClicked.bind(null, row));
        },
        drawCallback: function () {
            tableMensuras.columns.adjust();
        },
        initComplete: function () {
            $(this)
                .on("page.dt", function () {
                    $(tableMensuras.row(".selected").node()).trigger("click");
                    delay(adjustScrollbars, 10);
                });
        }
    };
    const tableMensuras = initGrid("grillaMensurasRelacionadas", mensurasOpts);

    const documentosOpts = {
        ajax: (_, callback) => {
            callback({ data: documentos })
            delay(adjustScrollbars, 10);
        },
        autoWidth: false,
        columns: [
            { title: "Tipo", data: "Tipo", width: "40%" },
            {
                title: "Fecha", data: "Fecha", width: "20%",
                render: (value) => FormatFechaHora(value, false)
            },
            {
                title: "Archivo", data: "Nombre", width: "30%",
                render: (value, _, data) => `<span title="${data.Descripcion}">${value}</span>` 
            },
            {
                width: "10%", className: "text-center",
                render: () => `<span title="Ver Archivo" class="fa fa-eye visualizador" aria-hidden="true"></span>`
            }
        ],
        createdRow: function (row) {
            $(row).on("click", rowClicked.bind(null, row));
            $(".visualizador", row).on("click", showFile.bind(null, row));
        },
        drawCallback: function () {
            tableDocumentos.columns.adjust();
        },
        initComplete: function () {
            $(this)
                .on("page.dt", function () {
                    $(tableDocumentos.row(".selected").node()).trigger("click");
                    delay(adjustScrollbars, 10);
                });
        }
    };
    const tableDocumentos = initGrid("grillaDocumentos", documentosOpts);

    $("#form-buscador", container).on("submit", async (evt) => {
        evt.preventDefault();
        tableResultado.ajax.reload().draw();
   });

    store.onDetailLoaded(showData);

    $("#btnAgregarMensura", container).on("click", function () {
        $(tableResultado.row(".selected").node()).trigger("click");
        enableEdicion();
    });
    $("#btnModificarMensura", container).on("click", enableEdicion);
    $("#btnEliminarMensura", container).on("click", async function () {
        const row = tableResultado.row(".selected"),
            data = row.data();
        if (data) {
            console.log(data);
            const confirma = await mostrarMensajeGeneral([`¿Está seguro que desea eliminar los datos de la mensura ${data.Descripcion}?`], "Eliminación de Mensura", true);
            if (!confirma) return;
            showLoading();
            try {
                await store.remove(row.data());
                toggleActionButtons(row.node());
                row.remove().draw();
            } catch (error) {
                mostrarMensajeError(["Ha ocurrido un error al eliminar la mensura."], "Eliminación de Mensura", true);
            } finally {
                hideLoading();
            }
        }
    });

    $("#form-mensura", container).on("submit", function (evt) {
        evt.stopPropagation();
        const mensura = $(this).serializeObject();
        save(mensura);
        return false;
    });
}
//# sourceURL=mensura.js

