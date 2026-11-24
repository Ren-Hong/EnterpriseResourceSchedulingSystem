document.addEventListener("DOMContentLoaded", () => {
    const root = document.getElementById("frontendScheduler");
    const calendarElement = document.getElementById("scheduleCalendar");
    if (!root || !calendarElement) return;

    const byId = id => document.getElementById(id);
    const tabs = [...document.querySelectorAll("[data-scheduler-tab]")];
    const panels = tabs.map(tab => byId(tab.dataset.schedulerTab));
    const groups = {
        resources: {
            list: byId("resourceFilterList"), search: byId("resourceSearch"), count: byId("resourceFilterCount"),
            countUnit: "項", idKey: "resourceId", nameKey: "resourceName",
            meta: item => `可使用 ${item.maxUsers} 人`, searchText: item => item.resourceName,
            rows: [], selected: new Set()
        },
        people: {
            list: byId("peopleFilterList"), search: byId("peopleSearch"), count: byId("peopleFilterCount"),
            countUnit: "位", idKey: "userId", nameKey: "displayName",
            meta: item => item.identityTypeName || "未設定身分別",
            searchText: item => `${item.displayName} ${item.identityTypeName ?? ""}`,
            rows: [], selected: new Set()
        }
    };

    function emitSelection() {
        root.dispatchEvent(new CustomEvent("schedule-filter-change", {
            detail: {
                resourceIds: [...groups.resources.selected],
                userIds: [...groups.people.selected]
            }
        }));
    }

    function updateCount(group) {
        group.count.textContent = `${group.selected.size} / ${group.rows.length} ${group.countUnit}`;
    }

    function applySearch(group) {
        const query = group.search.value.trim().toLocaleLowerCase();
        let visibleCount = 0;
        group.list.querySelectorAll(".scheduler-option-item").forEach(item => {
            item.hidden = !item.dataset.searchText.includes(query);
            if (!item.hidden) visibleCount++;
        });
        group.list.querySelector(".scheduler-option-search-empty")?.remove();
        if (query && visibleCount === 0 && group.rows.length > 0) {
            const empty = document.createElement("div");
            empty.className = "scheduler-option-list-state scheduler-option-search-empty";
            empty.textContent = "沒有符合的項目";
            group.list.append(empty);
        }
    }

    function renderGroup(group) {
        group.list.replaceChildren();
        if (!group.rows.length) {
            const empty = document.createElement("div");
            empty.className = "scheduler-option-list-state";
            empty.textContent = "無資料";
            group.list.append(empty);
            updateCount(group);
            return;
        }

        group.rows.forEach(row => {
            const id = row[group.idKey];
            const button = document.createElement("button");
            button.type = "button";
            button.className = "scheduler-option-item";
            button.dataset.searchText = group.searchText(row).toLocaleLowerCase();
            button.setAttribute("role", "checkbox");
            button.setAttribute("aria-checked", String(group.selected.has(id)));

            const check = document.createElement("span");
            check.className = "scheduler-option-check";
            check.innerHTML = '<i class="bi bi-check-lg" aria-hidden="true"></i>';
            const copy = document.createElement("span");
            copy.className = "scheduler-option-copy";
            const name = document.createElement("span");
            name.className = "scheduler-option-name";
            name.textContent = row[group.nameKey] || "未設定姓名";
            const meta = document.createElement("span");
            meta.className = "scheduler-option-meta";
            meta.textContent = group.meta(row);
            copy.append(name, meta);
            button.append(check, copy);

            button.addEventListener("click", () => {
                if (group.selected.has(id)) group.selected.delete(id);
                else group.selected.add(id);
                button.classList.toggle("is-selected", group.selected.has(id));
                button.setAttribute("aria-checked", String(group.selected.has(id)));
                updateCount(group);
                emitSelection();
            });
            button.classList.toggle("is-selected", group.selected.has(id));
            group.list.append(button);
        });

        applySearch(group);
        updateCount(group);
    }

    async function loadFilters() {
        Object.values(groups).forEach(group => {
            group.list.setAttribute("aria-busy", "true");
            group.list.replaceChildren();
            const state = document.createElement("div");
            state.className = "scheduler-option-list-state";
            state.textContent = "載入中…";
            group.list.append(state);
        });

        try {
            const response = await fetch(`${root.dataset.scheduleUrl}/options`, {
                credentials: "same-origin",
                cache: "no-store",
                headers: { Accept: "application/json", "X-Requested-With": "XMLHttpRequest" }
            });
            if (response.status === 401 || response.redirected) throw new Error("登入已逾時，請重新登入。");
            if (response.status === 403) throw new Error("目前帳號沒有查看排程條件的權限。");
            if (!response.ok) throw new Error("條件清單載入失敗。");

            const options = await response.json();
            groups.resources.rows = Array.isArray(options.resources) ? options.resources : [];
            groups.people.rows = Array.isArray(options.users) ? options.users : [];
            Object.values(groups).forEach(group => {
                group.selected = new Set(group.rows.map(item => item[group.idKey]));
                renderGroup(group);
                group.list.setAttribute("aria-busy", "false");
            });
            emitSelection();
        } catch (error) {
            Object.values(groups).forEach(group => {
                group.list.setAttribute("aria-busy", "false");
                const state = document.createElement("div");
                state.className = "scheduler-option-list-state scheduler-option-list-error";
                state.textContent = error instanceof TypeError ? "連線失敗" : error.message;
                group.list.replaceChildren(state);
            });
        }
    }

    Object.values(groups).forEach(group => group.search.addEventListener("input", () => applySearch(group)));
    document.querySelectorAll("[data-select-group]").forEach(button => {
        button.addEventListener("click", () => {
            const group = groups[button.dataset.selectGroup];
            group.selected = new Set(button.dataset.selectAction === "all" ? group.rows.map(item => item[group.idKey]) : []);
            renderGroup(group);
            emitSelection();
        });
    });

    function activateTab(tab, focus = false) {
        tabs.forEach((item, index) => {
            const active = item === tab;
            item.classList.toggle("is-active", active);
            item.setAttribute("aria-selected", String(active));
            item.tabIndex = active ? 0 : -1;
            panels[index].hidden = !active;
        });
        if (focus) tab.focus();
    }

    tabs.forEach((tab, index) => {
        tab.addEventListener("click", () => activateTab(tab));
        tab.addEventListener("keydown", event => {
            let nextIndex = index;
            if (event.key === "ArrowRight") nextIndex = (index + 1) % tabs.length;
            else if (event.key === "ArrowLeft") nextIndex = (index - 1 + tabs.length) % tabs.length;
            else if (event.key === "Home") nextIndex = 0;
            else if (event.key === "End") nextIndex = tabs.length - 1;
            else return;
            event.preventDefault();
            activateTab(tabs[nextIndex], true);
        });
    });

    if (!window.FullCalendar?.Calendar) {
        const message = byId("calendarLoadMessage");
        message.textContent = "日曆元件載入失敗，請重新整理頁面。";
        message.hidden = false;
        loadFilters();
        return;
    }

    const calendarMessage = byId("calendarLoadMessage");
    const modalElement = byId("scheduleSlotModal");
    const slotModal = modalElement ? new bootstrap.Modal(modalElement) : null;
    const form = byId("scheduleSlotForm");
    const cancelButton = byId("cancelScheduleSlot");
    const dateInput = byId("slotDate");
    const resourceInput = byId("slotResource");
    const versionInput = byId("slotVersion");
    const feedback = byId("slotFeedback");
    const saveButton = byId("saveScheduleSlot");
    const slotDetail = byId("scheduleSlotDetail");
    const slotFields = byId("scheduleSlotFields");
    const slotTemplates = byId("slotTemplates");
    const dropResource = byId("slotDropResource");
    const dropFeedback = byId("slotDropFeedback");
    const slotCreateTab = byId("slotCreateTab");
    const slotFeedback = byId("scheduleSlotFeedback");
    const todayTaipei = () => new Intl.DateTimeFormat("sv-SE", { timeZone: "Asia/Taipei" }).format(new Date());
    let selectedDate = todayTaipei();
    let selectedSlot = null;
    let versionRequest = 0;
    let slotModalOpen = false;
    let creatingDroppedSlot = false;
    let resourceInfoRequest = 0;
    let bookingsRequest = 0;

    async function scheduleRequest(path, options = {}) {
        const headers = { Accept: "application/json", "X-Requested-With": "XMLHttpRequest" };
        if (options.method === "POST") {
            headers.RequestVerificationToken = form.elements.__RequestVerificationToken.value;
            headers["Content-Type"] = "application/json";
        }
        const response = await fetch(root.dataset.scheduleUrl + path, {
            ...options, headers, credentials: "same-origin", cache: "no-store"
        });
        if (response.redirected || response.status === 401) throw new Error("登入已逾時，請重新登入。");
        if (response.status === 403) throw new Error("目前帳號沒有操作權限。");
        if (!response.ok) {
            const error = await response.json().catch(() => ({}));
            throw new Error(error.message || error.title || "排程操作失敗。");
        }
        return response.status === 204 ? null : response.json();
    }

    function getVersions(resourceId, date) {
        return scheduleRequest(`/resources/${resourceId}/versions?date=${encodeURIComponent(date)}`);
    }

    async function loadVersions() {
        const sequence = ++versionRequest;
        versionInput.replaceChildren(new Option("載入中…", ""));
        saveButton.disabled = true;
        feedback.textContent = "";
        if (!resourceInput.value || !dateInput.value) {
            versionInput.replaceChildren(new Option("請先選擇設備和日期", ""));
            return;
        }
        try {
            const versions = await getVersions(resourceInput.value, dateInput.value);
            if (sequence !== versionRequest) return;
            const placeholder = versions.length ? "請選擇版本" : "該日期沒有適用版本";
            versionInput.replaceChildren(new Option(placeholder, ""));
            for (const version of versions) {
                const label = `第 ${version.version} 版｜${version.effectiveStart.slice(0, 10)} ～ ${version.effectiveEnd.slice(0, 10)}｜上限 ${version.maxUsers} 人`;
                const option = new Option(label, version.resourceConfigId);
                option.dataset.capacity = version.maxUsers;
                versionInput.add(option);
            }
            if (versions.length === 1) versionInput.value = versions[0].resourceConfigId;
            applyVersionCapacity();
        } catch (error) {
            if (sequence !== versionRequest) return;
            versionInput.replaceChildren(new Option("版本載入失敗", ""));
            feedback.textContent = error.message;
        }
    }

    function applyVersionCapacity() {
        const capacity = byId("slotCapacity");
        capacity.max = versionInput.selectedOptions[0]?.dataset.capacity || "";
        capacity.value = capacity.max;
        saveButton.disabled = !versionInput.value;
    }

    function openSlotModal(date = selectedDate, start = "09:00", end = "10:00", resourceId = dropResource?.value) {
        if (slotModalOpen) return;
        form.reset();
        resourceInput.replaceChildren(new Option("請選擇設備", ""));
        for (const resource of groups.resources.rows) {
            resourceInput.add(new Option(resource.resourceName, resource.resourceId));
        }
        if (resourceId) resourceInput.value = resourceId;
        else if (groups.resources.selected.size === 1) resourceInput.value = [...groups.resources.selected][0];
        dateInput.value = date;
        byId("slotStart").value = start;
        byId("slotEnd").value = end;
        byId("slotCapacity").value = "";
        slotModal.show();
        loadVersions();
    }

    modalElement?.addEventListener("show.bs.modal", () => { slotModalOpen = true; });
    modalElement?.addEventListener("hidden.bs.modal", () => {
        slotModalOpen = false;
        versionRequest++;
        slotCreateTab.focus();
    });
    resourceInput?.addEventListener("change", loadVersions);
    dateInput?.addEventListener("change", loadVersions);
    versionInput?.addEventListener("change", applyVersionCapacity);
    form?.addEventListener("submit", async event => {
        event.preventDefault();
        saveButton.disabled = true;
        feedback.textContent = "";
        try {
            await scheduleRequest("/slots", {
                method: "POST",
                body: JSON.stringify({
                    resourceId: resourceInput.value, resourceConfigId: versionInput.value, date: dateInput.value,
                    start: byId("slotStart").value + ":00", end: byId("slotEnd").value + ":00",
                    capacity: Number(byId("slotCapacity").value), note: byId("slotNote").value
                })
            });
            calendar.gotoDate(dateInput.value);
            calendar.refetchEvents();
            slotModal.hide();
        } catch (error) {
            feedback.textContent = error.message;
        } finally {
            saveButton.disabled = !versionInput.value;
        }
    });

    cancelButton?.addEventListener("click", async () => {
        if (!selectedSlot || !window.confirm("確定取消此時段及其中的預約？")) return;
        const slotId = selectedSlot.slotId;
        cancelButton.disabled = true;
        slotFeedback.textContent = "";
        try {
            await scheduleRequest(`/slots/${slotId}/cancel`, { method: "POST" });
            if (selectedSlot?.slotId === slotId) {
                clearSlot("時段已取消。");
            }
            calendar.refetchEvents();
        } catch (error) {
            slotFeedback.textContent = error.message;
        } finally {
            cancelButton.disabled = false;
        }
    });

    const bookingPanel = byId("bookScheduleSlot");
    const bookingUser = byId("scheduleBookingUser");
    const bookingButton = byId("saveScheduleBooking");
    const capacityForm = byId("increaseSlotCapacityForm");
    const capacityInput = byId("increaseSlotCapacity");
    bookingButton?.addEventListener("click", async () => {
        if (!selectedSlot || !bookingUser.value) return;
        const slot = selectedSlot;
        const userId = bookingUser.value;
        bookingButton.disabled = true;
        slotFeedback.textContent = "";
        try {
            await scheduleRequest(`/slots/${slot.slotId}/bookings/${userId}`, { method: "POST" });
            if (selectedSlot?.slotId === slot.slotId) {
                showSlot({ ...selectedSlot, booked: selectedSlot.booked + 1 });
                slotFeedback.textContent = "已完成預約。";
            }
            calendar.refetchEvents();
        } catch (error) {
            slotFeedback.textContent = error.message;
        } finally {
            bookingButton.disabled = false;
        }
    });
    capacityForm?.addEventListener("submit", async event => {
        event.preventDefault();
        if (!selectedSlot) return;
        const slot = selectedSlot;
        const capacity = capacityInput.valueAsNumber;
        const button = event.submitter;
        if (button) button.disabled = true;
        slotFeedback.textContent = "";
        try {
            await scheduleRequest(`/slots/${slot.slotId}/capacity/${capacity}`, { method: "POST" });
            if (selectedSlot?.slotId === slot.slotId) {
                showSlot({ ...selectedSlot, capacity });
                slotFeedback.textContent = "已增加時段名額。";
            }
            calendar.refetchEvents();
        } catch (error) {
            if (selectedSlot?.slotId === slot.slotId) slotFeedback.textContent = error.message;
        } finally {
            if (button) button.disabled = false;
        }
    });

    const slotLabels = { open: "空白時間", booked: "已預約", completed: "已完成", cancelled: "已取消" };

    function slotDisplayStatus(slot) {
        if (slot.status === "CANCELLED") return "cancelled";
        if (slot.status === "COMPLETED") return "completed";
        return slot.booked > 0 ? "booked" : "open";
    }
    function renderResourceFields(empty, fields, values) {
        empty.hidden = values.length > 0;
        fields.hidden = values.length === 0;
        fields.replaceChildren(...values.map(([name, value, type = "text"], index) => {
            const row = document.createElement("div");
            row.className = "scheduler-panel-row";
            const label = document.createElement("label");
            const input = document.createElement("input");
            input.id = `resourceDetailField${index}`;
            input.className = "form-control";
            input.type = type;
            input.readOnly = true;
            input.value = value;
            label.htmlFor = input.id;
            label.textContent = name;
            row.append(label, input);
            return row;
        }));
    }

    async function loadResourceFees(resourceId, resourceConfigId, request) {
        byId("scheduleResourceFees").hidden = false;
        const status = byId("scheduleResourceFeesStatus");
        const table = byId("scheduleResourceFeesTable");
        const body = table.tBodies[0];
        table.hidden = true;
        body.replaceChildren();
        status.hidden = false;
        status.textContent = "載入身分費率…";
        try {
            const fees = await scheduleRequest(`/resources/${resourceId}/versions/${resourceConfigId}/fees`);
            if (request !== resourceInfoRequest) return;
            const money = new Intl.NumberFormat("zh-TW", { maximumFractionDigits: 2 });
            for (const fee of fees) {
                const row = body.insertRow();
                row.insertCell().textContent = fee.identityName;
                const amount = row.insertCell();
                amount.className = "text-end";
                amount.textContent = `${money.format(fee.fee)} 元`;
            }
            table.hidden = fees.length === 0;
            status.hidden = fees.length > 0;
            status.textContent = "此版本尚未設定身分費率。";
        } catch (error) {
            if (request !== resourceInfoRequest) return;
            status.textContent = error.message;
        }
    }

    async function refreshResourceInfo() {
        const request = ++resourceInfoRequest;
        byId("scheduleResourceFees").hidden = true;
        const empty = byId("scheduleResourceEmpty");
        const fields = byId("scheduleResourceFields");
        const slot = selectedSlot;
        const resourceId = slot?.resourceId || dropResource?.value;
        const resource = groups.resources.rows.find(row => row.resourceId === resourceId);
        const name = slot?.resourceName || resource?.resourceName;
        empty.textContent = "請先點選排程，或選擇新增時段所屬設備。";
        renderResourceFields(empty, fields, name ? [["設備名稱", name]] : []);
        if (!name) return;

        try {
            let version = slot;
            if (!version) {
                const versions = await getVersions(resourceId, selectedDate);
                if (request !== resourceInfoRequest) return;
                version = versions.length === 1 ? versions[0] : null;
            }
            if (version?.version == null) return;

            if (slot && capacityForm && version.maxUsers != null) {
                capacityInput.max = String(version.maxUsers);
                if (slot.capacity >= version.maxUsers) capacityForm.hidden = true;
            }

            renderResourceFields(empty, fields, [
                ["設備名稱", name],
                ["設備設定", `第 ${version.version} 版`],
                ["設定生效起日", version.effectiveStart?.slice(0, 10) ?? "", "date"],
                ["設定生效迄日", version.effectiveEnd?.slice(0, 10) ?? "", "date"],
                ["預設使用人數上限", version.maxUsers ?? ""]
            ]);
            if (version.resourceConfigId) {
                void loadResourceFees(resourceId, version.resourceConfigId, request);
            }
        } catch (error) {
            if (request !== resourceInfoRequest) return;
            empty.textContent = error.message;
            empty.hidden = false;
        }
    }

    const personEmpty = byId("schedulePersonEmpty");
    const personFields = byId("schedulePersonFields");
    const personFeedback = byId("schedulePersonFeedback");

    async function cancelBooking(slot, booking, button) {
        if (selectedSlot !== slot || !window.confirm(`確定取消「${booking.displayName}」的預約？`)) return;
        button.disabled = true;
        personFeedback.textContent = "";
        try {
            await scheduleRequest(`/slots/${slot.slotId}/bookings/${booking.bookingId}/cancel`, { method: "POST" });
            if (selectedSlot?.slotId === slot.slotId) {
                showSlot({ ...selectedSlot, booked: Math.max(0, selectedSlot.booked - 1) }, false);
                personFeedback.textContent = "已取消預約。";
            }
            calendar.refetchEvents();
        } catch (error) {
            if (selectedSlot?.slotId === slot.slotId) personFeedback.textContent = error.message;
        } finally {
            button.disabled = false;
        }
    }

    async function loadBookingChanges(slotId, request) {
        const history = byId("scheduleBookingChanges");
        if (!history) return;

        const status = byId("scheduleBookingChangesStatus");
        const list = byId("scheduleBookingChangesList");
        history.hidden = false;
        status.hidden = false;
        status.textContent = "載入異動紀錄…";
        list.replaceChildren();

        try {
            const changes = await scheduleRequest(`/slots/${slotId}/booking-changes`);
            if (request !== bookingsRequest) return;
            status.hidden = changes.length > 0;
            status.textContent = "此時段尚無異動紀錄。";
            for (const change of changes) {
                const item = document.createElement("li");
                item.className = "scheduler-booking-card";
                const copy = document.createElement("div");
                copy.className = "scheduler-option-copy";
                const name = document.createElement("span");
                name.className = "scheduler-option-name";
                name.textContent = `${change.displayName} · ${change.action === "CANCELLED" ? "取消預約" : "建立預約"}`;
                const detail = document.createElement("span");
                detail.className = "scheduler-option-meta";
                const time = new Date(change.occurredAt).toLocaleString("zh-TW", {
                    timeZone: "Asia/Taipei", dateStyle: "medium", timeStyle: "short"
                });
                detail.textContent = `${time} · 操作者 ${change.actor || "未記錄"}`;
                copy.append(name, detail);
                item.append(copy);
                list.append(item);
            }
        } catch (error) {
            if (request !== bookingsRequest) return;
            status.textContent = error.message;
        }
    }

    async function loadBookings(slot) {
        const request = ++bookingsRequest;
        void loadBookingChanges(slot.slotId, request);
        const output = byId("detailBookedNames");
        output.value = "載入中…";
        personEmpty.hidden = false;
        personEmpty.textContent = "載入預約人員…";
        personFields.hidden = true;
        personFields.replaceChildren();
        try {
            const bookings = await scheduleRequest(`/slots/${slot.slotId}/bookings`);
            if (request !== bookingsRequest) return;
            output.value = bookings.length ? bookings.map(booking => booking.displayName).join("\n") : "尚無預約";
            output.rows = Math.min(5, Math.max(2, bookings.length));
            personEmpty.textContent = "此時段尚無預約。";
            personEmpty.hidden = bookings.length > 0;
            personFields.hidden = bookings.length === 0;
            for (const booking of bookings) {
                const item = document.createElement("li");
                item.className = "scheduler-booking-card";
                const mark = document.createElement("span");
                mark.className = "scheduler-option-check";
                mark.textContent = "✓";
                mark.setAttribute("aria-hidden", "true");
                const person = document.createElement("span");
                person.className = "scheduler-option-copy";
                const name = document.createElement("span");
                name.className = "scheduler-option-name";
                name.textContent = booking.displayName;
                person.append(name);
                if (booking.identityTypeName) {
                    const identity = document.createElement("span");
                    identity.className = "scheduler-option-meta";
                    identity.textContent = booking.identityTypeName;
                    person.append(identity);
                }
                item.append(mark, person);
                if (cancelButton && ["OPEN", "CLOSED"].includes(slot.status)) {
                    const button = document.createElement("button");
                    button.type = "button";
                    button.className = "btn btn-outline-danger btn-sm";
                    button.textContent = "取消預約";
                    button.setAttribute("aria-label", `取消 ${booking.displayName} 的預約`);
                    button.addEventListener("click", () => cancelBooking(slot, booking, button));
                    item.append(button);
                }
                personFields.append(item);
            }
        } catch (error) {
            if (request !== bookingsRequest) return;
            output.value = error.message;
            personEmpty.textContent = error.message;
        }
    }
    function clearSlot(message = "請點選日曆上的時間格。") {
        selectedSlot = null;
        bookingsRequest++;
        const history = byId("scheduleBookingChanges");
        if (history) history.hidden = true;
        slotDetail.textContent = message;
        slotDetail.hidden = false;
        slotFields.hidden = true;
        slotFeedback.textContent = "";
        personFields.hidden = true;
        personFields.replaceChildren();
        personEmpty.hidden = false;
        personEmpty.textContent = "請先點選日曆上的時間格。";
        personFeedback.textContent = "";
        refreshResourceInfo();
    }

    async function loadBookingUsers(slot) {
        bookingUser.disabled = true;
        bookingUser.replaceChildren(new Option("載入可預約人員…", ""));
        try {
            const users = await scheduleRequest(`/slots/${slot.slotId}/booking-users`);
            if (selectedSlot !== slot) return;
            bookingUser.replaceChildren(new Option(users.length ? "請選擇人員" : "沒有符合設備權限的人員", ""));
            for (const user of users) bookingUser.add(new Option(user.displayName, user.userId));
            bookingUser.disabled = users.length === 0;
        } catch (error) {
            if (selectedSlot !== slot) return;
            bookingUser.replaceChildren(new Option("人員載入失敗", ""));
            slotFeedback.textContent = error.message;
        }
    }

    function showSlot(slot, switchTab = true) {
        selectedSlot = slot;
        selectedDate = slot.start.slice(0, 10);
        byId("detailSlotId").value = slot.slotId;
        byId("detailResource").value = slot.resourceName;
        byId("detailStatus").value = slotLabels[slotDisplayStatus(slot)];
        byId("detailDate").value = selectedDate;
        byId("detailStart").value = slot.start.slice(11, 16);
        byId("detailEnd").value = slot.end.slice(11, 16);
        byId("detailNote").value = slot.note || "";
        byId("detailCapacity").value = `已預約 ${slot.booked}／${slot.capacity} 人`;
        slotDetail.hidden = true;
        slotFields.hidden = false;
        slotFeedback.textContent = "";
        if (cancelButton) cancelButton.hidden = !["OPEN", "CLOSED"].includes(slot.status);
        if (bookingPanel) {
            const canChange = slot.status === "OPEN" && new Date(slot.start + "+08:00") > new Date();
            const canBook = canChange && slot.booked < slot.capacity;
            capacityForm.hidden = !canChange;
            capacityInput.removeAttribute("max");
            capacityInput.min = String(slot.capacity + 1);
            capacityInput.value = capacityInput.min;
            bookingPanel.hidden = !canBook;
            bookingButton.hidden = !canBook;
            if (canBook) loadBookingUsers(slot);
        }
        personFeedback.textContent = "";
        loadBookings(slot);
        refreshResourceInfo();
        if (switchTab) activateTab(byId("scheduleInfoTab"));
    }

    dropResource?.addEventListener("change", () => clearSlot());
    function syncDropResources() {
        if (!dropResource) return;
        const previous = dropResource.value;
        const resources = groups.resources.rows.filter(resource => groups.resources.selected.has(resource.resourceId));
        dropResource.replaceChildren(new Option("請選擇設備", ""));
        for (const resource of resources) {
            dropResource.add(new Option(resource.resourceName, resource.resourceId));
        }
        if (resources.length === 1) dropResource.value = resources[0].resourceId;
        else if (resources.some(resource => resource.resourceId === previous)) dropResource.value = previous;
        dropResource.disabled = resources.length === 0;
    }

    async function createDroppedSlot(info) {
        const start = info.event.start.toISOString();
        const end = info.event.end.toISOString();
        info.revert();

        const resourceId = dropResource.value;
        if (!resourceId) {
            dropFeedback.textContent = "請先選擇新增時段的設備。";
            dropResource.focus();
            return;
        }

        const date = start.slice(0, 10);
        const startTime = start.slice(11, 16);
        const endTime = end.slice(11, 16);
        creatingDroppedSlot = true;
        dropFeedback.textContent = "建立空白時段中…";
        try {
            const versions = await getVersions(resourceId, date);
            if (!versions.length) {
                dropFeedback.textContent = "該日期沒有適用的設備版本。";
                return;
            }
            if (versions.length > 1) {
                dropFeedback.textContent = "該日期有多個適用版本，請選擇後儲存。";
                openSlotModal(date, startTime, endTime, resourceId);
                return;
            }
            await scheduleRequest("/slots", {
                method: "POST",
                body: JSON.stringify({
                    resourceId, resourceConfigId: versions[0].resourceConfigId, date,
                    start: startTime + ":00", end: endTime + ":00"
                })
            });
            dropFeedback.textContent = groups.people.selected.size !== groups.people.rows.length
                ? "已建立空白時段。全選人員即可在日曆查看。"
                : "已建立空白時段。";
            calendar.refetchEvents();
        } catch (error) {
            dropFeedback.textContent = error.message;
        } finally {
            creatingDroppedSlot = false;
        }
    }
    const intervalControl = byId("calendarIntervalControl");
    const intervalSelect = byId("calendarInterval");
    let hideCancelled = false;

    function updateCalendarControls(view) {
        const toggle = calendarElement.querySelector(".fc-toggleCancelled-button");
        if (!toggle) return;
        if (toggle.previousElementSibling !== intervalControl) toggle.before(intervalControl);
        intervalControl.hidden = view.type === "dayGridMonth";
        toggle.textContent = hideCancelled ? "顯示已取消" : "隱藏已取消";
        toggle.setAttribute("aria-label", toggle.textContent);
        toggle.title = toggle.textContent;
    }
    const calendar = new FullCalendar.Calendar(calendarElement, {
        initialView: "timeGridWeek",
        locale: {
            code: "zh-tw",
            week: { dow: 1, doy: 4 },
            buttonText: { today: "今天", month: "月", week: "週", day: "日", list: "清單" },
            allDayText: "全天",
            moreLinkText: "更多",
            noEventsText: "目前沒有排程資料"
        },
        headerToolbar: { left: "toggleCancelled", center: "title", right: "prev,next today dayGridMonth,timeGridWeek,timeGridDay" },
        customButtons: {
            toggleCancelled: {
                text: "隱藏已取消",
                click() {
                    hideCancelled = !hideCancelled;
                    if (hideCancelled && selectedSlot?.status === "CANCELLED") {
                        clearSlot();
                    }
                    updateCalendarControls(calendar.view);
                    calendar.refetchEvents();
                }
            }
        },
        datesSet(info) {
            updateCalendarControls(info.view);
        },
        buttonText: { dayGridMonth: "月", timeGridWeek: "週", timeGridDay: "日" },
        firstDay: 1,
        timeZone: "UTC",
        initialDate: todayTaipei(),
        allDaySlot: false,
        slotMinTime: "07:00:00",
        slotMaxTime: "22:00:00",
        slotDuration: { minutes: 30 },
        slotLabelInterval: { minutes: 30 },
        snapDuration: { minutes: 30 },
        height: "100%",
        expandRows: true,
        selectable: false,
        droppable: Boolean(slotTemplates),
        dropAccept: ".scheduler-slot-template",
        eventAllow(info) {
            if (!slotTemplates || slotModalOpen || creatingDroppedSlot || calendar.view.type === "dayGridMonth" || info.allDay || !info.end) return false;
            return info.start.toISOString().slice(0, 10) === info.end.toISOString().slice(0, 10);
        },
        eventReceive: createDroppedSlot,
        editable: false,
        events(info, success, failure) {
            const resources = groups.resources;
            const people = groups.people;
            if (!resources.selected.size || !people.selected.size) {
                calendarMessage.hidden = true;
                success([]);
                return;
            }
            const query = new URLSearchParams({ from: info.startStr.slice(0, 10), to: info.endStr.slice(0, 10) });
            for (const id of resources.selected) query.append("resourceIds", id);
            for (const id of people.selected) query.append("userIds", id);
            if (!hideCancelled) query.set("includeCancelled", "true");
            if (people.selected.size === people.rows.length) query.set("includeUnbooked", "true");
            scheduleRequest(`/slots?${query}`).then(slots => {
                calendarMessage.hidden = true;
                success(slots.map(slot => ({
                    id: slot.slotId, title: slot.resourceName, start: slot.start, end: slot.end,
                    display: "block",
                    classNames: [`scheduler-slot-${slotDisplayStatus(slot)}`],
                    extendedProps: { slot }
                })));
            }).catch(error => {
                calendarMessage.textContent = error.message;
                calendarMessage.hidden = false;
                failure(error);
            });
        },
        eventClick(info) {
            showSlot(info.event.extendedProps.slot);
        },
        dateClick(info) {
            selectedDate = info.dateStr.slice(0, 10);
            if (slotCreateTab?.getAttribute("aria-selected") === "true") {
                if (info.view.type === "dayGridMonth") calendar.changeView("timeGridDay", selectedDate);
                clearSlot();
                return;
            }
            clearSlot();
            activateTab(byId("scheduleInfoTab"));
        }
    });

    root.addEventListener("schedule-filter-change", () => {
        syncDropResources();
        refreshResourceInfo();
        calendar.refetchEvents();
    });
    calendar.render();
    updateCalendarControls(calendar.view);
    intervalSelect.addEventListener("change", () => {
        const duration = { minutes: Number(intervalSelect.value) };
        calendar.batchRendering(() => {
            calendar.setOption("slotDuration", duration);
            calendar.setOption("slotLabelInterval", duration);
            calendar.setOption("snapDuration", duration);
        });
        updateCalendarControls(calendar.view);
    });
    if (slotTemplates) {
        new FullCalendar.Draggable(slotTemplates, {
            itemSelector: ".scheduler-slot-template",
            eventData: element => ({
                title: "空白時段",
                duration: { minutes: Number(element.dataset.minutes) },
                allDay: false
            })
        });

        byId("slotTemplateForm").addEventListener("submit", event => {
            event.preventDefault();
            const minutes = byId("slotTemplateMinutes").valueAsNumber;
            const template = byId("customSlotTemplate");
            template.dataset.minutes = String(minutes);
            template.textContent = `＋ 自訂 ${minutes} 分鐘空白時間`;
        });
    }
    window.addEventListener("resize", () => calendar.updateSize(), { passive: true });
    loadFilters();
});
