document.addEventListener("DOMContentLoaded", () => {
    const root = document.getElementById("usersPage");
    if (!root) return;

    const byId = id => document.getElementById(id);
    const apiUrl = root.dataset.apiUrl.replace(/\/$/, "");
    const token = root.querySelector('input[name="__RequestVerificationToken"]')?.value ?? "";
    const tableBody = byId("usersTableBody");
    const editorElement = byId("usersEditorModal");
    const deleteElement = byId("usersDeleteModal");
    const editorModal = new bootstrap.Modal(editorElement);
    const deleteModal = new bootstrap.Modal(deleteElement);
    const state = { page: 1, keyword: "", identityTypeId: "", users: new Map(), identities: [] };
    const taipeiDateTimeFormatter = new Intl.DateTimeFormat("zh-TW", {
        calendar: "gregory",
        timeZone: "Asia/Taipei",
        year: "numeric",
        month: "2-digit",
        day: "2-digit",
        hour: "2-digit",
        minute: "2-digit",
        hourCycle: "h23"
    });
    let editingUser = null;
    let deletingUser = null;
    let busy = false;

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

    function showFeedback(message, isError = false) {
        const feedback = byId("usersFeedback");
        feedback.textContent = message;
        feedback.classList.toggle("is-error", isError);
        feedback.hidden = !message;
    }

    function setBusy(value) {
        busy = value;
        root.setAttribute("aria-busy", String(value));
        document.querySelectorAll("#usersSearchForm button, #addUserButton, #usersTableBody button, .users-modal button")
            .forEach(button => { button.disabled = value; });
    }

    function fillIdentitySelects() {
        const filter = byId("usersIdentityFilter");
        const editor = byId("usersEditorIdentity");
        filter.replaceChildren(new Option("全部身分別", ""));
        editor.replaceChildren(new Option("請選擇身分別", ""));
        state.identities.forEach(identity => {
            filter.add(new Option(identity.identityName, identity.identityTypeId));
            editor.add(new Option(identity.identityName, identity.identityTypeId));
        });
        filter.value = state.identityTypeId;
        byId("addUserButton").disabled = busy || state.identities.length === 0;
    }

    async function loadIdentityTypes() {
        const identities = await request(`${apiUrl}/identity-types`);
        if (!Array.isArray(identities)) throw new Error("身分別清單格式不正確，請重新查詢。");
        state.identities = identities;
        fillIdentitySelects();
    }

    function appendCell(row, text, className = "") {
        const cell = document.createElement("td");
        if (className) cell.className = className;
        cell.textContent = text ?? "—";
        row.append(cell);
        return cell;
    }

    function renderRows(pageData) {
        state.users = new Map(pageData.items.map(user => [user.userId, user]));
        tableBody.replaceChildren();

        if (!pageData.items.length) {
            const row = document.createElement("tr");
            const cell = appendCell(row, "沒有符合條件的人員資料。", "users-empty");
            cell.colSpan = 6;
            tableBody.append(row);
            return;
        }

        pageData.items.forEach((user, index) => {
            const row = document.createElement("tr");
            appendCell(row, String(((pageData.pageNumber - 1) * pageData.pageSize) + index + 1), "users-number-column");
            appendCell(row, user.displayName || "未填姓名", "users-name");

            const identityCell = document.createElement("td");
            const identityBadge = document.createElement("span");
            identityBadge.className = `users-identity${user.identityTypeName ? "" : " users-identity-unassigned"}`;
            identityBadge.textContent = user.identityTypeName || "未設定";
            identityCell.append(identityBadge);
            row.append(identityCell);

            appendCell(row, user.createdBy || "—");
            const createdAtValue = user.createdAt ? String(user.createdAt) : "";
            const hasTimezone = /(?:Z|[+-]\d{2}:?\d{2})$/i.test(createdAtValue);
            const createdAt = createdAtValue
                ? new Date(hasTimezone ? createdAtValue : `${createdAtValue}Z`)
                : null;
            appendCell(row, createdAt && !Number.isNaN(createdAt.getTime())
                ? taipeiDateTimeFormatter.format(createdAt)
                : "—", "users-date");

            const actions = document.createElement("td");
            const actionGroup = document.createElement("div");
            actionGroup.className = "users-row-actions";
            const editButton = document.createElement("button");
            editButton.type = "button";
            editButton.className = "users-button users-button-edit";
            editButton.innerHTML = '<i class="bi bi-pencil-square" aria-hidden="true"></i>編輯';
            editButton.addEventListener("click", () => openEditor(user));
            const deleteButton = document.createElement("button");
            deleteButton.type = "button";
            deleteButton.className = "users-button users-button-delete";
            deleteButton.innerHTML = '<i class="bi bi-trash3" aria-hidden="true"></i>刪除';
            deleteButton.addEventListener("click", () => openDelete(user));
            actionGroup.append(editButton, deleteButton);
            actions.append(actionGroup);
            row.append(actions);
            tableBody.append(row);
        });
    }

    function renderPagination(data) {
        byId("usersResultCount").textContent = `共 ${data.totalCount} 筆`;
        const nav = byId("usersPagination");
        nav.replaceChildren();
        if (data.totalPages <= 1) return;

        const list = document.createElement("ul");
        list.className = "pagination users-pagination mb-0";
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

    async function loadPage(page = 1, force = false) {
        if (busy && !force) return;
        state.page = page;
        const url = new URL(apiUrl, window.location.origin);
        url.searchParams.set("page", String(page));
        if (state.keyword) url.searchParams.set("keyword", state.keyword);
        if (state.identityTypeId) url.searchParams.set("identityTypeId", state.identityTypeId);

        setBusy(true);
        showFeedback("");
        tableBody.innerHTML = '<tr><td colspan="6" class="users-empty">載入人員資料中…</td></tr>';
        try {
            const result = await request(url.toString());
            if (!result || !Array.isArray(result.items)) throw new Error("人員清單格式不正確，請重新查詢。");
            state.page = result.pageNumber;
            renderRows(result);
            renderPagination(result);
            return result;
        } catch (error) {
            tableBody.innerHTML = '<tr><td colspan="6" class="users-empty">無法載入資料。</td></tr>';
            showFeedback(error.message, true);
        } finally {
            setBusy(false);
            byId("addUserButton").disabled = state.identities.length === 0;
        }
    }

    function openEditor(user = null) {
        if (busy) return;
        editingUser = user;
        byId("usersEditorTitle").textContent = user ? "編輯人員" : "新增人員";
        byId("saveUserButton").textContent = user ? "儲存變更" : "新增人員";
        byId("usersEditorName").value = user?.displayName ?? "";
        byId("usersEditorIdentity").value = user?.identityTypeId ?? "";
        byId("usersEditorError").hidden = true;
        editorModal.show();
        window.setTimeout(() => byId("usersEditorName").focus(), 150);
    }

    function openDelete(user) {
        if (busy) return;
        deletingUser = user;
        byId("usersDeleteName").textContent = user.displayName || "未填姓名";
        byId("usersDeleteError").hidden = true;
        deleteModal.show();
    }

    byId("usersSearchForm").addEventListener("submit", event => {
        event.preventDefault();
        state.keyword = byId("usersKeyword").value.trim();
        state.identityTypeId = byId("usersIdentityFilter").value;
        loadPage(1);
    });

    byId("clearUserSearchButton").addEventListener("click", () => {
        byId("usersKeyword").value = "";
        byId("usersIdentityFilter").value = "";
        state.keyword = "";
        state.identityTypeId = "";
        loadPage(1);
    });

    byId("addUserButton").addEventListener("click", () => openEditor());

    byId("usersEditorForm").addEventListener("submit", async event => {
        event.preventDefault();
        const form = event.currentTarget;
        if (busy || !form.reportValidity()) return;
        const payload = {
            displayName: byId("usersEditorName").value.trim(),
            identityTypeId: byId("usersEditorIdentity").value
        };
        const method = editingUser ? "PUT" : "POST";
        const url = editingUser ? `${apiUrl}/${encodeURIComponent(editingUser.userId)}` : apiUrl;
        setBusy(true);
        try {
            const result = await request(url, method, payload);
            setBusy(false);
            editorModal.hide();
            showFeedback(result.message || (editingUser ? "人員資料已更新。" : "人員已新增。"));
            await loadPage(editingUser ? state.page : 1, true);
        } catch (error) {
            const message = byId("usersEditorError");
            message.textContent = error.message;
            message.hidden = false;
        } finally {
            setBusy(false);
        }
    });

    byId("confirmDeleteUserButton").addEventListener("click", async () => {
        if (busy || !deletingUser) return;
        setBusy(true);
        try {
            const result = await request(`${apiUrl}/${encodeURIComponent(deletingUser.userId)}`, "DELETE");
            setBusy(false);
            deleteModal.hide();
            showFeedback(result.message || "人員已刪除。");
            await loadPage(state.page, true);
        } catch (error) {
            const message = byId("usersDeleteError");
            message.textContent = error.message;
            message.hidden = false;
        } finally {
            setBusy(false);
        }
    });

    (async () => {
        setBusy(true);
        try {
            await loadIdentityTypes();
            await loadPage(1, true);
        } catch (error) {
            showFeedback(error.message, true);
            tableBody.innerHTML = '<tr><td colspan="6" class="users-empty">無法載入身分別資料。</td></tr>';
        } finally {
            setBusy(false);
            byId("addUserButton").disabled = state.identities.length === 0;
        }
    })();
});
