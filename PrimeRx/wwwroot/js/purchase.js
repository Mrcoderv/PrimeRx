/**
 * purchase.js — Multi-item purchase entry UI
 * Handles medicine search (floating popup), dynamic items table, and form serialisation.
 */

(function () {
    'use strict';

    // ── State ────────────────────────────────────────────────────────────────
    let items = [];
    let searchResults = [];
    let selectedIndex = -1;
    let searchTimer = null;

    // ── DOM refs ─────────────────────────────────────────────────────────────
    const searchInput   = document.getElementById('medicineSearchText');
    const popup         = document.getElementById('medicinePopup');
    const popupBody     = document.getElementById('medicinePopupBody');
    const itemsBody     = document.getElementById('itemsBody');
    const emptyRow      = document.getElementById('emptyRow');
    const totalDisplay  = document.getElementById('totalDisplay');
    const sidebarTotal  = document.getElementById('sidebarTotal');
    const itemsJsonInput= document.getElementById('itemsJson');
    const saveBtn       = document.getElementById('saveBtn');

    // ── Pre-load existing items (Edit page) ──────────────────────────────────
    if (typeof EXISTING_ITEMS !== 'undefined' && Array.isArray(EXISTING_ITEMS) && EXISTING_ITEMS.length > 0) {
        EXISTING_ITEMS.forEach(i => {
            items.push({
                id: i.id || 0,
                medicineId: i.medicineId,
                medicineName: i.medicineName,
                qty: i.quantity,
                freeQty: i.freeQuantity || 0,
                discountPercent: i.discountPercent || 0,
                purchasePrice: i.purchasePrice,
                mrp: i.mrp,
                batchNumber: i.batchNumber || '',
                expiryDate: i.expiryDate ? i.expiryDate.substring(0, 10) : ''
            });
        });
        renderTable();
    }

    // ── Medicine search (floating popup) ─────────────────────────────────────
    searchInput.addEventListener('input', () => {
        clearTimeout(searchTimer);
        const term = searchInput.value.trim();
        if (term.length < 2) { hidePopup(); return; }
        searchTimer = setTimeout(() => fetchMedicines(term), 200);
    });

    searchInput.addEventListener('keydown', e => {
        if (popup.style.display === 'none') return;
        if (e.key === 'ArrowDown') { e.preventDefault(); moveSel(1); }
        else if (e.key === 'ArrowUp') { e.preventDefault(); moveSel(-1); }
        else if (e.key === 'Enter') { e.preventDefault(); if (selectedIndex >= 0) selectMedicine(searchResults[selectedIndex]); }
        else if (e.key === 'Escape') hidePopup();
    });

    document.addEventListener('click', e => {
        if (!popup.contains(e.target) && e.target !== searchInput) hidePopup();
    });

    window.addEventListener('scroll', () => { if (popup.style.display !== 'none') positionPopup(); }, true);
    window.addEventListener('resize', () => { if (popup.style.display !== 'none') positionPopup(); });

    async function fetchMedicines(term) {
        showLoadingPopup();
        try {
            const res = await fetch(`?handler=Search&term=${encodeURIComponent(term)}`);
            searchResults = await res.json();
            renderPopup();
        } catch (err) { console.error('Medicine search failed', err); hidePopup(); }
    }

    function showLoadingPopup() {
        popupBody.innerHTML = `<div class="medicine-popup-loading"><span class="spinner"></span>Searching…</div>`;
        positionPopup();
        popup.style.display = '';
    }

    function renderPopup() {
        if (!searchResults.length) {
            popupBody.innerHTML = `<div class="medicine-popup-empty">No medicines found</div>`;
            positionPopup();
            popup.style.display = '';
            return;
        }

        selectedIndex = -1;
        popupBody.innerHTML = `<div class="medicine-popup-section">
            <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5"><circle cx="11" cy="11" r="8"/><path d="m21 21-4.35-4.35"/></svg>
            Results
        </div>`;

        const frag = document.createDocumentFragment();
        searchResults.forEach((m, i) => {
            const isLow = m.stockQuantity <= (m.lowStockThreshold ?? 10);
            const isOut = m.stockQuantity === 0;
            const stockBadge = isOut
                ? `<span class="purchase-stock-badge out-of-stock">Out</span>`
                : isLow
                    ? `<span class="purchase-stock-badge low-stock">⚠ ${m.stockQuantity}</span>`
                    : `<span class="purchase-stock-ok">${m.stockQuantity}</span>`;

            const row = document.createElement('div');
            row.className = 'medicine-popup-row' + (isLow && !isOut ? ' low-stock-row' : '');
            row.dataset.idx = i;
            row.innerHTML = `
                <div style="flex:1;min-width:0">
                    <div class="fw-semibold" style="white-space:nowrap;overflow:hidden;text-overflow:ellipsis">${esc(m.name)}</div>
                    ${m.genericName ? `<div style="font-size:0.78rem;opacity:0.65">${esc(m.genericName)}${m.formType ? ' · ' + esc(m.formType) : ''}</div>` : ''}
                    ${m.manufacturer ? `<div style="font-size:0.72rem;opacity:0.5">${esc(m.manufacturer)}</div>` : ''}
                </div>
                <div style="text-align:right;flex-shrink:0;padding-left:0.5rem">
                    <div style="font-size:0.8rem;opacity:0.75">${m.purchasePrice > 0 ? 'Rs.' + m.purchasePrice.toFixed(2) : '—'}</div>
                    <div>${stockBadge}</div>
                </div>`;
            row.addEventListener('mousedown', e => { e.preventDefault(); selectMedicine(searchResults[+row.dataset.idx]); });
            frag.appendChild(row);
        });
        popupBody.appendChild(frag);

        positionPopup();
        popup.style.display = '';
        if (searchResults.length > 0) {
            moveSel(1);
        }
    }

    function positionPopup() {
        const rect = searchInput.getBoundingClientRect();
        const viewH = window.innerHeight;
        const spaceBelow = viewH - rect.bottom;
        const spaceAbove = rect.top;

        popup.style.left  = rect.left + 'px';
        popup.style.width = rect.width + 'px';

        if (spaceBelow >= 200 || spaceBelow >= spaceAbove) {
            popup.style.top    = (rect.bottom + 4) + 'px';
            popup.style.bottom = 'auto';
            popupBody.style.maxHeight = Math.min(340, spaceBelow - 80) + 'px';
        } else {
            popup.style.bottom = (viewH - rect.top + 4) + 'px';
            popup.style.top    = 'auto';
            popupBody.style.maxHeight = Math.min(340, spaceAbove - 80) + 'px';
        }
    }

    function moveSel(dir) {
        const rows = popupBody.querySelectorAll('.medicine-popup-row');
        rows.forEach(r => r.classList.remove('active'));
        selectedIndex = Math.max(0, Math.min(rows.length - 1, selectedIndex + dir));
        rows[selectedIndex]?.classList.add('active');
        rows[selectedIndex]?.scrollIntoView({ block: 'nearest' });
    }

    function hidePopup() {
        popup.style.display = 'none';
        searchResults = [];
        selectedIndex = -1;
    }

    function selectMedicine(m) {
        hidePopup();
        searchInput.value = '';

        if (items.find(i => i.medicineId === m.id)) {
            items.find(i => i.medicineId === m.id).qty += 1;
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
            freeQty: 0,
            discountPercent: 0,
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
                <td>
                    <div class="fw-semibold">${esc(item.medicineName)}</div>
                </td>
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
                    <input type="number" class="form-control form-control-sm text-center"
                           value="${item.freeQty}" min="0" title="Free/Bonus units"
                           onchange="window.__purchaseUpdateField(${idx},'freeQty',Math.max(0,+this.value||0))" />
                </td>
                <td>
                    <input type="number" class="form-control form-control-sm"
                           value="${item.purchasePrice}" min="0" step="0.01"
                           onchange="window.__purchaseUpdatePP(${idx},+this.value||0)" />
                </td>
                <td>
                    <input type="number" class="form-control form-control-sm text-center"
                           value="${item.discountPercent}" min="0" max="100" step="0.01" title="Discount %"
                           onchange="window.__purchaseUpdateField(${idx},'discountPercent',Math.min(100,Math.max(0,+this.value||0)))" />
                </td>
                <td>
                    <input type="number" class="form-control form-control-sm"
                           value="${item.mrp.toFixed(2)}" min="0" step="0.01"
                           onchange="window.__purchaseUpdateField(${idx},'mrp',+this.value||0)" />
                </td>
                <td class="fw-semibold text-end align-middle">
                    Rs. ${(item.qty * item.purchasePrice * (1 - item.discountPercent / 100)).toFixed(2)}
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
        const total = items.reduce((s, i) => s + i.qty * i.purchasePrice * (1 - i.discountPercent / 100), 0);
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
            freeQuantity: i.freeQty || 0,
            discountPercent: i.discountPercent || 0,
            purchasePrice: i.purchasePrice,
            mrp: i.mrp,
            batchNumber: i.batchNumber || null,
            expiryDate: i.expiryDate || null
        }));
        itemsJsonInput.value = JSON.stringify(payload);
    }

    // ── Global callbacks (called from inline onchange) ────────────────────────
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
