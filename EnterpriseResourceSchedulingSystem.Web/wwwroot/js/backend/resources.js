document.addEventListener("DOMContentLoaded", () => {
    const root = document.getElementById("resourcesPage");
    if (!root) return;

    const byId = id => document.getElementById(id);
    const apiUrl = root.dataset.apiUrl.replace(/\/$/, "");
    const identityApiUrl = root.dataset.identityApiUrl.replace(/\/$/, "");
    const token = root.querySelector('input[name="__RequestVerificationToken"]')?.value ?? "";
    const tableBody = byId("resourcesTableBody");
    const resourceEditorModal = new bootstrap.Modal(byId("resourceEditorModal"));
    const resourceDeleteModal = new bootstrap.Modal(byId("resourceDeleteModal"));
    const resourceFeesModal = new bootstrap.Modal(byId("resourceFeesModal"));
    const state = { page: 1, keyword: "", items: new Map() };
    const taipeiDateTimeFormatter = new Intl.DateTimeFormat("zh-TW", {
        calendar: "gregory", timeZone: "Asia/Taipei", year: "numeric", month: "2-digit",
        day: "2-digit", hour: "2-digit", minute: "2-digit", hourCycle: "h23"
    });
    let busy = false;
    let editingResource = null;
    let deletingResource = null;
    let activeResource = null;
    let editingFee = null;
    let identityTypes = [];

    async function request(url, method = "GET", body) {
        const headers = { Accept: "application/json", "X-Requested-With": "XMLHttpRequest" };
        const options = { method, credentials: "same-origin", cache: "no-store", headers };
        if (method !== "GET") headers.RequestVerificationToken = token;
        if (body !== undefined) {
            headers["Content-Type"] = "application/json";
            options.body = JSON.stringify(body);
        }

        let response;
        try {
            response = await fetch(url, options);
        } catch {
            throw new Error("連線中斷，請重新查詢確認資料狀態後再操作。");
        }
        if (response.status === 401 || response.redirected) throw new Error("登入已逾時，請重新登入後再操作。");
        if (response.status === 403) throw new Error("目前帳號沒有操作權限。");

        const isJson = (response.headers.get("content-type") || "").includes("json");
        const result = isJson ? await response.json() : null;
        if (!response.ok) {
            const validation = result?.errors ? Object.values(result.errors).flat().join(" ") : "";
            throw new Error(result?.message || validation || result?.title || "操作未完成，請重新查詢後再試。");
        }
        if (!isJson) throw new Error("無法取得資料，請重新登入後再試。");
        return result;
    }

    function setBusy(value) {
        busy = value;
        root.setAttribute("aria-busy", String(value));
        document.querySelectorAll("#resourcesSearchForm button, #addResourceButton, #resourcesTableBody button, .resources-modal button")
            .forEach(button => { button.disabled = value; });
    }

    function setFeedback(elementId, message, success = false) {
        const element = byId(elementId);
        element.textContent = message;
        element.classList.toggle("resources-modal-feedback-success", success);
        element.classList.toggle("resources-alert-error", !success);
        element.hidden = !message;
    }

    function appendCell(row, text, className = "") {
        const cell = document.createElement("td");
        if (className) cell.className = className;
        cell.textContent = text ?? "—";
        row.append(cell);
        return cell;
    }

    function parseUtcDate(value) {
        const text = value ? String(value) : "";
        if (!text) return null;
        const date = new Date(/(?:Z|[+-]\d{2}:?\d{2})$/i.test(text) ? text : `${text}Z`);
        return Number.isNaN(date.getTime()) ? null : date;
    }

    function formatDate(value) {
        const date = parseUtcDate(value);
        return date ? taipeiDateTimeFormatter.format(date) : "—";
    }

    function formatCalendarDate(value) {
        const match = /^(\d{4})-(\d{2})-(\d{2})$/.exec(value ? String(value).slice(0, 10) : "");
        return match ? `${match[1]}/${match[2]}/${match[3]}` : "—";
    }

    function renderRows(data) {
        state.items = new Map(data.items.map(resource => [resource.resourceId, resource]));
        tableBody.replaceChildren();
        if (!data.items.length) {
            const row = document.createElement("tr");
            const cell = appendCell(row, "沒有符合條件的設備／資源資料。", "resources-empty");
            cell.colSpan = 7;
            tableBody.append(row);
            return;
        }

        data.items.forEach((resource, index) => {
            const row = document.createElement("tr");
            appendCell(row, String(((data.pageNumber - 1) * data.pageSize) + index + 1), "resources-number-column");
            appendCell(row, resource.resourceName, "resources-name");
            appendCell(row, String(resource.maxUsers), "resources-capacity");
            appendCell(row, resource.description || "—", "resources-description");
            appendCell(row, resource.createdBy || "—");
            appendCell(row, formatDate(resource.createdAt), "resources-date");

            const actions = document.createElement("td");
            const group = document.createElement("div");
            group.className = "resources-row-actions";
            const fees = document.createElement("button");
            fees.type = "button";
            fees.className = "resources-button resources-button-fees";
            fees.innerHTML = '<i class="bi bi-cash-coin" aria-hidden="true"></i>身分費率';
            fees.addEventListener("click", () => openFees(resource));
            const edit = document.createElement("button");
            edit.type = "button";
            edit.className = "resources-button resources-button-edit";
            edit.innerHTML = '<i class="bi bi-pencil-square" aria-hidden="true"></i>編輯';
            edit.addEventListener("click", () => openResourceEditor(resource));
            const remove = document.createElement("button");
            remove.type = "button";
            remove.className = "resources-button resources-button-delete";
            remove.innerHTML = '<i class="bi bi-trash3" aria-hidden="true"></i>刪除';
            remove.addEventListener("click", () => openResourceDelete(resource));
            group.append(fees, edit, remove);
            actions.append(group);
            row.append(actions);
            tableBody.append(row);
        });
    }

    function renderPagination(data) {
        byId("resourcesResultCount").textContent = `共 ${data.totalCount} 筆`;
        const nav = byId("resourcesPagination");
        nav.replaceChildren();
        if (data.totalPages <= 1) return;
        const list = document.createElement("ul");
        list.className = "pagination resources-pagination mb-0";
        const first = Math.max(1, data.pageNumber - 2);
        const last = Math.min(data.totalPages, data.pageNumber + 2);
        const addPage = (label, page, disabled = false, active = false, ariaLabel = "") => {
            const item = document.createElement("li");
            item.className = `page-item${disabled ? " disabled" : ""}${active ? " active" : ""}`;
            const button = document.createElement("button");
            button.type = "button";
            button.className = "page-link";
            button.textContent = label;
            button.disabled = disabled;
            if (ariaLabel) button.setAttribute("aria-label", ariaLabel);
            if (active) button.setAttribute("aria-current", "page");
            button.addEventListener("click", () => loadPage(page));
            item.append(button);
            list.append(item);
        };
        addPage("‹", data.pageNumber - 1, data.pageNumber <= 1, false, "上一頁");
        if (first > 1) addPage("1", 1);
        if (first > 2) {
            const dots = document.createElement("li");
            dots.className = "page-item disabled";
            dots.innerHTML = '<span class="page-link">…</span>';
            list.append(dots);
        }
        for (let page = first; page <= last; page++) addPage(String(page), page, false, page === data.pageNumber);
        if (last < data.totalPages - 1) {
            const dots = document.createElement("li");
            dots.className = "page-item disabled";
            dots.innerHTML = '<span class="page-link">…</span>';
            list.append(dots);
        }
        if (last < data.totalPages) addPage(String(data.totalPages), data.totalPages);
        addPage("›", data.pageNumber + 1, data.pageNumber >= data.totalPages, false, "下一頁");
        nav.append(list);
    }

    async function loadPage(page = 1) {
        state.page = page;
        const url = new URL(apiUrl, window.location.origin);
        url.searchParams.set("page", String(page));
        if (state.keyword) url.searchParams.set("keyword", state.keyword);
        setBusy(true);
        setFeedback("resourcesFeedback", "");
        tableBody.innerHTML = '<tr><td colspan="7" class="resources-empty">載入設備／資源資料中…</td></tr>';
        try {
            const data = await request(url.toString());
            if (!data || !Array.isArray(data.items)) throw new Error("設備／資源清單格式不正確，請重新查詢。");
            state.page = data.pageNumber;
            renderRows(data);
            renderPagination(data);
        } catch (error) {
            tableBody.innerHTML = '<tr><td colspan="7" class="resources-empty">無法載入資料。</td></tr>';
            setFeedback("resourcesFeedback", error.message);
        } finally {
            setBusy(false);
        }
    }

    function openResourceEditor(resource = null) {
        if (busy) return;
        editingResource = resource;
        byId("resourceEditorTitle").textContent = resource ? "編輯設備／資源" : "新增設備／資源";
        byId("saveResourceButton").textContent = resource ? "儲存變更" : "新增設備／資源";
        byId("resourceEditorName").value = resource?.resourceName ?? "";
        byId("resourceEditorMaxUsers").value = resource?.maxUsers ?? 1;
        byId("resourceEditorDescription").value = resource?.description ?? "";
        byId("resourceEditorError").hidden = true;
        resourceEditorModal.show();
        window.setTimeout(() => byId("resourceEditorName").focus(), 150);
    }

    function openResourceDelete(resource) {
        if (busy) return;
        deletingResource = resource;
        byId("resourceDeleteName").textContent = resource.resourceName;
        byId("resourceDeleteError").hidden = true;
        resourceDeleteModal.show();
    }

    async function loadIdentityTypes() {
        const options = await request(identityApiUrl);
        if (!Array.isArray(options)) throw new Error("身分別清單格式不正確，請先確認身分別設定。");
        identityTypes = options;
        const select = byId("resourceFeeIdentity");
        select.replaceChildren(new Option("請選擇身分別", ""));
        identityTypes.forEach(item => select.add(new Option(item.identityName, item.identityTypeId)));
        select.disabled = identityTypes.length === 0;
        byId("addResourceFeeButton").disabled = identityTypes.length === 0;
        if (!identityTypes.length) setFeedback("resourceFeesFeedback", "目前沒有身分別，請先到人員名單的「管理身分別」新增身分別。");
    }

    function todayTaipeiDate() {
        const parts = Object.fromEntries(new Intl.DateTimeFormat("en-CA", {
            timeZone: "Asia/Taipei", year: "numeric", month: "2-digit", day: "2-digit"
        }).formatToParts(new Date()).map(part => [part.type, part.value]));
        return `${parts.year}-${parts.month}-${parts.day}`;
    }

    function renderFees(items) {
        const body = byId("resourceFeesTableBody");
        body.replaceChildren();
        if (!items.length) {
            const row = document.createElement("tr");
            const cell = appendCell(row, "此設備／資源尚未設定身分費率。", "resources-empty");
            cell.colSpan = 7;
            body.append(row);
            return;
        }
        items.forEach(fee => {
            const row = document.createElement("tr");
            appendCell(row, `第 ${fee.resourceConfigNo} 版`, "resources-date");
            appendCell(row, fee.identityName, "resources-name");
            appendCell(row, `${new Intl.NumberFormat("zh-TW", { minimumFractionDigits: 2, maximumFractionDigits: 2 }).format(fee.fee)} 元`, "resources-fee-amount");
            appendCell(row, formatCalendarDate(fee.effectiveStart), "resources-date");
            appendCell(row, formatCalendarDate(fee.effectiveEnd), "resources-date");
            appendCell(row, fee.createdBy || "—");
            const actions = document.createElement("td");
            const group = document.createElement("div");
            group.className = "resources-row-actions";
            const edit = document.createElement("button");
            edit.type = "button";
            edit.className = "resources-button resources-button-edit";
            edit.innerHTML = '<i class="bi bi-pencil-square" aria-hidden="true"></i>編輯';
            edit.addEventListener("click", () => openFeeEditor(fee));
            const remove = document.createElement("button");
            remove.type = "button";
            remove.className = "resources-button resources-button-delete";
            remove.innerHTML = '<i class="bi bi-trash3" aria-hidden="true"></i>刪除';
            remove.addEventListener("click", () => deleteFee(fee));
            group.append(edit, remove);
            actions.append(group);
            row.append(actions);
            body.append(row);
        });
    }

    async function loadFees() {
        if (!activeResource) return;
        setBusy(true);
        try {
            const items = await request(`${apiUrl}/${encodeURIComponent(activeResource.resourceId)}/fees`);
            renderFees(items);
        } catch (error) {
            setFeedback("resourceFeesFeedback", error.message);
            byId("resourceFeesTableBody").innerHTML = '<tr><td colspan="7" class="resources-empty">無法載入身分費率。</td></tr>';
        } finally {
            setBusy(false);
            byId("addResourceFeeButton").disabled = identityTypes.length === 0;
        }
    }

    async function openFees(resource) {
        if (busy) return;
        activeResource = resource;
        byId("resourceFeesTitle").textContent = "管理身分費率";
        byId("resourceFeesResourceName").textContent = resource.resourceName;
        byId("resourceFeesListPanel").hidden = false;
        byId("resourceFeeEditorForm").hidden = true;
        setFeedback("resourceFeesFeedback", "");
        byId("resourceFeesTableBody").innerHTML = '<tr><td colspan="7" class="resources-empty">載入中…</td></tr>';
        resourceFeesModal.show();
        try {
            await loadIdentityTypes();
            await loadFees();
        } catch (error) {
            setFeedback("resourceFeesFeedback", error.message);
        }
    }

    function showFeeList() {
        byId("resourceFeesListPanel").hidden = false;
        byId("resourceFeeEditorForm").hidden = true;
        editingFee = null;
    }

    function openFeeEditor(fee = null) {
        if (busy || !identityTypes.length) return;
        editingFee = fee;
        byId("resourceFeesListPanel").hidden = true;
        byId("resourceFeeEditorForm").hidden = false;
        byId("resourceFeeEditorTitle").textContent = fee ? "編輯身分費率" : "新增身分費率";
        byId("saveResourceFeeButton").textContent = fee ? "儲存變更" : "新增費率";
        byId("resourceFeeIdentity").value = fee?.identityTypeId ?? "";
        byId("resourceFeeAmount").value = fee?.fee ?? "";
        byId("resourceFeeStart").value = fee ? String(fee.effectiveStart).slice(0, 10) : todayTaipeiDate();
        byId("resourceFeeEnd").value = fee ? String(fee.effectiveEnd).slice(0, 10) : todayTaipeiDate();
        byId("resourceFeeEditorError").hidden = true;
        window.setTimeout(() => byId("resourceFeeIdentity").focus(), 50);
    }

    async function deleteFee(fee) {
        if (!activeResource || !window.confirm(`確定刪除「${fee.identityName}」的這筆身分費率嗎？`)) return;
        setBusy(true);
        try {
            const result = await request(`${apiUrl}/${encodeURIComponent(activeResource.resourceId)}/fees/${encodeURIComponent(fee.resourceFeeId)}`, "DELETE");
            setFeedback("resourceFeesFeedback", result.message || "身分費率已刪除。", true);
            await loadFees();
        } catch (error) {
            setFeedback("resourceFeesFeedback", error.message);
        } finally {
            setBusy(false);
        }
    }

    byId("resourcesSearchForm").addEventListener("submit", event => {
        event.preventDefault();
        state.keyword = byId("resourcesKeyword").value.trim();
        loadPage(1);
    });

    byId("clearResourcesSearchButton").addEventListener("click", () => {
        byId("resourcesKeyword").value = "";
        state.keyword = "";
        loadPage(1);
    });

    byId("addResourceButton").addEventListener("click", () => openResourceEditor());

    byId("resourceEditorForm").addEventListener("submit", async event => {
        event.preventDefault();
        const form = event.currentTarget;
        if (busy || !form.reportValidity()) return;
        const payload = {
            resourceName: byId("resourceEditorName").value.trim(),
            maxUsers: Number(byId("resourceEditorMaxUsers").value),
            description: byId("resourceEditorDescription").value.trim() || null
        };
        const method = editingResource ? "PUT" : "POST";
        const url = editingResource ? `${apiUrl}/${encodeURIComponent(editingResource.resourceId)}` : apiUrl;
        setBusy(true);
        try {
            const result = await request(url, method, payload);
            resourceEditorModal.hide();
            setFeedback("resourcesFeedback", result.message || (editingResource ? "設備／資源資料已更新。" : "設備／資源已新增。"), true);
            await loadPage(editingResource ? state.page : 1);
        } catch (error) {
            const message = byId("resourceEditorError");
            message.textContent = error.message;
            message.hidden = false;
        } finally {
            setBusy(false);
        }
    });

    byId("confirmDeleteResourceButton").addEventListener("click", async () => {
        if (busy || !deletingResource) return;
        setBusy(true);
        try {
            const result = await request(`${apiUrl}/${encodeURIComponent(deletingResource.resourceId)}`, "DELETE");
            resourceDeleteModal.hide();
            setFeedback("resourcesFeedback", result.message || "設備／資源已刪除。", true);
            await loadPage(state.page);
        } catch (error) {
            const message = byId("resourceDeleteError");
            message.textContent = error.message;
            message.hidden = false;
        } finally {
            setBusy(false);
        }
    });

    byId("addResourceFeeButton").addEventListener("click", () => openFeeEditor());
    byId("cancelResourceFeeEditButton").addEventListener("click", showFeeList);

    byId("resourceFeeEditorForm").addEventListener("submit", async event => {
        event.preventDefault();
        const form = event.currentTarget;
        if (busy || !form.reportValidity() || !activeResource) return;
        const wasEditing = editingFee !== null;
        const payload = {
            identityTypeId: byId("resourceFeeIdentity").value,
            fee: Number(byId("resourceFeeAmount").value),
            effectiveStart: byId("resourceFeeStart").value,
            effectiveEnd: byId("resourceFeeEnd").value
        };
        const method = editingFee ? "PUT" : "POST";
        const url = editingFee
            ? `${apiUrl}/${encodeURIComponent(activeResource.resourceId)}/fees/${encodeURIComponent(editingFee.resourceFeeId)}`
            : `${apiUrl}/${encodeURIComponent(activeResource.resourceId)}/fees`;
        setBusy(true);
        try {
            const result = await request(url, method, payload);
            showFeeList();
            setFeedback("resourceFeesFeedback", result.message || (wasEditing ? "身分費率已更新。" : "身分費率已新增。"), true);
            await loadFees();
        } catch (error) {
            const message = byId("resourceFeeEditorError");
            message.textContent = error.message;
            message.hidden = false;
        } finally {
            setBusy(false);
        }
    });

    loadPage();
});
