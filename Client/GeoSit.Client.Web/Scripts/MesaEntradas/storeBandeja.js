const store = function (service) {
    const events = {
        ACTIVE_ROLE_CHANGED: "active-role-changed",
        AVAILABLE_ACTIONS_CHANGED: "available-actions-changed",
        ROWS_SELECTION_CHANGED: "rows-selection-changed",
        TABLE_REFRESHED: "table-refreshed",
        FORM_OPENED: "form-opened",
        DATA_RECEIVED: "data-received"
    };
    let tables = {}, subscriptions = {}, activeRole;

    function roleInTables(role) {
        return Object.hasOwn(tables, role);
    }
    function search(table, filters) {
        tables[activeRole] = { ...tables[activeRole], ...{ selection: [], table } };
        notify(events.ROWS_SELECTION_CHANGED);
        refreshAvailableActions(activeRole);
        return service.search(activeRole, filters);
    }
    async function toggleSelection(row) {
        const { selection } = tables[activeRole],
            idx = selection.indexOf(row);
        if (idx === -1) {
            selection.push(row);
        } else {
            selection.splice(idx, 1);
        }
        refreshAvailableActions(activeRole);
    }
    function refreshAvailableActions(role) {
        const { selection } = tables[role];

        debounce(async (selection) => {
            const availableActions = await service.getAvailableActions(role, selection);
            notify(events.AVAILABLE_ACTIONS_CHANGED, availableActions);
        }, 499, selection || []);
    }
    function getCausas(asunto) {
        return service.getCausas(asunto);
    }
    function setActiveRole(role, table) {
        if (roleInTables(role)) {
            const { table } = tables[role];
            notify(events.ACTIVE_ROLE_CHANGED, table);
        } else {
            tables[role] = { table };
        }
        refreshAvailableActions(role);
        activeRole = role;
    }
    function refreshTable() {
        if (roleInTables(activeRole)) {
            const { table } = tables[activeRole]
            table.ajax.reload();
            notify(events.TABLE_REFRESHED, table);
        }
    }
    async function executableAction(action) {
        const { selection } = tables[activeRole],
            { type, data } = await service.executableAction(activeRole, action, selection);

        if (type === "html") {
            notify(events.FORM_OPENED, { html: data });
        } else if (type === "none") {
            refreshTable();
        } else if (type === "json") {
            notify(events.DATA_RECEIVED, data);
        } else if (type === "file") {
            console.log("todo file");
        }
    }
    function notify(event, ...params) {
        for (let sub of (subscriptions[event] || [])) {
            sub(...params);
        }
    }
    function on(notification, fn) {
        const subscription = Object.assign(Object.defineProperty({}, notification, { value: [], writable: true }), subscriptions);
        subscriptions[notification] = subscription[notification].concat(fn);
    }

    return {
        search,
        getCausas,
        toggleSelection,
        setActiveRole,
        refreshTable,
        executableAction,

        onActiveRoleChanged: on.bind(null, events.ACTIVE_ROLE_CHANGED),
        onAvailableActionsChanged: on.bind(null, events.AVAILABLE_ACTIONS_CHANGED),
        onRowsSelectionChanged: on.bind(null, events.ROWS_SELECTION_CHANGED),
        onTableRefreshed: on.bind(null, events.TABLE_REFRESHED),
        onFormOpened: on.bind(null, events.FORM_OPENED),
        onDataReceived: on.bind(null, events.DATA_RECEIVED)
    };
}
function createStoreBandeja(service) {
    return store(service);
}
