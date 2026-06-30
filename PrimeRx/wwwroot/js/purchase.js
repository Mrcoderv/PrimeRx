/**
 * purchase.js — Multi-item purchase entry UI
 * Handles medicine search, dynamic items table, and form serialisation.
 */

(function () {
    'use strict';

    // ── State ────────────────────────────────────────────────────────────────
    let items = [];          // { id, medicineId, medicineName, qty, purchasePrice, mrp, batchNumber, expiryDate }
    let searchResults = [];
    let selectedIndex = -1;
    let searchTimer = null;

    // ── DOM refs ─────────────────────────────────────────────────────────────
    const searchInput   = document.getElementById('medicineSearchText');
    const dropdown      = document.getElementById('medicineSearchDropdown');
    const dropdownBody  = document.getElementById('medicineDropdownBody');
    const itemsBody     = document.getElementById('itemsBody');
    const emptyRow      = document.getElementById('emptyRow');
    const totalDisplay  = document.getElementById('totalDisplay');
    const sidebarTotal  = document.getElementById('sidebarTotal');
    const itemsJsonInput= document.getElementById('itemsJson');
    const saveBtn       = document.getElementById('saveBtn');

    // ── Pre-load existing items (Edit page) ───────────────────────────────────
    if (typeof EXISTING_ITEMS !== 'undefined' && Array.isArray(EXISTING_ITEMS) && EXISTING_ITEMS.length > 0) {
        EXISTING_ITEMS.forEach(i => {
            items.push({
                id: i.id || 0,
                medicineId: i.medicineId,
                medicineName: i.medicineName,
                qty: i.quantity,
                purchasePrice: i.purchasePrice,
                mrp: i.mrp,
                batchNumber: i.batchNumber || '',
                expiryDate: i.expiryDate ? i.expiryDate.substring(0, 10) : ''
            });
        });
        renderTable();
    }

    // ── Medicine search ───────────────────────────────────────────────────────
    searchInput.addEventListener('input', () => {
        clearTimeout(searchTimer);
        const term = searchInput.value.trim();
        if (term.length < 2) { hideDropdown(); return; }
        searchTimer = setTimeout(() => fetchMedicines(term), 200);
    });

    searchInput.addEventListener('keydown', e => {
        if (!dropdown.style || dropdown.style.display === 'none') return;
        if (e.key === 'ArrowDown') { e.preventDefault(); moveSel(1); }
        else if (e.key === 'ArrowUp') { e.preventDefault(); moveSel(-1); }
        else if (e.key === 'Enter') { e.preventDefault(); if (selectedIndex >= 0) selectMedicine(searchResults[selectedIndex]); }
        else if (e.key === 'Escape') hideDropdown();
    });

    document.addEventListener('click', e => {
        if (!dropdown.contains(e.target) && e.target !== searchInput) hideDropdown();
    });

    async function fetchMedicines(term) {
        try {
            const res = await fetch(`?handler=Search&term=${encodeURIComponent(term)}`);
            searchResults = await res.json();
            renderDropdown();
        } catch { hideDropdown(); }
    }

    function renderDropdown() {
        if (!searchResults.length) { hideDropdown(); return; }
        selectedIndex = -1;
        dropdownBody.innerHTML = searchResults.map((m, i) =>
            `<div class="medicine-dropdown-item" data-idx="${i}">
                <span style="width:55%">${esc(m.name)}${m.genericName ? `<br><small class="text-muted">${esc(m.genericName)}</small>` : ''}</span>
                <span style="width:25%"><small class="text-muted">${m.purchasePrice > 0 ? 'Rs.'+m.purchasePrice.toFixed(2) : '—'}</small></span>
                <span style="width:20%"><small>${m.stockQuantity}</small></span>
            </div>`
        ).join('');
        dropdownBody.querySelectorAll('.medicine-dropdown-item').forEach(el => {
            el.addEventListener('mousedown', e => { e.preventDefault(); selectMedicine(searchResults[+el.dataset.idx]); });
        });
        dropdown.style.display = '';
    }

    function moveSel(dir) {
        const rows = dropdownBody.querySelectorAll('.medicine-dropdown-item');
        rows.forEach(r => r.classList.remove('active'));
        selectedIndex = Math.max(0, Math.min(rows.length - 1, selectedIndex + dir));
        rows[selectedIndex]?.classList.add('active');
        rows[selectedIndex]?.scrollIntoView({ block: 'nearest' });
    }

    function hideDropdown() {
        dropdown.style.display = 'none';
        searchResults = [];
        selectedIndex = -1;
    }

    function selectMedicine(m) {
        hideDropdown();
        searchInput.value = '';

        // Check duplicate
        if (items.find(i => i.medicineId === m.id)) {
            const existing = items.find(i => i.medicineId === m.id);
            existing.qty += 1;
            renderTable();
            return;
        }

        const margin = typeof MARGIN_PERCENT !== 'undefined' ? MARGIN_PERCENT : 16;
        const mrp = m.mrp > 0 ? m.mrp : calcMrp(m.purchasePrice || 0, margin);

        items.push({
            id: 0,
            medicineId: m.id,
            medicineName: m.name,
            qty: 1,
            purchasePrice: m.purchasePrice || 0,
            mrp: mrp,
            batchNumber: '',
            expiryDate: ''
        });
        renderTable();
        searchInput.focus();
    }

    // ── Table rendering ───────────────────────────────────────────────────────
    function renderTable() {
        if (!items.length) {
            emptyRow.style.display = '';
            itemsBody.innerHTML = '';
            itemsBody.appendChild(emptyRow);
            updateTotals();
            return;
        }
        emptyRow.style.display = 'none';

        itemsBody.innerHTML = items.map((item, idx) => `
            <tr data-idx="${idx}">
                <td class="fw-semibold">${esc(item.medicineName)}</td>
                <td>
                    <input type="text" class="form-control form-control-sm"
                           value="${esc(item.batchNumber)}"
                           placeholder="Batch #"
                           onchange="window.__purchaseUpdateField(${idx},'batchNumber',this.value)" />
                </td>
                <td>
                    <input type="date" class="form-control form-control-sm"
                           value="${item.expiryDate}"
                           onchange="window.__purchaseUpdateField(${idx},'expiryDate',this.value)" />
                </td>
                <td>
                    <input type="number" class="form-control form-control-sm text-center"
                           value="${item.qty}" min="1"
                           onchange="window.__purchaseUpdateField(${idx},'qty',+this.value||1)" />
                </td>
                <td>
                    <input type="number" class="form-control form-control-sm"
                           value="${item.purchasePrice}" min="0" step="0.01"
                           onchange="window.__purchaseUpdatePP(${idx},+this.value||0)" />
                </td>
                <td>
                    <input type="number" class="form-control form-control-sm"
                           value="${item.mrp.toFixed(2)}" min="0" step="0.01"
                           onchange="window.__purchaseUpdateField(${idx},'mrp',+this.value||0)" />
                </td>
                <td class="fw-semibold text-end align-middle">
                    Rs. ${(item.qty * item.purchasePrice).toFixed(2)}
                </td>
                <td class="align-middle">
                    <button type="button" class="btn btn-sm btn-outline-danger" onclick="window.__purchaseRemove(${idx})">
                        <i class="bi bi-x"></i>
                    </button>
                </td>
            </tr>
        `).join('');

        updateTotals();
    }

    function updateTotals() {
        const total = items.reduce((s, i) => s + i.qty * i.purchasePrice, 0);
        const formatted = 'Rs. ' + total.toFixed(2);
        if (totalDisplay) totalDisplay.textContent = formatted;
        if (sidebarTotal) sidebarTotal.textContent = formatted;
        if (saveBtn) saveBtn.disabled = items.length === 0;
        serializeItems();
    }

    function serializeItems() {
        const payload = items.map(i => ({
            id: i.id,
            medicineId: i.medicineId,
            medicineName: i.medicineName,
            quantity: i.qty,
            purchasePrice: i.purchasePrice,
            mrp: i.mrp,
            batchNumber: i.batchNumber || null,
            expiryDate: i.expiryDate || null
        }));
        itemsJsonInput.value = JSON.stringify(payload);
    }

    // ── Global callbacks (called from inline onchange) ──────────────────────
    window.__purchaseUpdateField = function (idx, field, value) {
        items[idx][field] = value;
        renderTable();
    };

    window.__purchaseUpdatePP = function (idx, value) {
        const margin = typeof MARGIN_PERCENT !== 'undefined' ? MARGIN_PERCENT : 16;
        items[idx].purchasePrice = value;
        items[idx].mrp = calcMrp(value, margin);
        renderTable();
    };

    window.__purchaseRemove = function (idx) {
        items.splice(idx, 1);
        renderTable();
    };

    // ── Helpers ───────────────────────────────────────────────────────────────
    function calcMrp(pp, margin) {
        return Math.round(pp * (1 + margin / 100) * 100) / 100;
    }

    function esc(str) {
        return String(str ?? '').replace(/&/g,'&amp;').replace(/</g,'&lt;').replace(/>/g,'&gt;').replace(/"/g,'&quot;');
    }

})();
