document.addEventListener("DOMContentLoaded", () => {
    const root = document.getElementById("frontendScheduler");
    const calendarElement = document.getElementById("scheduleCalendar");
    if (!root || !calendarElement) return;

    const byId = id => document.getElementById(id);
    const tabs = [...document.querySelectorAll("[data-scheduler-tab]")];
    const panels = tabs.map(tab => byId(tab.dataset.schedulerTab));
    const dateOutput = byId("selectedCalendarDate");
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
            const response = await fetch(root.dataset.filtersUrl, {
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

    byId("clearSchedulerFilters")?.addEventListener("click", () => {
        Object.values(groups).forEach(group => {
            group.search.value = "";
            group.selected.clear();
            renderGroup(group);
        });
        dateOutput.textContent = "尚未選擇";
        emitSelection();
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
        headerToolbar: { left: "prev,next today", center: "title", right: "dayGridMonth,timeGridWeek,timeGridDay" },
        buttonText: { dayGridMonth: "月", timeGridWeek: "週", timeGridDay: "日" },
        firstDay: 1,
        nowIndicator: true,
        allDaySlot: false,
        slotMinTime: "07:00:00",
        slotMaxTime: "22:00:00",
        slotDuration: "00:30:00",
        height: "100%",
        expandRows: true,
        selectable: false,
        editable: false,
        events: [],
        dateClick(info) {
            dateOutput.textContent = new Intl.DateTimeFormat("zh-TW", {
                timeZone: "Asia/Taipei", year: "numeric", month: "2-digit", day: "2-digit", weekday: "short"
            }).format(info.date);
            activateTab(byId("scheduleInfoTab"));
        }
    });

    calendar.render();
    window.addEventListener("resize", () => calendar.updateSize(), { passive: true });
    loadFilters();
});
