function BandejaTramitesController(container, profesional, store) {
    const scrollableContent = $(".body-content", container);
    let lastActionLabel;

    $(document).ready(init);
    $(window).resize(adjustScrollbars);

    function init() {
        $(container)
            .one("shown.bs.modal", hideLoading)
            .modal("show");
        scrollableContent.niceScroll(getNiceScrollConfig());
        $(".filtros .date.form-control", container).datepicker(getDatePickerConfig({ clearBtn: true }));
        delay(() => $(".hidden-preserve", container).removeClass("hidden-preserve"), 2500);
    }

    function activeRoleChanged(roleTable) {
        roleTable.columns.adjust();
        adjustScrollbars();
    }

    function adjustScrollbars() {
        delay(() => {
            scrollableContent.getNiceScroll().resize();
            scrollableContent.getNiceScroll().show();
        }, 10);
    }

    function filterChanged(table, evt) {
        const colIdx = table.columns()[0].filter(c => table.column(c).visible()).find((_, idx) => idx === $(evt.target).parents("th").index()),
            col = table.column(colIdx);
        if (col.search() !== evt.target.value) {
            col.search(evt.target.value).draw();
        }
    }

    function rowClicked(row, data) {
        $(row).toggleClass('selected');
        store.toggleSelection(data);
    }

    function availableActionsChanged(available) {
        const btnAcciones = $("button", $(".btn-acciones", container)).addClass("disabled").attr("disabled", "disabled"),
            ulAcciones = $("ul", $(".btn-acciones", container)).empty();

        if (!available || (!available.Generales.length && !available.Seleccion.length)) return;

        const acciones = [],
            hasGenerales = available.Generales.length,
            hasSeleccion = available.Seleccion.length,
            agregarAccion = accion => acciones.push(`<li><a href="javascript:void(0)" data-action="${accion.IdAccion}">${accion.Nombre}</a></li>`),
            agregarSeparador = () => acciones.push('<li><div class="dropdown-divider"></div></li>');

        hasGenerales && available.Generales.forEach(agregarAccion);
        hasGenerales && hasSeleccion && agregarSeparador();
        hasSeleccion && available.Seleccion.forEach(agregarAccion);

        ulAcciones.html(acciones.join(""));
        $("a", ulAcciones).on("click", callAction);
        btnAcciones.removeClass("disabled").removeAttr("disabled");
    }

    async function callAction(evt) {
        try {
            showLoading();
            lastActionLabel = evt.target.text;
            await store.executableAction($(evt.target).data("action"));
        } catch {
            hideLoading();
            console.log("error");
        }
    }

    function openForm({ html }) {
        try {
            const form = $(`${container} + #form-container`)
                .off("formCerrado")
                .on("formCerrado", () => store.refreshTable())
                .off("mostrarMensaje")
                .on("mostrarMensaje", (_, { title, messages, type }) => mostrarMensaje(title, messages, type)).empty();
            form.html(html);
        } catch {
            hideLoading();
        }
    }

    function dataReceived(data) {
        if (data.error) {
            hideLoading();
            mostrarMensaje(lastActionLabel, data.mensajes, TIPO_MSG.ERROR);
        }
    }

    function initGrid(grid, options) {
        const tableDOM = $(`#${grid}`, container),
            role = Number(tableDOM.data("role-grid")),
            gridOptions = {
                ...{
                    ajax: (data, callback) => {
                        (async (data, callback) => {
                            callback(await store.search(table, data));
                            adjustScrollbars();
                        })(data, callback);
                    },
                    autoWidth: false,
                    destroy: true,
                    pageLength: 10,
                    dom: "tr<'row'<'col-sm-6'i><'col-sm-6'p>>",
                    orderCellsTop: true,
                    processing: true,
                    serverSide: true,
                    paging: true,
                    language: { url: `${BASE_URL}Scripts/dataTables.spanish.txt` },
                    drawCallback: function () {
                        table.columns.adjust();
                    },
                    createdRow: function (row, data) {
                        $(row).on("click", rowClicked.bind(null, row, data));
                    },
                    initComplete: function () {
                        $(".filtros input", tableDOM).on("input change", evt => debounce(filterChanged.bind(null, table), 300, evt));
                        $(".filtros select:not(select[name='filtroAsunto'])", tableDOM).on("change", filterChanged.bind(null, table));
                        $(".filtros select[name='filtroAsunto']", tableDOM).on("change", async function (evt) {
                            const filtroCausa = $(".filtros select[name='filtroCausa']", tableDOM).empty();
                            table.column($(filtroCausa).parents("th").index()).search("");

                            const objs = await store.getCausas(evt.target.value);
                            objs.forEach(o => filtroCausa.append(`<option value="${o.Value}">${o.Text}</option>`));

                            filterChanged(table, evt);
                        });
                    },
                }, ...options
            },
            table = tableDOM.DataTable(gridOptions);

        return { role, table };
    }

    function mostrarMensaje(title, messages, type) {
        return new Promise((ok) => {
            const modal = $("#mensajeBandeja"),
                footer = $(".modal-footer", modal).hide(),
                btn = $("[role='button']", footer).off("click"),
                mensajeAlerta = $("[role='alert']", modal)
                    .removeClass("alert-success alert-info alert-warning alert-danger");

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

    store.onActiveRoleChanged(activeRoleChanged);
    store.onAvailableActionsChanged(availableActionsChanged);
    store.onFormOpened(openForm);
    store.onTableRefreshed(hideLoading);
    store.onDataReceived(dataReceived);

    const cols = [
        { name: "IdTramite", data: "IdTramite", width: "7%" },
        { name: "Numero", data: "Numero", defaultContent: "", width: "8%" },
        { name: "Profesional", data: "Profesional", defaultContent: "", width: "12%" },
        { name: "Asunto", data: "Asunto", width: "20%" },
        { name: "Causa", data: "Causa", width: "25%" },
        { name: "Estado", data: "Estado", width: "8%" },
        { name: "Prioridad", data: "Prioridad", width: "8%" },
        { name: "UsuarioSectorActual", data: "UsuarioSectorActual", width: "12%", defaultContent: "" },
        { name: "SectorActual", data: "SectorActual", width: "12%", defaultContent: "" },
        { name: "FechaUltimaActualizacion", data: "FechaUltimaActualizacion", defaultContent: "" },
    ], columnProfesional = 2, columnUsuarioActual = 7, columnSectorActual = 8;

    const colsDef = [
        { targets: columnProfesional, visible: !profesional },
        { targets: columnUsuarioActual, visible: false },
        { targets: columnSectorActual, visible: profesional },
    ], order = [cols.length - 1, "desc"],
        optsPropios = {
            columns: cols,
            columnDefs: colsDef,
            order,
        };
    const { role, table } = initGrid("grilla-propios", optsPropios);
    store.setActiveRole(role, table);

    if (!profesional) {
        $("a[href=#tabSector]", scrollableContent).one("show.bs.tab", () => {
            const optsSector = {
                columns: cols,
                columnDefs: colsDef.filter(t => t.targets !== columnUsuarioActual),
                order,
            };
            initGrid("grilla-sector", optsSector);
        });
        $("a[href=#tabCatastro]", scrollableContent).one("show.bs.tab", () => {
            const optsCatastro = {
                columns: cols,
                columnDefs: colsDef.filter(t => t.targets !== columnSectorActual),
                order,
            };
            initGrid("grilla-catastro", optsCatastro);
        });
        $("a[data-toggle='tab']", scrollableContent).on("shown.bs.tab", (evt) => {
            const tab = evt.target.href.split("#")[1],
                role = Number($("table[data-role-grid]", `[role="tabpanel"]#${tab}`).data("role-grid"));
            store.setActiveRole(role);
        });
    }

    $("#refreshButton", container).on("click", () => store.refreshTable());
}
//# sourceURL=bandeja.js