function TramiteController(container, store, opts) {
    const scrollableContent = $(".tree-container", container),
        $modalObservaciones = $(container).siblings("#modalObservaciones"),
        $modalNotas = $(container).siblings("#modalNotas"),
        MOMENT_DATE_TIME_FORMAT = "L LTS",
        MOMENT_DATE_FORMAT = "L",
        ACTIONS = {
            GRABAR: 0,
            INGRESAR: 451,
            FINALIZAR: 454,
            REINGRESAR: 461,
            INFORME_HOJA_RUTA: 465,
            RECIBIR: 450,
            DERIVAR: 449,
            ANEXAR_INFORME: 487,
            OBSERVAR: 456,
            SOLICITAR_RESERVA: 523,
            CONFIRMAR_RESERVAR: 524,
            REASIGNAR: 518,
            FIRMAR_INFORME: 530,
            ACTUALIZAR_INFORME: 531,
        },
        CAUSAS = {
            CAMBIO_TITULARIDAD: 32,
            CERTIFICADO_CATASTRAL: 33,
            CERTIFICADO_VALUACION: 15,
            CONSULTA_SOLICITUD_COPIA_HISTORICA: 28,
            COPIAS_HELIOGRAFICAS: 30,
            COPIAS_VARIAS: 29,
            FOTOINTERPRETACION: 26,
            INFORMES_VARIOS: 23,
            MENSURA_ESCANEADA: 31,
            RESPUESTA_OFICIOS: 22,
            PLANO_APROBADO: 17,
        },
        ID_ASUNTO_MENSURA = 1,
        ID_TIPO_PLANO_APROBADO = 1,
        formularioExterno = $(`${container} ~ #formulario-externo`),
        classSelected = "selected",
        classDisabled = "boton-deshabilitado",
        { userFullName, userId, readonly, profesional, assignNomenclaturas } = opts;

    let file, $tableOrigen, $tableDestinos, $tableNotas, $tableMovimientos,
        asuntoActual = Number($("#IdTipoTramite", container).val()),
        causaActual = Number($("#IdObjetoTramite", container).val());

    $(document).ready(init);
    $(window).resize(adjustScrollbars);

    function init() {
        (async () => {
            const tramite = {
                id: $("#IdTramite", container).val(),
                numero: $("#Numero", container).val(),
                fechaIngreso: $("#FechaIngreso", container).val(),
                estado: $("#IdEstado", container).val(),
                prioridad: $("#IdPrioridad", container).val(),
                asunto: asuntoActual,
                causa: causaActual,
                comprobante: $("#Comprobante", container).val(),
                monto: $("#Monto", container).val(),
                plano: $("#Plano", container).val(),
                iniciador: $("#IdIniciador", container).val(),
            };

            store.initialize(tramite);
            habilitarOpciones();
            setTabsVisibility();
            try {
                store.onAsuntoChanged(updateAsuntoSeleccionado);
                store.onCausaChanged(updateCausaSeleccionada);
                store.onOrigenesChanged(refreshOrigenes);
                store.onDestinosChanged(refreshDestinos);
                store.onNotasLoaded(loadNotas);
                store.onMovimientosLoaded(loadMovimientos);
                store.onReloaded(reloaded);

                const thingsToLoad = [store.loadDatosOrigen(), store.loadNotas(), store.loadMovimientos()];
                esAsuntoMensura() && thingsToLoad.push(store.loadDatosDestino());
                await Promise.all(thingsToLoad);

                $(container)
                    .one("shown.bs.modal", () => {
                        $tableOrigen && $tableOrigen.columns.adjust();
                        $tableMovimientos.columns.adjust();
                        $tableNotas.columns.adjust();
                        scrollableContent.niceScroll(getNiceScrollConfig());
                    })
                    .one("hidden.bs.modal", () => $(container).parent().trigger("formCerrado").empty())
                    .modal("show");
            } catch (err) {
                console.error(err);
                mostrarMensaje("Carga de Trámite", "Ha ocurrido un error al cargar los datos de destino y/u origen del trámite", TIPO_MSG.ERROR);
            }
            finally {
                hideLoading();
                store.onReset(reset);
            }

            store.onNotaAdded(addNota);
            store.onNotaChanged(reloadNota);

            $("#txtNotaArchivoFile", $modalNotas).off("change").on("change", changeFile);
            $("[data-action]", container).off("click").on("click", execute);
        })();
    }

    $("#IdPrioridad", container).on("change", (evt) => store.updatePrioridad(evt.target.value));
    $("#Plano", container).on("input", (evt) => debounce(store.updatePlano.bind(null, evt.target.value), 500));
    $("#Comprobante", container).on("input", (evt) => debounce(store.updateComprobante.bind(null, evt.target.value), 500));
    $("#Monto", container).on("input", (evt) => debounce(store.updateMonto.bind(null, evt.target.value), 500));
    $("#IdTipoTramite", container).on("change", async function (evt) {
        if (asuntoActual === 0 || (await modalConfirm("Cambio de Asunto", ["Si cambia el <strong>Asunto</strong> se borrarán los datos cargados.", "", "¿Desea continuar?"]))) {
            store.updateAsunto(evt.target.value);
        } else {
            evt.target.value = asuntoActual;
        }
    });
    $("#IdObjetoTramite", container).on("change", async function (evt) {
        if (causaActual === 0 || (await modalConfirm("Cambio de Causa", ["Si cambia la <strong>Causa</strong> se borrarán los datos cargados.", "", "¿Desea continuar?"]))) {
            store.updateCausa(evt.target.value);
        } else {
            evt.target.value = causaActual;
        }
    });
    $("#cboTipoNota").on("change", HabilitarDeshabilitarFechaAprobacion);
    $("#fecha-aprobacion", $modalNotas).datepicker(getDatePickerConfig({ format: "dd/mm/yyyy" }));
    $("#btnAgregarNota", container).on("click", () => showModalNotas("Agregar Nota"));
    $("#btnEditarNota", container).on("click", () => showModalNotas("Editar Nota", $tableNotas.row(`.${classSelected}`).data()));
    $("#btnEliminarNota", container).on("click", () => removeNota());
    $("#btnBuscarPersona", container).on("click", addIniciador);

    function setTabsVisibility() {
        if (esAsuntoMensura() && causaActual) {
            $(".tabs-title", container).removeClass("hidden");
        } else {
            $(".tabs-title", container).addClass("hidden");
        }
    }
    function reset() { }
    function initTableOrigenes() {
        $(".origenes", container).hide();
        if (!causaActual || noRequiereOrigen()) return;
        $(".origenes", container).show();
        let columns, dom = "tr", esMensura = esAsuntoMensura(), tableName = "grilla-origenes";
        if (esMensura || !esBuscadorPlanos()) {
            if (esMensura) {
                dom = "trp";
                tableName = "grilla-origenes-mensura";
            } else {
                titulo = "Inmueble Involucrado";
            }
            configurable = 2;
            columns = [
                {
                    name: "Nomenclatura", title: "NOMENCLATURA", orderable: false,
                    render: (_, __, { Propiedades }) => {
                        return Propiedades.find(p => p.Id === "KeyIdParcela").Text;
                    }
                },
                {
                    name: "Tipo", title: "TIPO", orderable: false,
                    render: (_, __, { Propiedades }) => {
                        return Propiedades.find(p => p.Id === "KeyTipoParcela").Text;
                    }
                },
                {
                    name: "Partida", title: "PARTIDA", orderable: false,
                    render: (_, __, { Propiedades }) => {
                        return Propiedades.find(p => p.Id === "KeyIdUnidadTributaria").Text;
                    }
                },
                {
                    name: "Dominio", title: "DOMINIO", orderable: false,
                    render: (_, __, { Propiedades }) => {
                        return Propiedades.find(p => p.Id === "KeyDominios").Text;
                    }
                },
                {
                    name: "UF", title: "UF", orderable: false,
                    render: (_, __, { Propiedades }) => {
                        return Propiedades.find(p => p.Id === "KeyUnidadFuncional").Text;
                    }
                },
                {
                    name: "VigenciaDesde", title: "VIG. DESDE", orderable: false,
                    render: (_, type, { Propiedades }) => {
                        const fecha = Propiedades.find(p => p.Id === "KeyVigenciaDesde").Text;
                        if (type === "sort") {
                            return moment(fecha, "DD/MM/YYYY").toDate();
                        }
                        return fecha;
                    }
                },
            ]
        } else {
            $(`.grid-row.origenes > .columna.grilla`, container).removeClass("hidden");
            configurable = 1;
            titulo = "Mensura Involucrada";
            columns = [
                {
                    name: "Mensura", title: "MENSURA", width: "auto", orderable: false,
                    render: (_, __, { Propiedades }) => {
                        return Propiedades.find(p => p.Id === "KeyIdMensura").Text;
                    }
                },
                {
                    name: "Tipo", title: "TIPO", width: "auto", orderable: false,
                    render: (_, __, { Propiedades }) => {
                        return Propiedades.find(p => p.Id === "KeyTipoMensura").Text;
                    }
                },
                {
                    name: "Estado", title: "ESTADO", width: "auto", orderable: false,
                    render: (_, __, { Propiedades }) => {
                        return Propiedades.find(p => p.Id === "KeyEstadoMensura").Text;
                    }
                },
            ];
        }
        if ($tableOrigen) {
            $tableOrigen.destroy();
        }

        if (!esMensura) {
            $("#grilla-origenes", container).empty().html(`<caption>${titulo}</caption>`);
        }
        const opts = {
            ajax: (_, callback) => callback({ data: store.getOrigenes() }),
            createdRow: (row) => $(row).on("click", rowClickOrigen),
            initComplete: habilitarBotonesOrigen,
            dom,
            destroy: true,
            autoWidth: true,
            columns
        }
        $tableOrigen = initTable(tableName, opts);
        initBtnsTableOrigen(esMensura);
    }
    function initTableDestinos() {
        if ($tableDestinos) {
            $tableDestinos.destroy();
        }
        const getClassName = (editable) => !readonly && editable ? "has-input-child" : "",
            opts = {
                ajax: (_, callback) => callback({ data: store.getDestinos() }),
                createdRow: (row) => {
                    $("input.superficie", row).on("input", evt => debounce(updateSuperficieParcela.bind(null, row), 200, evt));
                    $("input.nomenclatura", row).on("input", evt => debounce(updateNomenclaturaDestino.bind(null, row), 200, evt));
                    $("input.partida", row).on("input", evt => debounce(updatePartidaDestino.bind(null, row), 200, evt));
                    $(row).on("click", rowClickDestino);
                },
                initComplete: habilitarBotonesDestino,
                destroy: true,
                autoWidth: true,
                pageLength: 10,
                columns: [
                    {
                        name: "Nomenclatura", title: "NOMENCLATURA", width: "250px", className: getClassName(assignNomenclaturas),
                        render: (_, __, { Propiedades }) => {
                            const text = Propiedades.find(p => p.Id === "KeyIdParcela").Text;
                            if (assignNomenclaturas && esAsuntoMensura()) {
                                return `<input class="nomenclatura form-control input-sm" type="text" value="${text || ""}">`;
                            }
                            return text;
                        }
                    },
                    {
                        name: "Partida", title: "PARTIDA", orderable: false, width: "100px", className: getClassName(assignNomenclaturas),
                        render: (_, __, { Propiedades }) => {
                            const text = Propiedades.find(p => p.Id === "KeyIdUnidadTributaria").Text;
                            if (assignNomenclaturas && esAsuntoMensura()) {
                                return `<input class="partida form-control input-sm" type="text" value="${text || ""}">`;
                            }
                            return text;
                        }
                    },
                    {
                        name: "TipoParcela", title: "TIPO PARCELA", orderable: false,
                        render: (_, __, { Propiedades }) => {
                            return Propiedades.find(p => p.Id === "KeyTipoParcela").Text;
                        }
                    },
                    {
                        name: "ClaseParcela", title: "ClASE PARCELA", orderable: false,
                        render: (_, __, { Propiedades }) => {
                            return Propiedades.find(p => p.Id === "KeyClaseParcela").Text;
                        }
                    },
                    {
                        name: "Superficie", title: "SUPERFICIE", orderable: false, className: getClassName(profesional),
                        render: (_, __, { Propiedades }) => {
                            const prop = Propiedades.find(p => p.Id === "KeySuperficieParcela"),
                                value = prop.Value, umed = prop.Text;
                            if (!readonly && profesional && esAsuntoMensura()) {
                                return `<input class="superficie form-control input-sm" type="number" value="${value}"> <span>${umed}</span>`;
                            }
                            return `${value} ${umed}`;
                        }
                    },
                    {
                        name: "TipoPartida", title: "TIPO PARTIDA", orderable: false, visible: false,
                        render: (_, __, { Propiedades }) => {
                            return Propiedades.find(p => p.Id === "KeyIdTipoUnidadTributaria").Text;
                        }
                    },
                    {
                        name: "UF", title: "UF", orderable: false, visible: false,
                        render: (_, __, { Propiedades }) => {
                            return Propiedades.find(p => p.Id === "KeyUnidadFuncional").Text;
                        }
                    }
                ]
            };
        $tableDestinos = initTable("grilla-destino-mensura", opts);
        initBtnsTableDestino();
    }
    function initBtnsTableOrigen(esMensura) {
        const btnAgregarOrigen = "btnAgregarOrigen",
            btnEliminarOrigen = "btnEliminarOrigen",
            btnAgregarOrigenMensura = "btnAgregarOrigenMensura",
            btnEliminarOrigenMensura = "btnEliminarOrigenMensura",
            btnDescargarAntecedentes = "btnDescargarAntecedentes";

        for (let btn of [btnAgregarOrigen, btnEliminarOrigen, btnAgregarOrigenMensura, btnEliminarOrigenMensura, btnDescargarAntecedentes]) {
            $(`#${btn}`, container).off("click");
        }
        if (esMensura) {
            $(`#${btnAgregarOrigenMensura}`, container).on("click", addOrigenes);
            $(`#${btnEliminarOrigenMensura}`, container).on("click", removeOrigenes);
            $(`#${btnDescargarAntecedentes}`, container).on("click", saveAntecedentes);
        } else {
            $(`#${btnAgregarOrigen}`, container).on("click", addOrigenes);
            $(`#${btnEliminarOrigen}`, container).on("click", removeOrigenes);
        }
    }
    function initBtnsTableDestino() {
        const btnAgregarDestinoMensura = "btnAgregarDestinoMensura",
            btnEliminarDestinoMensura = "btnEliminarDestinoMensura",
            btnAgregarValuacion = "btnAgregarValuacion";
        for (let btn of [btnAgregarDestinoMensura, btnEliminarDestinoMensura, btnAgregarValuacion]) {
            $(`#${btn}`, container).off("click");
        }
        if (!esAsuntoMensura()) return;

        $(`#${btnAgregarDestinoMensura}`, container).on("click", addDestinos);
        $(`#${btnEliminarDestinoMensura}`, container).on("click", removeDestinos);
        $(`#${btnAgregarValuacion}`, container).on("click", addValuacion);
    }
    function updateAsuntoSeleccionado(asunto, causas, acciones) {
        const opts = causas.reduce((accum, obj) => accum += `<option value="${obj.Value}">${obj.Text}</option>`, "");
        $("#IdObjetoTramite", container).empty().html(opts);
        $(".btn-acciones > ul", container).empty().html(acciones);
        $("[data-action]", container).off("click").on("click", execute);
        asuntoActual = asunto;
        habilitarOpciones();
        if (esAsuntoMensura()) {
            initTableOrigenes();
            initTableDestinos();
        }
    }
    function updateCausaSeleccionada(causa) {
        causaActual = causa;
        habilitarOpciones();
        if (causaActual) {
            setTabsVisibility();
            initTableOrigenes();
        }
    }
    function modalConfirm(title, messages) {
        return mostrarMensaje(title, messages, TIPO_MSG.CONFIRM);
    }
    function mostrarMensaje(title, messages, type) {
        return new Promise((ok) => {
            const modal = $("#mensajeModalTramites"),
                footer = $(".modal-footer", modal).hide(),
                btn = $("[role='button']", footer).off("click"),
                mensajeAlerta = $("[role='alert']", modal).removeClass("alert-success alert-info alert-warning alert-danger");

            let msgType = "info";
            switch (type) {
                case TIPO_MSG.ERROR:
                    msgType = "danger";
                    break;
                case TIPO_MSG.SUCCESS:
                    msgType = "success";
                    break;
                case TIPO_MSG.CONFIRM:
                case TIPO_MSG.WARNING:
                    msgType = "warning";
                    break;
            }
            mensajeAlerta.addClass(`alert-${msgType}`);

            $(".modal-title", modal).text(title);

            messages = typeof (messages) === "string" && [messages] || messages;
            $("p", mensajeAlerta).html(messages.join("<br/>"));

            if (type === TIPO_MSG.CONFIRM) {
                btn.one("click", () => {
                    modal.off("hidden.bs.modal");
                    ok(true);
                });
                footer.show();
            }
            modal.off("hidden.bs.modal").one("hidden.bs.modal", () => ok(false)).modal("show");
        });
    }
    function rowClickOrigen(evt) {
        if (!evt) return;

        $(evt.currentTarget).toggleClass(classSelected).siblings().removeClass(classSelected);
        habilitarBotonesOrigen();
    }
    function rowClickDestino(evt) {
        if (!evt || $(evt.target).hasClass("input-sm")) return;
        $(evt.currentTarget).toggleClass(classSelected);
        habilitarBotonesDestino();
    }
    function rowClickNotas(usuarioNota, evt) {
        if (!evt) return;

        const botones = $("#btnEditarNota,#btnEliminarNota", container).addClass(classDisabled),
            tableRow = $(evt.currentTarget),
            isSelected = tableRow.toggleClass(classSelected).hasClass(classSelected);

        tableRow.siblings().removeClass(classSelected);

        if (isSelected && usuarioNota === userId) {
            botones.removeClass(classDisabled);
        }
    }
    function showModalNotas(title, selected, processor, readonly = false, fileonly = false) {
        return new Promise((ok) => {
            const cboTipo = $("#cboTiposNota", $modalNotas).val(""),
                hdnIdNota = $("#hdnIdNota", $modalNotas).val(Date.now() * -1),
                txtDescripcion = $("#txtNotaDescripcion", $modalNotas).val(""),
                txtArchivo = $("#txtNotaArchivo", $modalNotas).val(""),
                hdnExtension = $("#hdnExtensionArchivo", $modalNotas).val(""),
                txtObservacion = $("#txtNotaObservacion", $modalNotas).val(""),
                hdnFechaAlta = $("#FechaAlta", $modalNotas).val(moment().format(MOMENT_DATE_TIME_FORMAT)),
                txtFechaAprobacion = $("#fecha-aprobacion", $modalNotas).val("");

            if (fileonly) {
                $(".hide-when-fileonly", $modalNotas).hide();
            } else {
                $(".hide-when-fileonly", $modalNotas).show();
            }

            $("#btnNotaExplorar", $modalNotas).off("click").on("click", () => $("#txtNotaArchivoFile", $modalNotas).click());
            $("#txtNotaArchivoFile", $modalNotas).val("");
            file = null;

            if (selected) {
                cboTipo.val(selected.Documento.id_tipo_documento);
                hdnIdNota.val(selected.id_documento);
                txtDescripcion.val(selected.Documento.descripcion);
                txtArchivo.val(selected.Documento.nombre_archivo);
                hdnExtension.val(selected.Documento.extension_archivo);
                hdnFechaAlta.val(moment(selected.FechaAlta).format(MOMENT_DATE_TIME_FORMAT));
                txtFechaAprobacion.val(FormatFechaHora(selected.FechaAprobacion));
                txtObservacion.val(selected.Documento.observaciones);
            }
            if (!readonly) {
                $("#btnAceptarInfoNota", $modalNotas).off().on("click", async () => {
                    try {
                        await aceptarNota(processor);
                        $modalNotas.off("hidden.bs.modal").modal("hide");
                        ok(true);
                    } catch (error) {
                        $modalNotas.modal("hide");
                        mostrarMensaje("Nota", error, TIPO_MSG.WARNING);
                    } finally {
                        hideLoading();
                    }
                });
            }
            $(".modal-title", $modalNotas).html(title);
            $modalNotas
                .off("hidden.bs.modal")
                .one("hidden.bs.modal", ok.bind(null, false))
                .modal("show");
        });
    }
    function updateSuperficieParcela(row, evt) {
        const data = $tableDestinos.row(row).data();
        store.updateSuperficieParcelaDestino(data.Guid, evt.target.valueAsNumber);
    }
    function updateNomenclaturaDestino(row, evt) {
        const data = $tableDestinos.row(row).data();
        store.updateNomenclaturaDestino(data.Guid, evt.target.value);
    }
    function updatePartidaDestino(row, evt) {
        const data = $tableDestinos.row(row).data();
        store.updatePartidaDestino(data.Guid, evt.target.value);
    }
    function aceptarNota(processor) {
        showLoading();
        const id = Number($("#hdnIdNota", $modalNotas).val()),
            nota = {
                id_documento: id,
                FechaAlta: moment($("#FechaAlta", $modalNotas).val(), MOMENT_DATE_TIME_FORMAT).toDate(),
                FechaAprobacion: moment($("#fecha-aprobacion", $modalNotas).val(), MOMENT_DATE_FORMAT).toDate(),
                Usuario_Alta: {
                    NombreApellidoCompleto: userFullName,
                },
                UsuarioAlta: userId,
                Documento: {
                    Tipo: {
                        Descripcion: $("#cboTiposNota option:selected", $modalNotas).text(),
                    },
                    id_documento: id,
                    extension_archivo: $("#hdnExtensionArchivo", $modalNotas).val(),
                    descripcion: $("#txtNotaDescripcion", $modalNotas).val(),
                    id_tipo_documento: parseInt($("#cboTiposNota", $modalNotas).val()),
                    observaciones: $("#txtNotaObservacion", $modalNotas).val(),
                },
            };
        if (!processor) {
            processor = store.processNota
        }
        return processor(nota, file);
    }
    function changeFile(evt) {
        file = evt.target.files[0];
        let nombre_archivo = evt.target.value.substr(evt.target.value.lastIndexOf("\\") + 1),
            extension = nombre_archivo.substr(nombre_archivo.lastIndexOf(".")),
            nombre_sin_extension = nombre_archivo.substr(0, nombre_archivo.lastIndexOf("."));

        $("#hdnExtensionArchivo", $modalNotas).val(extension);

        if (!$("#txtNotaDescripcion", $modalNotas).val()) {
            $("#txtNotaDescripcion", $modalNotas).val(nombre_sin_extension);
        }

        $("#txtNotaArchivo", $modalNotas).val(nombre_archivo);
    }
    async function downloadFile(filename) {
        showLoading();
        try {
            await store.downloadFile(filename);
        } catch (err) {
            let msg = `Ha ocurrido un error al descargar el archivo <strong>${filename}</strong>.`;
            if (err.status === 404) {
                msg = `El archivo <strong>${filename}</strong> no existe.`;
            }
            mostrarMensaje("Descargar Archivo", msg, TIPO_MSG.ERROR);
        } finally {
            hideLoading();
        }
    }
    function HabilitarDeshabilitarFechaAprobacion() {
        const $selector = $("#fecha-aprobacion,#lblFechaAprobacion", $modalNotas).addClass("hidden");
        if (parseInt($("#cboTipoNota", $modalNotas).val()) === ID_TIPO_PLANO_APROBADO) {
            $selector.removeClass("hidden");
        }
    }
    async function addIniciador() {
        const data = await buscarPersonas();
        if (!data) return;
        store.updateIniciador(data[0]);
        $("#IdIniciador", container).val(data[0]);
        $("#txtIniciador", container).val(data[1]);
    }
    async function addOrigenes() {
        if (causaActual === 0) {
            mostrarMensaje("Causa", "Debe seleccionar una causa", TIPO_MSG.WARNING);
            return;
        }
        try {
            const data = await buscarObjetos();
            if (!data) return;
            showLoading();
            await store.loadDatosEspecificos(data.tipo, null, data.results.map((d) => d[1]));
        } catch (err) {
            mostrarMensaje("Agregar Origen", ["Ha ocurrido un error al agregar el objeto.", "", ...[err]], TIPO_MSG.ERROR);
        } finally {
            hideLoading();
        }
    }
    async function addDestinos() {
        if (causaActual === 0) {
            mostrarMensaje("Causa", "Debe seleccionar una causa", TIPO_MSG.WARNING);
            return;
        }
        try {
            showLoading();
            formularioExterno.html(await store.loadGeneradorDestinos())
                .one("shown.bs.modal", hideLoading)
                .one("hidden.bs.modal", () => {
                    formularioExterno
                        .off("mostrarMensaje")
                        .off("destinosGenerados")
                        .empty();
                });

            formularioExterno.one("destinosGenerados", (evt) => store.addDestinos(evt.destinos));
            formularioExterno.on("mostrarMensaje", evt => mostrarMensaje("Agregar Parcelas Destino", evt.params.messages, evt.params.type));
        } catch (err) {
            console.log(err);
        } finally {
            hideLoading();
        }
    }
    function removeDestinos() {
        store.removeDestinos($tableDestinos.rows(`.${classSelected}`).data().toArray());
    }
    async function addValuacion() {
        try {
            showLoading();
            const data = $tableDestinos.row(`.${classSelected}`).data();
            formularioExterno.html(await store.loadFormValuacion(data))
                .one("shown.bs.modal", (evt) => {
                    $(evt.target).one("valuacionGenerada", evt => store.updateValuacionParcela(data.Guid, evt.valuacion.superficies));
                })
                .one("hidden.bs.modal", (evt) => {
                    $(evt.target).off("valuacionGenerada");
                    formularioExterno
                        .off("mostrarMensaje")
                        .empty();
                });
            formularioExterno.on("mostrarMensaje", evt => mostrarMensaje("Agregar Valuación", evt.params.messages, evt.params.type));
        } catch (err) {
            console.log(err);
        } finally {
            hideLoading();
        }
    }
    async function saveAntecedentes() {
        const validations = store.validate();
        if (validations.length !== 0) {
            mostrarMensaje("Datos Incompletos", ["Por favor, verifique lo siguiente", "", ...validations], TIPO_MSG.WARNING);
            return;
        }

        const titulo = "Generación de Antecedentes";
        if (await modalConfirm(titulo, ["Una vez generados los antecedentes, no podrá modificar las parcelas de origen.", "Por favor, verifique que sean correctas.", "", "¿Desea continuar?"])) {
            try {
                showLoading();
                const { mensajes, error } = await store.saveAntecedentes();
                hideLoading();

                if (error) {
                    resultado = TIPO_MSG.ERROR;
                    texto = ["Ha ocurrido un error al generar los antecedentes", "Por favor verifique lo siguiente:", ...["", ...mensajes]];
                } else {
                    resultado = TIPO_MSG.SUCCESS;
                    texto = ["Se han generado el archivo de antecedentes correctamente."];
                }
                await mostrarMensaje(titulo, texto, resultado);
                !error && reload(false);
            } catch (err) {
                hideLoading();
                mostrarMensaje(titulo, "Ha ocurrido un error al generar el archivo de antecedentes", TIPO_MSG.ERROR);
            }
        }
    }
    function removeOrigenes() {
        store.removeOrigenes($tableOrigen.rows(`.${classSelected}`).data().toArray());
    }
    function addNota() {
        $tableNotas.ajax.reload().draw();
    }
    function reloadNota({ id_documento }) {
        $tableNotas.ajax.reload().draw();
        $($tableNotas.row((_, data) => data.id_documento === id_documento).node()).trigger("click");
    };
    async function removeNota() {
        await store.removeNota($tableNotas.row(`.${classSelected}`).data());
        $tableNotas.ajax.reload().draw();
        $("#btnEliminarNota").addClass("boton-deshabilitado");
        $("#btnEditarNota").addClass("boton-deshabilitado");
    }
    function refreshOrigenes(reloadCurrent) {
        if (!reloadCurrent) {
            initTableOrigenes();
            return;
        }
        $tableOrigen.ajax.reload().draw();
        habilitarBotonesOrigen();
    }
    function refreshDestinos(reloadCurrent) {
        if (!reloadCurrent) {
            initTableDestinos();
            return;
        }
        $tableDestinos.ajax.reload().draw();
        initBtnsTableDestino();
    }
    function loadNotas() {
        const opts = {
            ajax: (_, callback) => {
                callback({ data: store.getNotas() })
            },
            createdRow: (row, { Documento, UsuarioAlta }) => {
                $(row).on("click", rowClickNotas.bind(null, UsuarioAlta));
                $("a.file", row).on("click", (evt) => {
                    evt.stopImmediatePropagation();
                    downloadFile(evt.target.text);
                });
                $("a.observaciones", row).on("click", (evt) => {
                    evt.stopImmediatePropagation();
                    showObservaciones(`${(Documento.nombre_archivo ? "Adjunto" : Documento.Tipo.Descripcion)} ${(Documento.nombre_archivo || "")}`, Documento.observaciones);
                });
            },
            order: [0, "asc"],
            columns: [
                {
                    title: "FECHA", data: "FechaAlta", type: "date",
                    render: (value, type) => {
                        if (type === "sort") {
                            return moment(value).toDate();
                        }
                        return FormatFechaHora(value);
                    }
                },
                { title: "TIPO", data: "Documento.Tipo.Descripcion" },
                {
                    title: "USUARIO", data: "Usuario_Alta.NombreApellidoCompleto"
                },
                {
                    title: "ADJUNTO", data: "Documento.nombre_archivo",
                    render: (value) => !!value && `<a class="file" href="javascript:void(0);">${value}</a>` || ""
                },
                {
                    title: "OBS", data: "Documento.observaciones",
                    render: (value, type) => {
                        if (type === "sort") {
                            return !!value;
                        }
                        return !!value && `<a class="observaciones" href="javascript:void(0)"><i class="fa fa-file-text-o"></i></a>` || "";
                    }
                },
            ],
        }
        $tableNotas = initTable("grilla-notas", opts);
    }
    function loadMovimientos(movimientos) {
        const opts = {
            createdRow: (row, { Tipo, Observacion }) => {
                $("a.observaciones", row).on("click", showObservaciones.bind(null, `Movimiento ${Tipo}`, Observacion));
            },
            data: movimientos,
            order: [0, "desc"],
            columns: [
                {
                    title: "FECHA", data: "Fecha", type: "num",
                    render: (value, type) => {
                        if (type === "sort") {
                            return moment(value).valueOf();
                        }
                        return FormatFechaHora(value);
                    }
                },
                { title: "TIPO", data: "Tipo" },
                { title: "DESTINATARIO", data: "SectorDestino" },
                { title: "USUARIO", data: "Usuario" },
                { title: "ESTADO", data: "Estado" },
                {
                    title: "OBS", data: "Observacion",
                    render: (value, type) => {
                        if (type === "sort") {
                            return !!value;
                        }
                        return !!value && `<a class="observaciones black" href="javascript:void(0)"><i class="fa fa-file-text-o"></i></a>` || "";
                    }
                },
            ]
        };
        $tableMovimientos = initTable("grilla-movimientos", opts);
    }
    function esAsuntoMensura() {
        return asuntoActual === ID_ASUNTO_MENSURA;
    }
    function noRequiereOrigen() {
        return causasSinOrigenes().includes(causaActual);
    }
    function showObservaciones(objeto, observaciones) {
        $("p", $modalObservaciones).html(observaciones.replaceAll("\n", "<br/>"));
        $(".modal-title", $modalObservaciones).text(`Observaciones - ${objeto}`);
        $modalObservaciones.modal("show");
    }
    function habilitarOpciones() {
        if (esAsuntoMensura() || !causaActual) {
            $(".grid-row.origenes > .columna.empty", container).removeClass("hidden");
            $(".grid-row.origenes > .columna.grilla", container).addClass("hidden");
        } else {
            $(".grid-row.origenes > .columna.empty", container).addClass("hidden");
            $(".grid-row.origenes > .columna.grilla", container).removeClass("hidden");
        }
    }
    function habilitarBotonesOrigen() {
        habilitarAgregarOrigen();
        habilitarEliminarOrigen();
        habilitarDescargarAntecedentes();
    }
    function habilitarEliminarOrigen() {
        const btnName = esAsuntoMensura() ? "btnEliminarOrigenMensura" : "btnEliminarOrigen",
            btn = $(`#${btnName}`, container).addClass(classDisabled);
        if ($tableOrigen.row(`.${classSelected}`).data()) {
            btn.removeClass(classDisabled);
        }
    }
    function habilitarAgregarOrigen() {
        const btnName = esAsuntoMensura() ? "btnAgregarOrigenMensura" : "btnAgregarOrigen",
            btn = $(`#${btnName}`, container).removeClass(classDisabled);
        if (!esAsuntoMensura() && $tableOrigen.data().length !== 0) {
            btn.addClass(classDisabled);
        }
    }
    function habilitarDescargarAntecedentes() {
        const btn = $("#btnDescargarAntecedentes", container).addClass(classDisabled);
        if (esAsuntoMensura() && $tableOrigen.data().length !== 0) {
            btn.removeClass(classDisabled);
        }
    }
    function habilitarBotonesDestino() {
        const btnEliminar = $("#btnEliminarDestinoMensura", container).addClass(classDisabled),
            btnValuacion = $("#btnAgregarValuacion", container).addClass(classDisabled);
        if ($tableDestinos.row(`.${classSelected}`).data()) {
            btnEliminar.removeClass(classDisabled);
        }
        if ($tableDestinos.rows(`.${classSelected}`).data().length === 1) {
            btnValuacion.removeClass(classDisabled);
        }
    }
    function initTable(tableId, opts) {
        return $(`#${tableId}`, container).DataTable({
            ...{
                dom: "trp",
                destroy: true,
                autoWidth: true,
                pageLength: 5,
                order: [],
                language: { url: `${BASE_URL}Scripts/dataTables.spanish.txt` },
                drawCallback: function () {
                    this.api().columns.adjust();
                },
            }, ...opts
        });
    }
    function adjustScrollbars() {
        delay(() => {
            scrollableContent.getNiceScroll().resize();
            scrollableContent.getNiceScroll().show();
        }, 10);
    }
    function execute(evt) {
        const action = Number($(evt.currentTarget).data("action"));
        switch (action) {
            case ACTIONS.GRABAR:
                guardarTramite();
                break;
            case ACTIONS.INGRESAR:
            case ACTIONS.REINGRESAR:
                guardarTramite(true);
                break;
            case ACTIONS.SOLICITAR_RESERVA:
                solicitarReservas();
                break;
            case ACTIONS.FINALIZAR:
                finalizar();
                break;
            case ACTIONS.CONFIRMAR_RESERVAR:
                confirmarReservas();
                break;
            case ACTIONS.DERIVAR:
                derivarTramite();
                break;
            case ACTIONS.OBSERVAR:
                observarTramite();
                break;
            case ACTIONS.ANEXAR_INFORME:
                anexarInforme();
                break;
            case ACTIONS.FIRMAR_INFORME:
                firmarInforme();
                break;
            case ACTIONS.ACTUALIZAR_INFORME:
                actualizarInforme();
                break;
            case ACTIONS.RECIBIR:
                recibirTramite();
                break;
            case ACTIONS.REASIGNAR:
                reasignarTramite();
                break;
            case ACTIONS.INFORME_HOJA_RUTA:
                generarHojaDeRuta();
                break;
            default:
                const name = evt.currentTarget.text;
                executeAction(action, name);
                break;
        }
    }
    async function executeAction(action, title) {
        showLoading();
        try {
            const { error, mensajes } = await store.execute(action);
            hideLoading();

            let texto = "Ha ocurrido un error al ejecutar la acción.",
                resultado = TIPO_MSG.SUCCESS, reloadForm = !error;

            if (error) {
                resultado = TIPO_MSG.ERROR;
                texto = [texto, "Por favor verifique lo siguiente:", ...["", ...mensajes]].join("<br />");
            } else {
                texto = ["La acción se ha ejecutado con éxito."];
            }
            await mostrarMensaje(title, texto, resultado);
            reloadForm && reload();
        } catch (error) {
            hideLoading();
            mostrarMensaje(title, error, TIPO_MSG.ERROR);
        }
    }
    async function buscarPersonas() {
        showLoading();
        formularioExterno.html(await store.loadBuscadorPersonas())
            .one("shown.bs.modal", hideLoading)
            .one("hidden.bs.modal", () => {
                $(window).off("seleccionAceptada");
                $(window).off("agregarObjetoBuscado");
                formularioExterno.empty();
            });
        return new Promise((ok) => {
            $(window).one("seleccionAceptada", (evt) => {
                let data;
                if (evt.seleccion) {
                    data = [evt.seleccion[2], evt.seleccion[1]];
                }
                ok(data)
            });
            $(window).one("agregarObjetoBuscado", async function () {
                formularioExterno.modal("hide");
                showLoading();
                formularioExterno.html(await store.loadAdminPersonas())
                    .one("shown.bs.modal", () => {
                        $(window).one("personaAgregada", function (evt) {
                            ok([evt.persona.PersonaId, evt.persona.NombreCompleto]);
                        });
                    })
                    .one("hidden.bs.modal", () => {
                        $(window).off("personaAgregada");
                        formularioExterno.empty();
                    });
            });
        });
    }
    function esBuscadorPlanos() {
        return [CAUSAS.PLANO_APROBADO, CAUSAS.MENSURA_ESCANEADA].includes(causaActual);
    }
    function esBuscadorUTs() {
        return [CAUSAS.CERTIFICADO_VALUACION, CAUSAS.CAMBIO_TITULARIDAD, CAUSAS.CERTIFICADO_CATASTRAL].indexOf(causaActual) !== -1;
    }
    function causasSinOrigenes() {
        return [
            CAUSAS.CONSULTA_SOLICITUD_COPIA_HISTORICA, CAUSAS.COPIAS_HELIOGRAFICAS,
            CAUSAS.COPIAS_VARIAS, CAUSAS.FOTOINTERPRETACION, CAUSAS.INFORMES_VARIOS,
            CAUSAS.RESPUESTA_OFICIOS
        ];
    }
    function esBuscadorParcelas() {
        return !esBuscadorUTs() && (esAsuntoMensura() || !esBuscadorPlanos());
    }
    async function buscarObjetos() {
        if (noRequiereOrigen()) return Promise.reject("Esta causa no permite especificar un origen.");

        let formLoader = store.loadBuscadorMensuras,
            tipo = 4;
        if (esBuscadorUTs()) {
            tipo = 2;
            formLoader = store.loadBuscadorPartidas;
        } else if (esBuscadorParcelas()) {
            tipo = 1;
            formLoader = store.loadBuscadorPartidas;
        }
        showLoading();
        formularioExterno.html(await formLoader())
            .one("shown.bs.modal", hideLoading)
            .one("hidden.bs.modal", () => {
                $(window).off("seleccionAceptada");
                formularioExterno.empty();
            });

        return new Promise((ok) => {
            $(window).one("seleccionAceptada", (evt) => {
                let seleccion = evt.seleccion || [];
                if (!Array.isArray(evt.seleccion[0])) {
                    seleccion = [evt.seleccion];
                }
                const data = {
                    tipo,
                    results: seleccion.map(p => p.slice(1))
                };
                ok(data)
            });
        });
    }
    async function anexarInforme() {
        const title = "Anexar Informe del Trámite";
        try {
            if (!(await mostrarFormularioInforme(title, store.processAddedInforme, true)))
                return;
        } catch {
            mostrarMensaje(title, ["Ha ocurrido un error al anexar el informe"], TIPO_MSG.ERROR);
            return;
        }
        guardarInforme(title, ["El informe se ha anexado correctamente."]);
    }
    async function actualizarInforme() {
        const title = "Anexar Informe Actualizado";
        try {
            if (!(await mostrarFormularioInforme(title, store.processUpdatedInforme, false)))
                return;
        } catch {
            mostrarMensaje(title, ["Ha ocurrido un error al actualizar el informe"], TIPO_MSG.ERROR);
            return;
        }
        guardarInforme(title, ["El informe se ha actualizado correctamente."], false);
    }
    async function firmarInforme() {
        const profesional = $("#txtProfesional", container).val(),
            title = "Anexar Informe Firmado",
            msg = `Una vez anexado el informe firmado, los datos del trámite serán tomados como válidos y el trámite será finalizado, siéndole el mismo devuelto al profesional <em>${profesional}</em>.`;

        if (!(await modalConfirm(title, [msg, "", "¿Desea continuar?"]))) return;
        try {
            if (!(await mostrarFormularioInforme(title, store.processUpdatedInforme, false)))
                return;
        } catch {
            mostrarMensaje(title, ["Ha ocurrido un error al cargar el informe"], TIPO_MSG.ERROR);
            return;
        }
        guardarInforme(title, ["El informe se anexó correctamente.", `El trámite se ha finalizado y ha sido devuelto a <em>${profesional}</em>`], false, true);
    }
    async function mostrarFormularioInforme(titulo, procesar, esNuevo) {
        return store.hasValidInforme() || await showModalNotas(titulo, null, procesar, false, !esNuevo);
    }
    async function derivarTramite() {
        const title = "Derivar Trámite";
        try {
            formularioExterno.html(await store.loadSelectorSector())
                .off("formCerrado").one("formCerrado", async () => {
                    await mostrarMensaje(title, ["El trámite se ha derivado con éxito."], TIPO_MSG.SUCCESS);
                    reload();
                })
                .off("mostrarMensaje").on("mostrarMensaje", (_, { messages, type }) => mostrarMensaje(title, messages, type))
                .one("hidden.bs.modal", () => formularioExterno.empty());
        } catch (error) {
            mostrarMensaje(title, error, TIPO_MSG.ERROR);
        }
    }
    async function reasignarTramite() {
        const title = "Reasignar Trámite";
        try {
            formularioExterno.html(await store.loadSelectorUsuario())
                .off("formCerrado").one("formCerrado", async () => {
                    await mostrarMensaje(title, ["El trámite se ha reasignado con éxito."], TIPO_MSG.SUCCESS);
                    reload();
                })
                .off("mostrarMensaje").on("mostrarMensaje", (_, { messages, type }) => mostrarMensaje(title, messages, type))
                .one("hidden.bs.modal", () => formularioExterno.empty());
        } catch (error) {
            mostrarMensaje(title, error, TIPO_MSG.ERROR);
        }
    }
    async function guardarInforme(titulo, textos, nuevo = true, firmado = false) {
        showLoading();
        const { mensajes, error } = await store.saveInforme(nuevo, firmado);
        hideLoading();

        let resultado = TIPO_MSG.SUCCESS,
            reloadForm = !error;

        if (error) {
            resultado = TIPO_MSG.ERROR;
            textos = ["Ha ocurrido un error y el informe no pudo anexarse al trámite.", "Por favor verifique lo siguiente:", ...["", ...mensajes]].join("<br />");
        }
        await mostrarMensaje(titulo, textos, resultado);
        reloadForm && reload();
    }
    async function observarTramite() {
        const title = "Observar Trámite",
            msg = `Una vez observado, el trámite le será devuelto automáticamente al profesional <em>${$("#txtProfesional", container).val()}</em>.`;
        if (!(await modalConfirm(title, [msg, "", "¿Desea continuar?"]))) return;
        try {
            formularioExterno.html(await store.loadObservacion());
            formularioExterno
                .off("formCerrado").one("formCerrado", async () => {
                    await mostrarMensaje(title, ["El trámite se ha observado con éxito."], TIPO_MSG.SUCCESS);
                    reload();
                })
                .off("mostrarMensaje").on("mostrarMensaje", (_, { messages, type }) => mostrarMensaje(title, messages, type))
                .one("hidden.bs.modal", () => formularioExterno.empty());
        } catch (error) {
            mostrarMensaje(title, error, TIPO_MSG.ERROR);
        }
    }
    async function recibirTramite() {
        const titulo = "Recibir Trámite";
        try {
            await store.setTramiteCurrentUser();
            await mostrarMensaje(titulo, ["El trámite se ha recepcionado con éxito."], TIPO_MSG.SUCCESS);
            reload();
        } catch (error) {
            mostrarMensaje(titulo, error, TIPO_MSG.ERROR);
        }
    }
    async function reload(soloLectura) {
        showLoading();
        try {
            await store.reload(soloLectura);
        } catch (err) {
            hideLoading();
            await mostrarMensaje("Trámite - Recarga", ["Ha ocurrido un error al recargar el trámite.", "", "Se cerrará el formulario.", "Por favor, ábralo desde la bandeja nuevamente."]);
            $(container).modal("hide");
        }
    }
    function reloaded(html) {
        $(container).off("hidden.bs.modal").modal("hide");
        delay(() => $(container).parent().empty().html(html), 500);
    }
    async function guardarTramite(ingresar = false) {
        const form = $("#divDatosGenerales", container),
            bootstrapValidator = form.data("bootstrapValidator");

        const validations = store.validate();
        if (validations.length !== 0) {
            mostrarMensaje("Datos Incompletos", ["Por favor, verifique lo siguiente", "", ...validations], TIPO_MSG.WARNING);
            return;
        }

        if (true || bootstrapValidator.isValid()) {
            showLoading();
            const { mensajes, error } = await store.save(ingresar);
            hideLoading();

            let texto = `Ha ocurrido un error y el trámite no pudo ${ingresar ? "ingresarse" : "guardarse"}.`,
                titulo = ingresar ? "Ingresar" : "Grabar", resultado = TIPO_MSG.SUCCESS, reloadForm = !error;

            if (error) {
                resultado = TIPO_MSG.ERROR;
                texto = [texto, "Por favor verifique lo siguiente:", ...["", ...mensajes]].join("<br />");
            } else {
                texto = ingresar ? "El trámite se ingresó correctamente." : "Los cambios se guardaron correctamente.";
                if (mensajes) {
                    resultado = TIPO_MSG.WARNING;
                    texto = [texto, "Tenga en cuenta que:", ...["", ...mensajes]].join("<br />");
                }
            }
            await mostrarMensaje(titulo, texto, resultado);
            reloadForm && reload(ingresar);
        }
    }
    async function solicitarReservas() {
        const validations = store.validate();
        if (validations.length !== 0) {
            mostrarMensaje("Datos Incompletos", ["Por favor, verifique lo siguiente", "", ...validations], TIPO_MSG.WARNING);
            return;
        }

        showLoading();
        const { mensajes, error } = await store.askReservas();
        hideLoading();

        let texto = `Ha ocurrido un error y el trámite no pudo enviarse a la reserva de nomenclaturas.`,
            titulo = "Solicitar Reservas", resultado = TIPO_MSG.SUCCESS, reloadForm = !error;

        if (error) {
            resultado = TIPO_MSG.ERROR;
            texto = [texto, "Por favor verifique lo siguiente:", ...["", ...mensajes]].join("<br />");
        } else {
            texto = ["El trámite se envió para la reserva de nomenclaturas correctamente."];
        }
        await mostrarMensaje(titulo, texto, resultado);
        reloadForm && reload(true);
    }
    async function confirmarReservas() {
        const validations = store.validate();
        if (validations.length !== 0) {
            mostrarMensaje("Datos Incompletos", ["Por favor, verifique lo siguiente", "", ...validations], TIPO_MSG.WARNING);
            return;
        }

        const titulo = "Confirmar Reservas",
            msg = `Una vez confirmada las reservas, el trámite le será devuelto automáticamente al profesional <em>${$("#txtProfesional", container).val()}</em>.`;
        if (!(await modalConfirm(titulo, [msg, "", "¿Desea continuar?"]))) return;
        showLoading();
        const { mensajes, error } = await store.confirmReservas();
        hideLoading();

        let texto = "Ha ocurrido un error y las reservas no pudieron confirmarse.",
            resultado = TIPO_MSG.SUCCESS, reloadForm = !error;

        if (error) {
            resultado = TIPO_MSG.ERROR;
            texto = [texto, "Por favor verifique lo siguiente:", ...["", ...mensajes]].join("<br />");
        } else {
            texto = ["Las reservas han sido confirmadas correctamente."];
        }
        await mostrarMensaje(titulo, texto, resultado);
        reloadForm && reload(true);
    }
    async function finalizar() {
        const validations = store.validate();
        if (validations.length !== 0) {
            mostrarMensaje("Datos Incompletos", ["Por favor, verifique lo siguiente", "", ...validations], TIPO_MSG.WARNING);
            return;
        }

        const titulo = "Finalizar Trámite",
            msgs = [
                "Finalizar el trámite implica impactar toda la mensura en los datos vigentes, pasando a histórico lo que sea necesario.",
                "",
                `Una vez finalizado, el trámite le será devuelto automáticamente al profesional <em>${$("#txtProfesional", container).val()}</em>.`,
                "",
            ];
        if (!(await modalConfirm(titulo, msgs.concat("¿Desea continuar?")))) return;
        showLoading();
        let mensajes = null, error = null,
            texto = "Ha ocurrido un error y el trámite no pudo finalizarse.",
            resultado = TIPO_MSG.ERROR;
        try {
            ({ mensajes, error } = await store.execute(ACTIONS.FINALIZAR));
            hideLoading();
        } catch (err) {
            hideLoading();
            mostrarMensaje(titulo, ["Ha ocurrido un error al finalizar el trámite."], TIPO_MSG.ERROR);
            return;
        }

        if (error) {
            texto = [texto, "Por favor verifique lo siguiente:", ...["", ...mensajes]].join("<br />");
        } else {
            resultado = TIPO_MSG.SUCCESS;
            texto = ["Las reservas han sido confirmadas correctamente."];
        }
        await mostrarMensaje(titulo, texto, resultado);
        !error && reload(true);
    }

    async function generarHojaDeRuta() {
        showLoading();
        try {
            await store.openHojaRuta();
            hideLoading();
        } catch (error) {
            hideLoading();
            mostrarMensaje("Generar Hoja de Ruta", ["Ha ocurrido un error al generar la hoja de ruta"], TIPO_MSG.ERROR);
        }
    }
}
//# sourceURL=tramite.js