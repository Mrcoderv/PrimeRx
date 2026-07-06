/**
 * purchase.js — Enhanced multi-item purchase entry UI
 * Smart Enter navigation, CC, calculator, batch info, master data
 */
(function () {
    'use strict';

    // ── State ────────────────────────────────────────────────────────────────
    let items = [];
    let searchResults = [];
    let selectedIndex = -1;
    let searchTimer = null;
    let itemIndex = 0;

    // ── DOM refs ─────────────────────────────────────────────────────────────
    const searchInput   = document.getElementById('medicineSearchText');
    const popup         = document.getElementById('medicinePopup');
    const popupBody     = document.getElementById('medicinePopupBody');
    const itemsBody     = document.getElementById('itemsBody');
    const emptyRow      = document.getElementById('emptyRow');
    const itemsJsonInput= document.getElementById('itemsJson');
    const saveBtn       = document.getElementById('saveBtn');
    const subtotalDisp  = document.getElementById('subtotalDisplay');
    const discountDisp  = document.getElementById('discountDisplay');
    const ccTotalDisp   = document.getElementById('ccTotalDisplay');
    const netTotalDisp  = document.getElementById('netTotalDisplay');
    const sidebarSubtotal = document.getElementById('sidebarSubtotal');
    const sidebarDiscount = document.getElementById('sidebarDiscount');
    const sidebarCC      = document.getElementById('sidebarCC');
    const sidebarNetTotal = document.getElementById('sidebarNetTotal');
    const batchInfoPanel = document.getElementById('batchInfoPanel');
    const batchInfoBody  = document.getElementById('batchInfoBody');

    // ── Helpers ───────────────────────────────────────────────────────────────
    function esc(str) {
        return String(str ?? '').replace(/&/g,'&amp;').replace(/</g,'&lt;').replace(/>/g,'&gt;').replace(/"/g,'&quot;');
    }

    function fmt(n) {
        return Number(n).toFixed(2);
    }

    function calcMrp(pp, margin) {
        return Math.round(pp * (1 + margin / 100) * 100) / 100;
    }

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
                conversionCharge: i.conversionCharge || 0,
                batchNumber: i.batchNumber || '',
                expiryDate: i.expiryDate ? i.expiryDate.substring(0, 10) : '',
                genericName: i.genericName || '',
                manufacturer: i.manufacturer || '',
                formType: i.formType || '',
                strength: i.strength || '',
                unit: i.unit || ''
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

    searchInput.addEventListener('focus', () => {
        const term = searchInput.value.trim();
        if (term.length >= 2) fetchMedicines(term);
    });

    searchInput.addEventListener('keydown', e => {
        if (popup.style.display === 'none') {
            if (e.key === 'Enter') e.preventDefault();
            return;
        }
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

        // If already in list, increment qty
        const existing = items.find(i => i.medicineId === m.id);
        if (existing) {
            existing.qty += 1;
            renderTable();
            focusField(existing._idx, 'qty');
            return;
        }

        const margin = typeof MARGIN_PERCENT !== 'undefined' ? MARGIN_PERCENT : 16;
        const mrp = m.mrp > 0 ? m.mrp : calcMrp(m.purchasePrice || 0, margin);

        const idx = itemIndex++;
        items.push({
            _idx: idx,
            id: 0,
            medicineId: m.id,
            medicineName: m.name,
            genericName: m.genericName || '',
            manufacturer: m.manufacturer || '',
            formType: m.formType || '',
            strength: m.strength || '',
            unit: m.unit || '',
            qty: 1,
            freeQty: 0,
            discountPercent: 0,
            purchasePrice: m.purchasePrice || 0,
            mrp: mrp,
            conversionCharge: 0,
            batchNumber: '',
            expiryDate: ''
        });
        renderTable();
        // Focus the qty input of the new row
        const newRow = itemsBody.querySelector(`tr[data-idx="${idx}"]`);
        if (newRow) {
            const qtyInput = newRow.querySelector('.qty-input');
            if (qtyInput) setTimeout(() => qtyInput.focus(), 50);
        }
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

        itemsBody.innerHTML = items.map((item, idx) => {
            const lineAmount = item.qty * item.purchasePrice * (1 - item.discountPercent / 100);
            const lineTotal = lineAmount + item.conversionCharge;
            const masterInfo = item.genericName || item.manufacturer
                ? esc([item.genericName, item.manufacturer].filter(Boolean).join(' · '))
                : '';
            return `<tr data-idx="${item._idx}">
                <td>
                    <div class="purchase-med-info">
                        <div class="purchase-med-name">${esc(item.medicineName)}</div>
                        ${masterInfo ? `<div class="purchase-med-master">${masterInfo}</div>` : ''}
                    </div>
                </td>
                <td>
                    <input type="text" class="form-control form-control-sm batch-input"
                           value="${esc(item.batchNumber)}"
                           placeholder="Batch#"
                           data-idx="${item._idx}" />
                </td>
                <td>
                    <input type="date" class="form-control form-control-sm expiry-input"
                           value="${item.expiryDate}"
                           data-idx="${item._idx}" />
                </td>
                <td>
                    <input type="number" class="form-control form-control-sm text-center qty-input"
                           value="${item.qty}" min="1" data-idx="${item._idx}" />
                </td>
                <td>
                    <input type="number" class="form-control form-control-sm text-center free-input"
                           value="${item.freeQty}" min="0" title="Free/Bonus" data-idx="${item._idx}" />
                </td>
                <td>
                    <input type="number" class="form-control form-control-sm rate-input"
                           value="${item.purchasePrice.toFixed(2)}" min="0" step="0.01" data-idx="${item._idx}" />
                </td>
                <td>
                    <input type="number" class="form-control form-control-sm text-center disc-input"
                           value="${item.discountPercent}" min="0" max="100" step="0.01" data-idx="${item._idx}" />
                </td>
                <td>
                    <input type="number" class="form-control form-control-sm text-center cc-input"
                           value="${item.conversionCharge.toFixed(2)}" min="0" step="0.01" data-idx="${item._idx}" />
                </td>
                <td>
                    <input type="number" class="form-control form-control-sm mrp-input"
                           value="${item.mrp.toFixed(2)}" min="0" step="0.01" data-idx="${item._idx}" />
                </td>
                <td class="fw-semibold text-end align-middle purchase-calc-field">
                    Rs. ${fmt(lineTotal)}
                </td>
                <td class="align-middle">
                    <button type="button" class="btn btn-sm btn-outline-danger btn-remove-item" data-idx="${item._idx}">&times;</button>
                </td>
            </tr>`;
        }).join('');

        // ── Wire up events ──────────────────────────────────────────────────
        itemsBody.querySelectorAll('.batch-input').forEach(el => bindInput(el, 'batchNumber'));
        itemsBody.querySelectorAll('.expiry-input').forEach(el => bindInput(el, 'expiryDate'));
        itemsBody.querySelectorAll('.free-input').forEach(el => bindInput(el, 'freeQty', v => Math.max(0, +v || 0)));
        itemsBody.querySelectorAll('.disc-input').forEach(el => bindInput(el, 'discountPercent', v => Math.min(100, Math.max(0, +v || 0)), true));
        itemsBody.querySelectorAll('.mrp-input').forEach(el => bindInput(el, 'mrp', v => +v || 0));
        itemsBody.querySelectorAll('.qty-input').forEach(el => bindQtyRate(el));
        itemsBody.querySelectorAll('.rate-input').forEach(el => bindQtyRate(el));
        itemsBody.querySelectorAll('.cc-input').forEach(el => bindCC(el));

        // Remove buttons
        itemsBody.querySelectorAll('.btn-remove-item').forEach(btn => {
            btn.addEventListener('click', () => {
                const idx = parseInt(btn.dataset.idx);
                const i = items.findIndex(x => x._idx === idx);
                if (i >= 0) items.splice(i, 1);
                renderTable();
                if (items.length === 0) searchInput.focus();
            });
        });

        // ── Keyboard navigation (Smart Enter) ────────────────────────────────
        const fieldOrder = ['batch-input','expiry-input','qty-input','free-input','rate-input','disc-input','cc-input','mrp-input'];

        itemsBody.querySelectorAll('input').forEach(input => {
            input.addEventListener('keydown', e => {
                if (e.key === 'Enter') {
                    e.preventDefault();
                    const cls = input.className;
                    let nextField = null;

                    // Find next field in order
                    for (let i = 0; i < fieldOrder.length - 1; i++) {
                        if (cls.includes(fieldOrder[i])) {
                            const tr = input.closest('tr');
                            nextField = tr ? tr.querySelector(`.${fieldOrder[i+1]}`) : null;
                            break;
                        }
                    }

                    // If MRP or last field, auto-add new row + focus search
                    if (cls.includes('mrp-input') || !nextField) {
                        searchInput.focus();
                        return;
                    }

                    if (nextField) nextField.focus();
                    return;
                }

                // Arrow Up/Down navigation between rows
                if (e.key === 'ArrowUp' || e.key === 'ArrowDown') {
                    const tr = input.closest('tr');
                    if (!tr) return;
                    const sibling = e.key === 'ArrowUp' ? tr.previousElementSibling : tr.nextElementSibling;
                    if (!sibling) return;
                    e.preventDefault();
                    const targetInput = sibling.querySelector(`.${input.classList[1]}`) || sibling.querySelector('input');
                    if (targetInput) targetInput.focus();
                }
            });
        });

        // Batch# - show batch info panel on focus
        itemsBody.querySelectorAll('.batch-input').forEach(el => {
            el.addEventListener('focus', (e) => {
                const tr = e.target.closest('tr');
                if (!tr) return;
                const idx = parseInt(e.target.dataset.idx);
                const item = items.find(x => x._idx === idx);
                if (!item) return;
                // Show a quick info popup about this medicine's details
                showBatchInfo(e.target, item);
            });
            el.addEventListener('blur', () => {
                setTimeout(() => {
                    if (!batchInfoPanel.contains(document.activeElement)) hideBatchInfo();
                }, 150);
            });
        });

        updateTotals();
    }

    function bindInput(el, field, transform, recalcMargin) {
        el.addEventListener('change', () => {
            const idx = parseInt(el.dataset.idx);
            const item = items.find(x => x._idx === idx);
            if (!item) return;
            const val = transform ? transform(el.value) : el.value;
            item[field] = val;
            if (recalcMargin && item.purchasePrice > 0) {
                item.mrp = calcMrp(item.purchasePrice, typeof MARGIN_PERCENT !== 'undefined' ? MARGIN_PERCENT : 16);
            }
            renderTable();
        });
    }

    function bindQtyRate(el) {
        el.addEventListener('change', () => {
            const idx = parseInt(el.dataset.idx);
            const item = items.find(x => x._idx === idx);
            if (!item) return;
            if (el.classList.contains('qty-input')) {
                item.qty = Math.max(1, parseInt(el.value) || 1);
            } else {
                item.purchasePrice = Math.max(0, parseFloat(el.value) || 0);
                item.mrp = calcMrp(item.purchasePrice, typeof MARGIN_PERCENT !== 'undefined' ? MARGIN_PERCENT : 16);
            }
            renderTable();
        });
    }

    function bindCC(el) {
        el.addEventListener('change', () => {
            const idx = parseInt(el.dataset.idx);
            const item = items.find(x => x._idx === idx);
            if (!item) return;
            item.conversionCharge = Math.max(0, parseFloat(el.value) || 0);
            renderTable();
        });
    }

    function focusField(idx, field) {
        const tr = itemsBody.querySelector(`tr[data-idx="${idx}"]`);
        if (!tr) return;
        const input = tr.querySelector(`.${field}-input`);
        if (input) input.focus();
    }

    // ── Batch Info Panel ─────────────────────────────────────────────────────
    function showBatchInfo(anchor, item) {
        const rect = anchor.getBoundingClientRect();
        const viewH = window.innerHeight;
        const spaceBelow = viewH - rect.bottom - 8;

        batchInfoPanel.style.left = rect.left + 'px';
        if (spaceBelow >= 160) {
            batchInfoPanel.style.top = (rect.bottom + 4) + 'px';
            batchInfoPanel.style.bottom = 'auto';
        } else {
            batchInfoPanel.style.bottom = (viewH - rect.top + 4) + 'px';
            batchInfoPanel.style.top = 'auto';
        }

        batchInfoBody.innerHTML = `
            <div class="batch-info-row"><span class="batch-info-label">Medicine</span><span class="batch-info-value">${esc(item.medicineName)}</span></div>
            ${item.genericName ? `<div class="batch-info-row"><span class="batch-info-label">Generic</span><span class="batch-info-value">${esc(item.genericName)}</span></div>` : ''}
            ${item.manufacturer ? `<div class="batch-info-row"><span class="batch-info-label">Company</span><span class="batch-info-value">${esc(item.manufacturer)}</span></div>` : ''}
            ${item.formType ? `<div class="batch-info-row"><span class="batch-info-label">Form</span><span class="batch-info-value">${esc(item.formType)}</span></div>` : ''}
            ${item.strength ? `<div class="batch-info-row"><span class="batch-info-label">Strength</span><span class="batch-info-value">${esc(item.strength)}</span></div>` : ''}
            ${item.unit ? `<div class="batch-info-row"><span class="batch-info-label">Unit</span><span class="batch-info-value">${esc(item.unit)}</span></div>` : ''}
            <div class="batch-info-row"><span class="batch-info-label">Batch #</span><span class="batch-info-value">${esc(item.batchNumber || '—')}</span></div>
            <div class="batch-info-row"><span class="batch-info-label">Expiry</span><span class="batch-info-value">${item.expiryDate || '—'}</span></div>
            <div class="batch-info-row"><span class="batch-info-label">Rate</span><span class="batch-info-value">Rs. ${fmt(item.purchasePrice)}</span></div>
            <div class="batch-info-row"><span class="batch-info-label">MRP</span><span class="batch-info-value">Rs. ${fmt(item.mrp)}</span></div>
        `;
        batchInfoPanel.style.display = '';
    }

    function hideBatchInfo() {
        batchInfoPanel.style.display = 'none';
    }

    document.addEventListener('mousedown', e => {
        if (batchInfoPanel.style.display !== 'none' && !batchInfoPanel.contains(e.target)) {
            hideBatchInfo();
        }
    });

    // ── Floating Calculator ─────────────────────────────────────────────────
    const floatingCalc = document.getElementById('floatingCalc');
    let calcTarget = null;

    function showCalc(target) {
        calcTarget = target;
        const rect = target.getBoundingClientRect();
        floatingCalc.style.left = Math.max(4, rect.left) + 'px';
        floatingCalc.style.top = (rect.bottom + 4) + 'px';
        floatingCalc.style.display = '';
        const calcRect = floatingCalc.getBoundingClientRect();
        if (calcRect.right > window.innerWidth) {
            floatingCalc.style.left = (window.innerWidth - calcRect.width - 4) + 'px';
        }
    }

    function hideCalc() {
        floatingCalc.style.display = 'none';
        calcTarget = null;
    }

    document.addEventListener('keydown', e => {
        if (e.ctrlKey && e.shiftKey && (e.key === 'C' || e.key === 'c')) {
            e.preventDefault();
            const active = document.activeElement;
            if (active && (active.classList.contains('qty-input') || active.classList.contains('rate-input') || active.classList.contains('cc-input'))) {
                if (floatingCalc.style.display === 'none') {
                    showCalc(active);
                } else {
                    hideCalc();
                }
            }
        }
    });

    document.addEventListener('contextmenu', e => {
        const input = e.target.closest('.qty-input, .rate-input, .cc-input');
        if (input) {
            e.preventDefault();
            showCalc(input);
        }
    });

    floatingCalc.addEventListener('click', e => {
        const btn = e.target.closest('[data-calc]');
        if (!btn || !calcTarget) return;
        const val = btn.dataset.calc;
        if (val === 'Enter') {
            hideCalc();
            const tr = calcTarget.closest('tr');
            if (calcTarget.classList.contains('qty-input') && tr) {
                (tr.querySelector('.free-input') || tr.querySelector('.rate-input')).focus();
            } else if (calcTarget.classList.contains('rate-input') && tr) {
                tr.querySelector('.disc-input').focus();
            } else if (calcTarget.classList.contains('disc-input') && tr) {
                tr.querySelector('.cc-input').focus();
            } else if (calcTarget.classList.contains('cc-input') && tr) {
                tr.querySelector('.mrp-input').focus();
            }
            return;
        }
        if (val === 'C') {
            calcTarget.value = '0';
        } else if (val === '\u232B') {
            calcTarget.value = calcTarget.value.slice(0, -1) || '0';
        } else {
            if (calcTarget.value === '0' && val !== '.') {
                calcTarget.value = val;
            } else {
                calcTarget.value += val;
            }
        }
        calcTarget.dispatchEvent(new Event('input', { bubbles: true }));
        // Trigger change to recalc
        const changeEvent = new Event('change', { bubbles: true });
        calcTarget.dispatchEvent(changeEvent);
    });

    document.addEventListener('mousedown', e => {
        if (floatingCalc.style.display !== 'none' && !floatingCalc.contains(e.target) && e.target !== calcTarget) {
            hideCalc();
        }
    });

    // ── Totals ───────────────────────────────────────────────────────────────
    function updateTotals() {
        let subtotal = 0;
        let totalDiscount = 0;
        let totalCC = 0;

        items.forEach(item => {
            const gross = item.qty * item.purchasePrice;
            const discAmt = gross * (item.discountPercent / 100);
            subtotal += gross;
            totalDiscount += discAmt;
            totalCC += item.conversionCharge || 0;
        });

        const netAmount = subtotal - totalDiscount + totalCC;

        const fmtSub = 'Rs. ' + fmt(subtotal);
        const fmtDisc = '- Rs. ' + fmt(totalDiscount);
        const fmtCC = 'Rs. ' + fmt(totalCC);
        const fmtNet = 'Rs. ' + fmt(netAmount);

        if (subtotalDisp) subtotalDisp.textContent = fmtSub;
        if (discountDisp) discountDisp.textContent = fmtDisc;
        if (ccTotalDisp) ccTotalDisp.textContent = fmtCC;
        if (netTotalDisp) netTotalDisp.textContent = fmtNet;
        if (sidebarSubtotal) sidebarSubtotal.textContent = fmtSub;
        if (sidebarDiscount) sidebarDiscount.textContent = fmtDisc;
        if (sidebarCC) sidebarCC.textContent = fmtCC;
        if (sidebarNetTotal) sidebarNetTotal.textContent = fmtNet;

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
            conversionCharge: i.conversionCharge || 0,
            batchNumber: i.batchNumber || null,
            expiryDate: i.expiryDate || null,
            genericName: i.genericName || null,
            manufacturer: i.manufacturer || null,
            formType: i.formType || null,
            strength: i.strength || null,
            unit: i.unit || null
        }));
        itemsJsonInput.value = JSON.stringify(payload);
    }

})();
