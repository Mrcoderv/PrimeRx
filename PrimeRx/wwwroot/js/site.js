// ── Global Feature Search (Ctrl+K / Cmd+K) ────────────────
(function () {
    'use strict';

    // ── Feature index ────────────────────────────────────────
    const features = [
        { name: 'Dashboard',          route: '/Dashboard/Index',        icon: 'bi-speedometer2',  category: 'Main' },
        { name: 'Billing',            route: '/Billing/Index',          icon: 'bi-receipt',       category: 'Main' },
        { name: 'Billing (Point of Sale)', route: '/Billing/Index',     icon: 'bi-receipt',       category: 'Billing' },
        { name: 'Billing History',    route: '/Billing/History',        icon: 'bi-clock-history', category: 'Billing' },
        { name: 'Edit Bill',          route: '/Billing/Edit',           icon: 'bi-pencil',        category: 'Billing' },
        { name: 'Stock View',         route: '/Inventory/Index',        icon: 'bi-list-ul',       category: 'Inventory' },
        { name: 'Batch-wise Stock',   route: '/Inventory/Batches',      icon: 'bi-layers',        category: 'Inventory' },
        { name: 'Add Medicine',       route: '/Inventory/AddMedicine',  icon: 'bi-plus-circle',   category: 'Inventory' },
        { name: 'Stock Adjustment',   route: '/Inventory/Adjust',       icon: 'bi-arrow-left-right', category: 'Inventory' },
        { name: 'Stock Exchange (Aaicho Paicho)', route: '/Inventory/StockExchange', icon: 'bi-arrow-left-right', category: 'Inventory' },
        { name: 'Transaction History',route: '/Inventory/History',      icon: 'bi-clock-history', category: 'Inventory' },
        { name: 'Expiry Alerts',      route: '/Inventory/Expiry',       icon: 'bi-exclamation-triangle', category: 'Inventory' },
        { name: 'New Purchase',       route: '/Purchase/Create',        icon: 'bi-plus-lg',       category: 'Purchase' },
        { name: 'Purchase History',   route: '/Purchase/Index',         icon: 'bi-clock-history', category: 'Purchase' },
        { name: 'Edit Purchase',      route: '/Purchase/Edit',          icon: 'bi-pencil',        category: 'Purchase' },
        { name: 'Supplier View',      route: '/Purchase/Supplier',      icon: 'bi-building',      category: 'Purchase' },
        { name: 'Return to Supplier', route: '/Purchase/Return/Create', icon: 'bi-arrow-return-left', category: 'Purchase' },
        { name: 'Returns & Credit Notes', route: '/Purchase/Return/Index', icon: 'bi-receipt-cutoff', category: 'Purchase' },
        { name: 'Due Collection',     route: '/Due/Index',              icon: 'bi-wallet2',       category: 'Due' },
        { name: 'Pay Due',            route: '/Due/Pay',                 icon: 'bi-credit-card',   category: 'Due' },
        { name: 'Ageing Dues',        route: '/Due/AgingDues/Index',     icon: 'bi-clock-history', category: 'Due' },
        { name: 'Reports & Analytics',route: '/Reports/Index',          icon: 'bi-bar-chart-line',category: 'Reports' },
        { name: 'Custom Audit Report',route: '/Reports/Index',          icon: 'bi-journal-text',  category: 'Reports' },
        { name: 'Add Expense',        route: '/Expenses/Add',           icon: 'bi-cash-stack',    category: 'Expenses' },
        { name: 'Notifications',      route: '/Notifications/Index',    icon: 'bi-bell',          category: 'Other' },
        // Admin features
        { name: 'Settings & Bill Design', route: '/Admin/Settings/Index', icon: 'bi-sliders',    category: 'Admin' },
        { name: 'Medicine Master List', route: '/Admin/MedicineMaster/Index', icon: 'bi-capsule',   category: 'Admin' },
        { name: 'Manage Medicines',   route: '/Admin/Medicines/Index',  icon: 'bi-capsule',       category: 'Admin' },
        { name: 'Add New Medicine',   route: '/Admin/Medicines/Create',  icon: 'bi-plus-circle',  category: 'Admin' },
        { name: 'Expenses',           route: '/Admin/Expenses/Index',    icon: 'bi-cash-stack',   category: 'Admin' },
        { name: 'Add New Expense',    route: '/Admin/Expenses/Create',   icon: 'bi-plus-circle',  category: 'Admin' },
        { name: 'Payables',           route: '/Admin/Payables/Index',    icon: 'bi-credit-card',  category: 'Admin' },
        { name: 'Ageing Payables',    route: '/Due/Payables/Ageing/Index', icon: 'bi-clock-history', category: 'Admin' },
        { name: 'Add Payable',        route: '/Admin/Payables/Create',   icon: 'bi-plus-circle',  category: 'Admin' },
        { name: 'Manage Suppliers',   route: '/Admin/Suppliers/Index',   icon: 'bi-building',     category: 'Admin' },
        { name: 'Add Supplier',       route: '/Admin/Suppliers/Add',     icon: 'bi-plus-circle',  category: 'Admin' },
        { name: 'Staff Accounts',     route: '/Admin/Users/Index',       icon: 'bi-people',       category: 'Admin' },
        { name: 'Backup & Restore',   route: '/Admin/Backup/Index',      icon: 'bi-hdd',          category: 'Admin' },
        { name: 'Custom Date Backup', route: '/Admin/Backup/Index',      icon: 'bi-calendar-range', category: 'Admin' },
        { name: 'Audit Log',          route: '/Admin/AuditLog/Index',    icon: 'bi-shield-check', category: 'Admin' },
    ];

    // ── DOM refs ─────────────────────────────────────────────
    const overlay = document.getElementById('globalSearchOverlay');
    const input   = document.getElementById('globalSearchInput');
    const results = document.getElementById('globalSearchResults');
    const close   = document.getElementById('globalSearchClose');

    if (!overlay) return;

    let selectedIndex = -1;
    let currentResults = [];

    // ── Fuzzy match ──────────────────────────────────────────
    function fuzzyMatch(term, text) {
        if (!term) return true;
        const t = term.toLowerCase();
        const s = text.toLowerCase();
        let ti = 0;
        for (let si = 0; si < s.length && ti < t.length; si++) {
            if (s[si] === t[ti]) ti++;
        }
        return ti === t.length;
    }

    // ── Search ───────────────────────────────────────────────
    function performSearch(term) {
        if (!term || term.length < 1) {
            currentResults = [];
            selectedIndex = -1;
            results.innerHTML = '<div class="gs-empty">Start typing to search...</div>';
            return;
        }
        const matches = features.filter(f =>
            fuzzyMatch(term, f.name) || fuzzyMatch(term, f.category)
        );
        currentResults = matches;
        selectedIndex = -1;
        renderResults(matches, term);
    }

    function highlightText(text, term) {
        if (!term) return escapeHtml(text);
        const escaped = escapeHtml(text);
        const regex = new RegExp('(' + term.replace(/[.*+?^${}()|[\]\\]/g, '\\$&').split('').join('') + ')', 'gi');
        return escaped.replace(regex, '<mark>$1</mark>');
    }

    function escapeHtml(str) {
        const div = document.createElement('div');
        div.textContent = str;
        return div.innerHTML;
    }

    function getIconHtml(iconClass) {
        return '<i class="bi ' + iconClass + '"></i>';
    }

    function renderResults(matches, term) {
        if (matches.length === 0) {
            results.innerHTML = '<div class="gs-empty"><i class="bi bi-search"></i> No results found</div>';
            return;
        }
        let html = '';
        let lastCategory = '';
        matches.forEach(function (f, i) {
            if (f.category !== lastCategory) {
                if (lastCategory) html += '</div>';
                html += '<div class="gs-group"><div class="gs-group-label">' + escapeHtml(f.category) + '</div>';
                lastCategory = f.category;
            }
            html += '<a href="' + f.route + '" class="gs-item" data-index="' + i + '">'
                  + '<span class="gs-item-icon">' + getIconHtml(f.icon) + '</span>'
                  + '<span class="gs-item-name">' + highlightText(f.name, term) + '</span>'
                  + '</a>';
        });
        if (lastCategory) html += '</div>';
        results.innerHTML = html;
        updateSelection();
    }

    function navigateTo(route) {
        closeSearch();
        window.location.href = route;
    }

    // Event delegation on results
    results.addEventListener('click', function (e) {
        var item = e.target.closest('.gs-item');
        if (item) {
            e.preventDefault();
            navigateTo(item.getAttribute('href'));
        }
    });

    // ── Selection ────────────────────────────────────────────
    function updateSelection() {
        const items = results.querySelectorAll('.gs-item');
        items.forEach(function (el, i) {
            el.classList.toggle('gs-selected', i === selectedIndex);
        });
        if (selectedIndex >= 0 && items[selectedIndex]) {
            items[selectedIndex].scrollIntoView({ block: 'nearest' });
        }
    }

    function selectNext() {
        const count = results.querySelectorAll('.gs-item').length;
        if (count === 0) return;
        selectedIndex = (selectedIndex + 1) % count;
        updateSelection();
    }

    function selectPrev() {
        const count = results.querySelectorAll('.gs-item').length;
        if (count === 0) return;
        selectedIndex = (selectedIndex - 1 + count) % count;
        updateSelection();
    }

    function activateSelected() {
        if (selectedIndex < 0) return;
        const item = results.querySelector('.gs-item[data-index="' + selectedIndex + '"]');
        if (item) item.click();
    }

    // ── Open / Close ─────────────────────────────────────────
    function openSearch() {
        overlay.classList.add('gs-open');
        document.body.style.overflow = 'hidden';
        setTimeout(function () { input.focus(); }, 50);
        selectedIndex = -1;
        currentResults = [];
        if (input.value) performSearch(input.value);
        else results.innerHTML = '<div class="gs-empty">Start typing to search...</div>';
    }

    function closeSearch() {
        overlay.classList.remove('gs-open');
        document.body.style.overflow = '';
        input.blur();
    }

    // ── Events ───────────────────────────────────────────────
    document.addEventListener('click', function (e) {
        if (e.target.closest('[data-search-trigger]')) openSearch();
    });
    close.addEventListener('click', closeSearch);
    overlay.addEventListener('click', function (e) {
        if (e.target === overlay) closeSearch();
    });

    input.addEventListener('input', function () {
        performSearch(this.value);
    });

    input.addEventListener('keydown', function (e) {
        if (e.key === 'ArrowDown') { e.preventDefault(); selectNext(); }
        else if (e.key === 'ArrowUp') { e.preventDefault(); selectPrev(); }
        else if (e.key === 'Enter') { e.preventDefault(); activateSelected(); }
        else if (e.key === 'Escape') { closeSearch(); }
    });

    document.addEventListener('keydown', function (e) {
        if ((e.ctrlKey || e.metaKey) && e.key === 'k') {
            e.preventDefault();
            if (overlay.classList.contains('gs-open')) closeSearch();
            else openSearch();
        }
    });
})();
