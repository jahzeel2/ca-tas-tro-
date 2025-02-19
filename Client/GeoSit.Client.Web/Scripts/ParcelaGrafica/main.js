function AdministradorParcelasGraficosController(container, store) {
    let parcelas = [],
        estados = {
            SIN_SELECCION: 1,
            SIN_GRAFICO: 2,
            TIENE_GRAFICO: 3,
        },
        tiposMensaje = {
            INFO: 1,
            WARNING: 2,
            ERROR: 3,
            SUCCESS: 4,
        };

    function mostrarMensaje(title, description, tipo, fn) {
        const modal = $("#modal-info-administrador"),
            body = $("div[role='alert']", modal),
            footer = $(".modal-footer", modal),
            button = $("[role='button']", modal);
        $(".modal-title", modal).html(title);
        $("p", body).html(description);
        footer.addClass("hidden");
        if (fn && tipo === tiposMensaje.WARNING) {
            footer.removeClass("hidden");
            button.one("click", fn);
        } else if (fn) {
            modal.modal("show").one("hidden.bs.modal", fn);
        }
        switch (tipo) {
            case tiposMensaje.WARNING:
                cls = "alert-warning";
                break;
            case tiposMensaje.ERROR:
                cls = "alert-danger";
                break;
            case tiposMensaje.INFO:
                cls = "alert-info";
                break;
            default:
                cls = "alert-success";
                break;
        }
        body.removeClass("alert-warning alert-success alert-danger alert-info").addClass(cls);
        modal.modal("show").one("hidden.bs.modal", () => button.off());
    }

    function refreshMap() {
        mapController.refrescar();
    }
    function updateButtonsState(state) {
        $("#btnAsociar, #btnDesasociar", container).addClass("hidden");
        if (state === estados.SIN_GRAFICO) {
            $("#btnAsociar", container).removeClass("hidden");
        }
        if (state === estados.TIENE_GRAFICO) {
            $("#btnDesasociar", container).removeClass("hidden");
        }
    }
    function rowClicked(evt) {
        evt.preventDefault();
        evt.stopImmediatePropagation();

        if ($(evt.currentTarget).hasClass("selected")) {
            store.unselectParcela();
        } else {
            store.selectParcela(tabla.row(evt.currentTarget).data());
        }
        $(evt.currentTarget).toggleClass("selected");
        $(evt.currentTarget).siblings().removeClass("selected");
    }
    function searchByNeighbour() {
        return new Promise(function (resolve) {
            var data = {
                tipos: `${BuscadorTipos.Parcelas},${BuscadorTipos.ParcelaSanear},${BuscadorTipos.Prescripciones}`,
                multiSelect: false,
                verAgregar: false,
                titulo: "Buscar Parcela de Referencia",
                campos: ["Partida", "nomenclatura:Nomenclatura"],
                retrieveFeatids: true
            };
            $("#formulario-externo-parcela-grafica").load(`${BASE_URL}BuscadorGenerico`, data, function () {
                $(".modal", this).one("hidden.bs.modal", function () {
                    $(window).off("seleccionAceptada");
                });
                $(window).one("seleccionAceptada", function (evt) {
                    if (evt.seleccion) {
                        resolve({ featids: evt.seleccion[4], layer: evt.seleccion[5] });
                    } else {
                        resolve();
                    }
                });
            });
        });
    }
    async function openMantenedor(domRow, evt) {
        const selectedRow = tabla.row(".selected").node(),
            parcela = tabla.row(domRow).data();

        if (selectedRow === domRow) {
            evt.stopImmediatePropagation();
            return;
        }
        showLoading();
        try {
            $("#formulario-externo-parcela-grafica").html(await store.loadMantenedor(parcela));
        } catch (ex) {
            console.log(ex);
        } finally {
            hideLoading();
        }
    }
    function initGrid() {
        const gridOptions = {
            destroy: true,
            dom: "t",
            ajax: (_, callback) => callback({ data: parcelas }),
            processing: true,
            scrollCollapse: true,
            language: { url: `${BASE_URL}Scripts/dataTables.spanish.txt` },
            autoWidth: false,
            ordering: false,
            columns: [
                { data: "nomenclatura", title: "Nomenclatura", width: "80%" },
                {
                    width: "10%", className: "text-center",
                    render: (_, __, row) => `<span style="margin-inline:auto" class="fa fa-globe black ${(store.hasGraphics(row) ? "" : "boton-deshabilitado")}" aria-hidden="true"></span>`
                },
                {
                    width: "10%", className: "text-center",
                    render: () => `<span style="margin-inline:auto" class="fa fa-th black cursor-pointer mantenedor" aria-hidden="true"></span>`
                }
            ],
            createdRow: function (row) {
                $(row).on("click", rowClicked);
                $(".mantenedor", row).on("click", openMantenedor.bind(null, row));
            },
            drawCallback: function () {
                tabla.columns.adjust();
            }
        };
        return $("#grilla", container).DataTable(gridOptions);
    }
    $(document).ready(function () {
        $(container)
            .one("shown.bs.modal", () => {
                mapController = store.initializeMap("map-tag");
                hideLoading();
            }).modal("show");
        $("form input[name=departamento]", "#modal-parcela-grafico").focus();
    });

    let tabla = initGrid();

    store.onNoSelection(() => {
        updateButtonsState(estados.SIN_SELECCION);
        mapController.limpiar();
    });

    store.onSelectedWithoutGraphic(() => {
        updateButtonsState(estados.SIN_GRAFICO);
        mapController.limpiar();
    });

    store.onSelectedWithGraphic((featids, capa) => {
        updateButtonsState(estados.TIENE_GRAFICO);
        mapController.seleccionarObjetos([featids], [capa]);
    });

    $("#btnAsociar", container).on("click", async () => {
        if (!mapController.obtenerSeleccion().seleccion.length) {
            mostrarMensaje("Asociar Gráfico", "Debe seleccionar un gráfico para poder asociarlo a la parcela.", tiposMensaje.ERROR);
            return;
        }
        showLoading();
        try {
            const result = await store.link(mapController.obtenerSeleccion().seleccion[0][0]);
            if (result.ok) {
                mostrarMensaje("Asociar Gráfico", "La asociación fue realizada correctamente.", tiposMensaje.SUCCESS, refreshMap);
            } else {
                mostrarMensaje("Asociar Gráfico", "La asociación fue realizada correctamente pero ha ocurrido un error al refrescar el mapa.", tiposMensaje.WARNING);
            }
        } catch (err) {
            let msg = "Ha ocurrido un error al asociar el gráfico a la parcela seleccionada.";
            switch (err.status) {
                case 400:
                    msg = "La parcela o el gráfico no están vigentes.";
                    break;
                case 409:
                    msg = "El gráfico está asociado a otra parcela.";
                    break;
            }
            mostrarMensaje("Asociar Gráfico", msg, tiposMensaje.ERROR);
        }
        hideLoading();
    });

    $("#btnDesasociar", container).on("click", async () => {
        if (!mapController.obtenerSeleccion().seleccion.length) {
            mostrarMensaje("Desasociar Gráfico", "Debe seleccionar un gráfico para poder desasociarlo de la parcela.", tiposMensaje.ERROR);
            return;
        }
        showLoading();
        try {
            const result = await store.unlink(mapController.obtenerSeleccion().seleccion[0][0]);
            if (result.ok) {
                mostrarMensaje("Desasociar Gráfico", "La desasociación fue realizada correctamente.", tiposMensaje.SUCCESS, refreshMap);
            } else {
                mostrarMensaje("Desasociar Gráfico", "La desasociación fue realizada correctamente pero ha ocurrido un error al refrescar el mapa.", tiposMensaje.WARNING);
            }

        } catch (err) {
            let msg = "Ha ocurrido un error al desasociar el gráfico a la parcela seleccionada.";
            switch (err.status) {
                case 400:
                    msg = "La parcela o el gráfico no están vigentes.";
                    break;
                case 409:
                    msg = "El gráfico no está asociado a la parcela.";
                    break;
            }
            mostrarMensaje("Desasociar Gráfico", msg, tiposMensaje.ERROR);
        }
        hideLoading();

    });

    $("#btnBuscarObjetoReferencia", container).on("click", async () => {
        const parcela = await searchByNeighbour();
        if (parcela && parcela.featids) {
            mapController.seleccionarObjetos([parcela.featids], [parcela.layer]);
        } else if (parcela) {
            mostrarMensaje('Buscar Parcela de Referencia', 'La parcela seleccionada no tiene gráfico asociado.', 3);
        } else {
            mostrarMensaje('Buscar Parcela de Referencia', 'No se ha seleccionado ninguna parcela.', 2);
        }
    });
    $("form", container).on("submit", async (evt) => {
        evt.preventDefault();
        showLoading();
        try {
            parcelas = await store.search(new FormData(evt.currentTarget));
            tabla.ajax.reload().draw();
        } catch {
            mostrarMensaje('Buscar Parcela de Referencia', 'Ha ocurrido un error al realizar la búsqueda.', 2);
        }
        hideLoading();
    });
}
//# sourceURL=parcelaGrafica.js