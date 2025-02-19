function FormularioValuacionController(container, editable, store) {
    let superficieTOUT = 0, trazaTOUT = 0, tabla;
    const detailContainer = $(".detalle-aptitud", container),
        modalDetalle = $(container).siblings("#modal-detalle-valuacion"),
        modalInfo = $(container).siblings("#modal-info-formulario");

    $(window).resize(ajustarmodal);

    $(".formulario-content", container).niceScroll(getNiceScrollConfig());

    function ajustarmodal() {
        var altura = $(window).height() - 180;
        $(".formulario-body").css({ "max-height": altura });
        $(".formulario-content").css({ "max-height": altura, "overflow": "hidden" });
        ajustarScrollBars();
    }
    function ajustarScrollBars() {
        $(".formulario-content").getNiceScroll().resize();
        $(".formulario-content").getNiceScroll().show();
    }
    function mostrarMensaje(mensajes, titulo, tipo) {
        $(".modal-title", modalInfo).html(titulo);
        $("[role='alert'] > p", modalInfo).html(mensajes.join("<br/>"));
        $("[role='alert']", modalInfo)
            .removeClass("alert-danger alert-success alert-info alert-warning")
            .addClass(tipo);
        $(".modal-footer", modalInfo).hide();
        $("[role='button']", modalInfo).off("click");
        $(modalInfo).modal("show").one("hidden.bs.modal", e => e.stopImmediatePropagation());
    }
    function mostrarMensajeError(mensajes, titulo, error) {
        return mostrarMensaje(mensajes, titulo, (error ? "alert-danger" : "alert-warning"));
    }
    async function mostrarMensajeGeneral(mensajes, titulo, confirmacion) {
        mostrarMensaje(mensajes, titulo, (confirmacion ? "alert-warning" : "alert-success"));
        if (confirmacion) {
            $(".modal-footer", modalInfo).show();
            return new Promise(ok => {
                $(modalInfo).off("hidden.bs.modal").one("hidden.bs.modal", evt => {
                    evt.stopImmediatePropagation();
                    ok(false)
                })
                $("[role='button']", modalInfo).on("click", ok.bind(null, true));
            });
        }
        return Promise.resolve(true);
    }

    function setPreviousAptitud(elem, aptitud) {
        $(elem).data("prev", aptitud);
    }
    function getSelectedSuperficie() {
        return tabla.row(".selected");
    }

    function aptitudChanged(domRow, evt) {
        const aptitud = parseInt(evt.target.value),
            row = tabla.row(domRow),
            superficie = store.reset(row.data(), aptitud);

        setPreviousAptitud(domRow, aptitud);
        row.data(superficie).draw();

        showLoading();
        loadDetail(row, true);
        hideLoading();
    }
    function trazaChanged(domRow, evt) {
        clearTimeout(trazaTOUT);
        trazaTOUT = setTimeout(applyDepreciacion, 500, tabla.row(domRow), evt);
    }
    function trazaBlured(domRow, evt) {
        const row = tabla.row(domRow);
        applyDepreciacion(row, evt);
    }
    function superficieChanged(domRow, evt) {
        clearTimeout(superficieTOUT);
        superficieTOUT = setTimeout(superficieBlured, 1500, domRow, evt);
    }
    async function superficieBlured(domRow, evt) {
        const row = tabla.row(domRow),
            superficie = await applyUpdatedSuperficie(row, evt);

        row.data(superficie).draw();
    }
    function evalCaracteristicas() {
        const caracteristicas = $("select", detailContainer).toArray()
            .reduce((map, cbo) => {
                if (eval(cbo.dataset["maestra"])) {
                    map.comunes.push(parseInt(cbo.value));
                } else {
                    map.propias.push(parseInt(cbo.value));
                }
                return map;
            }, { comunes: [], propias: [] });

        return caracteristicas;
    }
    async function caracteristicaChanged(evt) {
        const caracteristicas = evalCaracteristicas(),
            es_maestra = eval(evt.target.dataset["maestra"]),
            row = getSelectedSuperficie();

        await store.updateCaracteristicas(row.data(), caracteristicas.comunes, caracteristicas.propias, es_maestra);
    }
    async function applySelectedCaracteristicas(row) {
        const caracteristicas = evalCaracteristicas(),
            [actuales, _] = await store.initializeCaracteristicas(row.data(), caracteristicas.comunes, caracteristicas.propias);

        for (let actual of actuales) {
            $(`option[value="${actual}"]`, detailContainer).parent().val(actual);
        }
    }
    function applyDepreciacion(row, evt) {
        return store.updateDepreciacion(row.data(), evt.target.valueAsNumber);
    }
    function applyUpdatedSuperficie(row, evt) {
        return store.updateSuperficie(row.data(), evt.target.valueAsNumber.round(8));
    }
    function rowClicked(evt) {
        evt.preventDefault();
        evt.stopImmediatePropagation();
        $(evt.currentTarget).siblings().removeClass("selected");
        $(evt.currentTarget).addClass("selected");

        $("#btnEliminarSuperficie").removeClass("boton-deshabilitado");

        const row = tabla.row(evt.currentTarget),
            applySum = row.data().Puntaje === 0;

        loadDetail(row, applySum && editable);
    }
    function initGridSuperficies(superficies, aptitudes) {
        const gridOptions = {
            destroy: true,
            dom: "t",
            processing: true,
            paging: false,
            language: { url: `${BASE_URL}Scripts/dataTables.spanish.txt` },
            ordering: false,
            autoWidth: false,
            columns: [
                {
                    data: "Aptitud.IdAptitud", width: "25%", orderable: false,
                    render: function (data) {
                        const options = aptitudes.map(x => `<option value="${x.IdAptitud}" ${x.IdAptitud === data ? "selected" : ""}>${x.Descripcion}</option>`);
                        return `<select data-editable="${editable}" class="form-control input-sm">${options}</select>`;
                    }
                },
                {
                    data: "SuperficieHa", width: "25%", orderable: false,
                    render: (data) => `<input data-editable="${editable}" type="number" min="0" step="0.00000001" class="superficie form-control input-sm" value="${data}"/>`
                },
                {
                    data: "TrazaDepreciable", width: "15%", orderable: false,
                    render: (data) => `<input data-editable="${editable}" type="number" min="1" max="6" step="1" class="traza form-control input-sm" value="${(data === 0 ? "": data)}"/>`
                },
                {
                    data: "Puntaje", width: "15%", orderable: false,
                    render: (data) => `<input type="number" readonly="readonly" class="form-control input-sm" value="${data}"/>`
                },
                {
                    data: "PuntajeSuperficie", width: "20%", orderable: false,
                    render: (data) => `<input type="number" readonly="readonly" class="form-control input-sm" value="${data}"/>`
                },
            ],
            data: superficies,
            createdRow: function (row, data) {
                $(row).on("click", rowClicked)
                    .on("change", "select", aptitudChanged.bind(null, row))
                    .on("blur", "input.superficie:not([readonly])", superficieBlured.bind(null, row))
                    .on("input", "input.superficie:not([readonly])", superficieChanged.bind(null, row))
                    .on("blur", "input.traza:not([readonly])", trazaBlured.bind(null, row))
                    .on("input", "input.traza:not([readonly])", trazaChanged.bind(null, row));

                setPreviousAptitud(row, data.Aptitud.IdAptitud);
            },
            drawCallback: function () {
                tabla.columns.adjust();
                ajustarScrollBars();
            },
            initComplete: function () {
                if ($(this).dataTable().api().data().length) {
                    $(this).dataTable().api().row(0).nodes().to$().click();
                }
            }
        };
        return $("#grillaSuperficies", container).DataTable(gridOptions);
    }
    async function confirm() {
        try {
            showLoading();
            await store.save();
            $(container).trigger("valuacion-generada");
            $(container).modal("hide");
        } catch {
            mostrarMensajeError(["Ha ocurrido un error al generar la valuación."], "Valuación", true);
        }
        finally {
            hideLoading();
        }
    }

    async function loadDetail(row, sum) {
        detailContainer.html(await store.loadDetail(row.data(), editable));
        applySelectedCaracteristicas(row, sum);
    }

    $("#btnAgregarSuperficie", container).on("click", () => {
        const addedRow = tabla.rows.add([store.add()]).draw().nodes()[0];
        setTimeout((row) => $(row).click(), 200, addedRow);
    });

    $("#btnEliminarSuperficie", container).on("click", async () => {
        const confirma = await mostrarMensajeGeneral(["¿Está seguro que desea eliminar este registro?"], "Eliminación de Aptitud", true);
        if (!confirma) return;
        const row = getSelectedSuperficie();
        store.remove(row.data());
        row.remove().draw();
        $("#btnEliminarSuperficie").addClass("boton-deshabilitado");
        detailContainer.empty();
    });

    $(detailContainer).on("change", "select", caracteristicaChanged);

    $("#form-valuacion", container).on("submit", async (evt) => {
        evt.preventDefault();
        showLoading();
        const [ok, errores] = await store.validate();
        if (!ok) {
            hideLoading();
            mostrarMensajeError(errores, "Formulario de Valuación", true);
            return;
        }
        try {
            $("#detalle-content", modalDetalle).html(await store.loadPreview());
            $(modalDetalle)
                .one("hidden.bs.modal", evt => {
                    evt.stopImmediatePropagation();
                    $("[role='button']", modalDetalle).off("click");
                })
                .on("shown.bs.modal", () => $("[role='button']", modalDetalle).on("click", confirm))
                .modal("show");
        } catch (err) {
            let errors = ["Ha ocurrido un error al calcular la valuación"];
            if (Array.isArray(err)) {
                errors = err;
            }
            mostrarMensajeError(["No se pudo obtener el detalle de la valuación.", "", ...errors], "Vista Previa de Valuación", true);
        } finally {
            hideLoading();
        }
    });

    store.onSuperficieUpdated(async (superficieValuada) => {
        const resumenContainer = $(".superficies-container");
        const superficieParcela = parseFloat($(".superficie-parcela input[type=hidden]", resumenContainer).val());
        resumenContainer.html(await store.refreshResumenSuperficies(superficieParcela, superficieValuada));
    });

    store.onPuntajeUpdated((puntajeValuado) => {
        const puntajeContainer = $(".puntaje-valuado");
        $("input[type=number]", puntajeContainer).val(puntajeValuado);
    });

    store.onCaracteristicasUpdated((data) => {
        tabla.rows().data().each((_, row) => {
            const tr = tabla.row(row),
                current = tr.data(),
                updated = data.find(d => d.IdSuperficie === current.IdSuperficie);

            updated && tr.data(updated);
        });
        tabla.draw();
    });

    store.onInitialized((superficies, aptitudes) => {
        tabla = initGridSuperficies(superficies, aptitudes);
        $(container)
            .one("shown.bs.modal", () => {
                ajustarmodal();
                hideLoading();
            })
            .modal("show");
    });
    store.initialize();
}
//# sourceURL=formulario.js