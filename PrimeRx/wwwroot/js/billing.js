// PrimeRx - client-side bill builder
(function () {
    const items = [];
    let itemIndex = 0;
    let lastAddedTr = null;

    const itemsBody = document.getElementById('itemsBody');
    const emptyRow = document.getElementById('emptyRow');
    const itemsJson = document.getElementById('itemsJson');
    const generateBtn = document.getElementById('generateBtn');

    // Load initial bill items for edit mode
    (function loadInitialItems() {
        const dataEl = document.getElementById('initialBillData');
        if (!dataEl || !dataEl.textContent) return;
        try {
            const initialItems = JSON.parse(dataEl.textContent);
            if (!initialItems.length) return;
            initialItems.forEach(itemData => {
                const idx = itemIndex++;
                items.push({
                    index: idx,
                    medicineId: itemData.medicineId,
                    medicineName: itemData.medicineName,
                    rate: itemData.rate,
                    purchasePrice: itemData.purchasePrice || 0,
                    quantity: itemData.quantity,
                    availableStock: itemData.availableStock ?? 999,
                    discountPercent: itemData.discountPercent || 0,
                    discountAmount: itemData.discountAmount || 0,
                    batchId: itemData.selectedBatchId || null,
                    batchNumber: itemData.batchNumber || null,

                    expiryDate: itemData.expiryDate || null
                });
                renderRow(items[items.length - 1]);
                const lastTr = itemsBody.lastElementChild;
                if (lastTr) {
                    const qtyInput = lastTr.querySelector('.qty-input');
                    if (qtyInput) qtyInput.value = itemData.quantity ?? '';
                    if (itemData.selectedBatchId) {
                        const label = lastTr.querySelector('.batch-label');
                        if (label) label.textContent = itemData.batchNumber || 'Batch #' + itemData.selectedBatchId;
                        lastTr.querySelector('.batch-btn').classList.add('has-batch');
                    }
                }
            });
            recalcTotals();
        } catch (e) {
            console.error('Failed to load initial bill data', e);
        }
    })();

    function formatMoney(n) {
        return Number(n).toFixed(2);
    }

    function recalcTotals() {
        let subtotal = 0;
        let itemDiscount = 0;
        items.forEach(item => {
            const gross = (item.rate ?? 0) * (item.quantity ?? 0);
            const discountAmount = gross * ((item.discountPercent ?? 0) / 100);
            item.discountAmount = discountAmount;
            subtotal += gross;
            itemDiscount += discountAmount;
        });

        const totalDiscount = itemDiscount;
        const netAmount = subtotal - totalDiscount;

        const fmtSub = 'Rs. ' + formatMoney(subtotal);
        const fmtDisc = '- Rs. ' + formatMoney(totalDiscount);
        const fmtNet = 'Rs. ' + formatMoney(Math.max(0, netAmount));

        const subEl = document.getElementById('subtotalDisplay');
        const discEl = document.getElementById('discountDisplay');
        const netEl = document.getElementById('netTotalDisplay');
        if (subEl) subEl.textContent = fmtSub;
        if (discEl) discEl.textContent = fmtDisc;
        if (netEl) netEl.textContent = fmtNet;

        const sbSub = document.getElementById('sidebarSubtotal');
        const sbDisc = document.getElementById('sidebarDiscount');
        const sbNet = document.getElementById('sidebarNetTotal');
        if (sbSub) sbSub.textContent = fmtSub;
        if (sbDisc) sbDisc.textContent = fmtDisc;
        if (sbNet) sbNet.textContent = fmtNet;

        itemsJson.value = JSON.stringify(items.map(i => ({
            medicineId: i.medicineId,
            medicineName: i.medicineName,
            rate: i.rate,
            purchasePrice: i.purchasePrice,
            quantity: i.quantity,
            availableStock: i.availableStock,
            discountPercent: i.discountPercent,
            discountAmount: i.discountAmount,
            selectedBatchId: i.batchId ?? null
        })));

        generateBtn.disabled = items.length === 0;
        emptyRow.style.display = items.length === 0 ? '' : 'none';
    }

    function addItem(medicine) {
        if (items.some(i => i.medicineId === medicine.id)) {
            alert('Medicine already added. Update quantity in the table.');
            return;
        }

        const idx = itemIndex++;
        items.push({
            index: idx,
            medicineId: medicine.id,
            medicineName: medicine.name,
            rate: medicine.mrp,
            purchasePrice: medicine.purchasePrice || 0,
            quantity: 1,
            availableStock: medicine.stockQuantity,
            discountPercent: medicine.discountPercent || 0,
            discountAmount: 0,
            batchId: null,
            batchNumber: null,
            expiryDate: null
        });

        renderRow(items[items.length - 1]);
        recalcTotals();

        const lastTr = itemsBody.lastElementChild;
        if (lastTr) {
            const rateInput = lastTr.querySelector('.rate-input');
            if (rateInput) rateInput.focus();
        }
    }

    function getMarginFlag(rate, purchasePrice, discountPercent) {
        if (!purchasePrice || purchasePrice <= 0) return '';
        const margin = rate / purchasePrice;
        const effectiveRate = rate * (1 - (discountPercent || 0) / 100);
        const effectiveMargin = effectiveRate / purchasePrice;
        if (effectiveMargin < 1.05) {
            return '<span class="margin-flag danger" title="Low margin: ' + effectiveMargin.toFixed(2) + 'x (purchase: Rs. ' + purchasePrice.toFixed(2) + ')">⚠ High Disc.</span>';
        } else if (effectiveMargin < 1.10) {
            return '<span class="margin-flag warning" title="Reduced margin: ' + effectiveMargin.toFixed(2) + 'x (purchase: Rs. ' + purchasePrice.toFixed(2) + ')">' + effectiveMargin.toFixed(2) + 'x</span>';
        }
        return '';
    }

    function renderRow(item) {
        const tr = document.createElement('tr');
        tr.dataset.index = item.index;

        const marginFlag = getMarginFlag(item.rate, item.purchasePrice, item.discountPercent);
        const effectiveMargin = item.purchasePrice > 0 ? (item.rate * (1 - (item.discountPercent || 0) / 100)) / item.purchasePrice : 99;
        if (effectiveMargin < 1.05) {
            tr.classList.add('high-discount-row');
        }

        const nameTd = document.createElement('td');
        nameTd.innerHTML = `
            <div class="purchase-med-info">
                <div class="purchase-med-name">${escapeHtml(item.medicineName)}${marginFlag}</div>
                <small class="text-muted">Stock: ${item.availableStock}</small>
            </div>`;
        const batchBtn = document.createElement('button');
        batchBtn.type = 'button';
        batchBtn.className = 'batch-btn';
        batchBtn.title = 'Change batch';
        batchBtn.innerHTML = `<svg width="10" height="10" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5"><rect x="3" y="3" width="7" height="7"/><rect x="14" y="3" width="7" height="7"/><rect x="3" y="14" width="7" height="7"/><path d="M14 17h7m-3.5-3.5v7"/></svg><span class="batch-label">Auto batch</span> ▾`;
        nameTd.appendChild(batchBtn);

        const batchInfo = document.createElement('span');
        batchInfo.className = 'batch-info';
        if (item.batchId) {
            batchInfo.textContent = `Batch: ${item.batchNumber || '#' + item.batchId} | Exp: ${item.expiryDate || ''}`;
        } else {
            batchInfo.style.display = 'none';
        }
        nameTd.appendChild(batchInfo);

        tr.appendChild(nameTd);
        tr.innerHTML += `
            <td class="text-end"><input type="number" class="form-control form-control-sm text-end rate-input" value="${item.rate ?? ''}" step="0.01" min="0"></td>
            <td class="text-end"><input type="number" class="form-control form-control-sm text-end qty-input" value="${item.quantity ?? ''}" min="1" max="${item.availableStock}"></td>
            <td class="text-end"><input type="number" class="form-control form-control-sm text-end disc-percent-input" value="${item.discountPercent ?? ''}" step="0.1" min="0" max="100"></td>
            <td class="disc-amount text-end">${formatMoney(item.discountAmount ?? 0)}</td>
            <td class="line-total text-end">${formatMoney((item.rate ?? 0) * (item.quantity ?? 0) - (item.discountAmount ?? 0))}</td>
            <td><button type="button" class="btn btn-sm btn-outline-danger btn-remove-item">&times;</button></td>`;

        tr.querySelector('.batch-btn').addEventListener('click', e => {
            e.stopPropagation();
            openBatchPicker(e.currentTarget, item, tr);
        });

        tr.querySelector('.rate-input').addEventListener('input', e => {
            item.rate = parseFloat(e.target.value) || 0;
            updateMarginFlag(tr, item);
            updateLineTotal(tr, item);
            recalcTotals();
        });

        tr.querySelector('.qty-input').addEventListener('input', e => {
            let qty = parseInt(e.target.value) || 1;
            if (qty > item.availableStock) {
                qty = item.availableStock;
                e.target.value = qty;
                alert(`Only ${item.availableStock} in stock.`);
            }
            item.quantity = qty;
            updateLineTotal(tr, item);
            recalcTotals();
        });

        tr.querySelector('.disc-percent-input').addEventListener('input', e => {
            let percent = parseFloat(e.target.value) || 0;
            if (percent < 0) percent = 0;
            if (percent > 100) percent = 100;
            e.target.value = percent;
            item.discountPercent = percent;
            updateMarginFlag(tr, item);
            updateLineTotal(tr, item);
            recalcTotals();
        });

        tr.querySelector('.rate-input').addEventListener('keydown', e => {
            if (e.key === 'Enter') {
                e.preventDefault();
                tr.querySelector('.qty-input').focus();
            }
        });

        tr.querySelector('.qty-input').addEventListener('keydown', e => {
            if (e.key === 'Enter') {
                e.preventDefault();
                tr.querySelector('.disc-percent-input').focus();
            }
        });

        tr.querySelector('.disc-percent-input').addEventListener('keydown', e => {
            if (e.key === 'Enter') {
                e.preventDefault();
                searchInput.focus();
            }
        });

        tr.querySelector('.btn-remove-item').addEventListener('click', () => {
            const i = items.findIndex(x => x.index === item.index);
            if (i >= 0) items.splice(i, 1);
            tr.remove();
            if (lastAddedTr === tr) lastAddedTr = null;
            recalcTotals();
        });

        itemsBody.appendChild(tr);

        lastAddedTr = tr;
        tr.classList.add('row-just-added');
        setTimeout(() => tr.classList.remove('row-just-added'), 600);
    }

    function updateLineTotal(tr, item) {
        const gross = (item.rate ?? 0) * (item.quantity ?? 0);
        const discountAmount = gross * ((item.discountPercent ?? 0) / 100);
        item.discountAmount = discountAmount;
        tr.querySelector('.disc-amount').textContent = formatMoney(discountAmount);
        tr.querySelector('.line-total').textContent = formatMoney(gross - discountAmount);
    }

    function updateMarginFlag(tr, item) {
        const nameCell = tr.querySelector('td:first-child');
        const existingFlag = nameCell.querySelector('.margin-flag');
        if (existingFlag) existingFlag.remove();
        const flagHtml = getMarginFlag(item.rate, item.purchasePrice, item.discountPercent);
        if (flagHtml) {
            const medName = nameCell.childNodes[0];
            const wrapper = document.createElement('span');
            wrapper.innerHTML = flagHtml;
            medName.after(wrapper.firstChild);
        }
        const effectiveMargin = item.purchasePrice > 0 ? (item.rate * (1 - (item.discountPercent || 0) / 100)) / item.purchasePrice : 99;
        tr.classList.toggle('high-discount-row', effectiveMargin < 1.05);
    }

    // ── Batch picker ────────────────────────────────────────────────────────
    function escHtml(s) {
        return String(s ?? '').replaceAll('&','&amp;').replaceAll('<','&lt;').replaceAll('>','&gt;');
    }

    function updateBatchDisplay(tr, item) {
        const btn = tr.querySelector('.batch-btn');
        if (!btn) return;
        const label = btn.querySelector('.batch-label');
        if (item.batchId) {
            label.textContent = item.batchNumber || 'Batch #' + item.batchId;
            btn.classList.add('has-batch');
        } else {
            label.textContent = 'Auto batch';
            btn.classList.remove('has-batch');
        }
        const batchInfo = tr.querySelector('.batch-info');
        if (batchInfo) {
            if (item.batchId) {
                batchInfo.textContent = `Batch: ${item.batchNumber || '#' + item.batchId} | Exp: ${item.expiryDate || ''}`;
                batchInfo.style.display = '';
            } else {
                batchInfo.textContent = '';
                batchInfo.style.display = 'none';
            }
        }
        recalcTotals();
    }

    let closeBatchPickerOutside = null;

    function closeBatchPicker() {
        const existing = document.getElementById('batchPickerPopup');
        if (existing) existing.remove();
        if (closeBatchPickerOutside) {
            document.removeEventListener('mousedown', closeBatchPickerOutside);
            closeBatchPickerOutside = null;
        }
    }

    function openBatchPicker(btn, item, tr) {
        closeBatchPicker();

        const picker = document.createElement('div');
        picker.id = 'batchPickerPopup';
        picker.className = 'batch-picker';

        const rect = btn.getBoundingClientRect();
        const viewH = window.innerHeight;
        const spaceBelow = viewH - rect.bottom - 8;
        if (spaceBelow >= 120) {
            picker.style.top  = (rect.bottom + 4) + 'px';
        } else {
            picker.style.bottom = (viewH - rect.top + 4) + 'px';
        }
        picker.style.left = rect.left + 'px';

        picker.innerHTML = `<div class="batch-picker-header">Select Batch — ${escHtml(item.medicineName)}</div>
            <div class="batch-picker-loading"><div class="bp-spinner"></div> Loading batches…</div>`;
        document.body.appendChild(picker);

        fetch(`?handler=Batches&medicineId=${item.medicineId}`, { headers: { Accept: 'application/json' } })
            .then(r => r.json())
            .then(batches => {
                const body = document.createElement('div');

                const autoRow = document.createElement('div');
                autoRow.className = 'batch-picker-row' + (!item.batchId ? ' selected' : '');
                autoRow.innerHTML = `<span class="bp-number">Auto (FEFO)</span><span class="bp-qty">—</span><span class="bp-expiry">Earliest expiry first</span>${!item.batchId ? '<span class="bp-check">✓</span>' : '<span></span>'}`;
                autoRow.addEventListener('mousedown', e => {
                    e.preventDefault();
                    item.batchId = null; item.batchNumber = null; item.expiryDate = null;
                    const qtyInput = tr.querySelector('.qty-input');
                    qtyInput.max = item.availableStock;
                    updateBatchDisplay(tr, item);
                    closeBatchPicker();
                });
                body.appendChild(autoRow);

                if (!batches.length) {
                    const empty = document.createElement('div');
                    empty.className = 'batch-picker-empty';
                    empty.textContent = 'No batches with stock found';
                    body.appendChild(empty);
                } else {
                    batches.forEach(b => {
                        const row = document.createElement('div');
                        row.className = 'batch-picker-row' + (b.id === item.batchId ? ' selected' : '');
                        row.innerHTML = `<span class="bp-number">${escHtml(b.batchNumber)}</span><span class="bp-qty">${b.quantity} units</span><span class="bp-expiry">${escHtml(b.expiryDate || 'No expiry')}</span>${b.id === item.batchId ? '<span class="bp-check">✓</span>' : '<span></span>'}`;
                        row.addEventListener('mousedown', e => {
                            e.preventDefault();
                            item.batchId = b.id;
                            item.batchNumber = b.batchNumber;
                            item.expiryDate = b.expiryDate || '';
                            const qtyInput = tr.querySelector('.qty-input');
                            if (parseInt(qtyInput.value) > b.quantity) {
                                qtyInput.value = b.quantity;
                                item.quantity = b.quantity;
                            }
                            qtyInput.max = b.quantity;
                            updateBatchDisplay(tr, item);
                            updateLineTotal(tr, item);
                            closeBatchPicker();
                        });
                        body.appendChild(row);
                    });
                }

                picker.querySelector('.batch-picker-loading').replaceWith(body);
            })
            .catch(err => {
                console.error('Failed to load batches', err);
                picker.querySelector('.batch-picker-loading').textContent = 'Failed to load batches.';
            });

        closeBatchPickerOutside = e => {
            if (!picker.contains(e.target) && e.target !== btn) closeBatchPicker();
        };
        setTimeout(() => document.addEventListener('mousedown', closeBatchPickerOutside), 10);
    }

    // ── Payment Method Button Group ─────────────────────────────────────────
    const paymentMethodSelect = document.getElementById('paymentMethod');
    const paymentMethodGroup  = document.getElementById('paymentMethodGroup');
    const customerPhone       = document.getElementById('customerPhone');
    const customerPhoneHint   = document.getElementById('customerPhoneHint');
    const customerPhoneGroup  = document.getElementById('customerPhoneGroup');

    if (paymentMethodGroup) {
        function setPaymentMethod(value) {
            paymentMethodSelect.value = value;
            paymentMethodGroup.querySelectorAll('.payment-method-btn').forEach(function (btn) {
                btn.classList.toggle('active', btn.dataset.value === value);
            });
            updateCustomerPhoneRequirement();
        }

        paymentMethodGroup.addEventListener('click', function (e) {
            var btn = e.target.closest('.payment-method-btn');
            if (btn) setPaymentMethod(btn.dataset.value);
        });

        // Keep hidden select in sync for form submission
        paymentMethodSelect.addEventListener('change', function () {
            setPaymentMethod(this.value);
        });

        function updateCustomerPhoneRequirement() {
            if (paymentMethodSelect.value === 'Due') {
                customerPhone.setAttribute('required', 'required');
                customerPhoneHint.textContent = 'Required when payment is Due';
                customerPhoneHint.className = 'small text-danger';
                customerPhoneGroup.classList.add('phone-required');
            } else {
                customerPhone.removeAttribute('required');
                customerPhoneHint.textContent = 'Required when payment is Due';
                customerPhoneHint.className = 'small text-muted';
                customerPhoneGroup.classList.remove('phone-required');
            }
        }

        // Init default state
        setPaymentMethod(paymentMethodSelect.value);
    }

    // ── Floating medicine search popup ─────────────────────────────────────
    const searchInput = document.getElementById('medicineSearchText');
    const popup       = document.getElementById('medicinePopup');
    const popupBody   = document.getElementById('medicinePopupBody');

    let popupItems  = [];
    let activeIndex = -1;
    let debounceTimer = null;
    let fetchSeq    = 0;
    let isOpen      = false;

    const RECENT_KEY = 'primerx_recent_meds';
    const RECENT_MAX = 8;

    function loadRecent() {
        try { return JSON.parse(localStorage.getItem(RECENT_KEY) || '[]'); }
        catch (e) { console.error('Failed to load recent medicines', e); return []; }
    }

    function saveRecent(m) {
        const list = loadRecent().filter(r => String(r.id) !== String(m.id));
        list.unshift({
            id: m.id, name: m.name, genericName: m.genericName,
            mrp: m.mrp, purchasePrice: m.purchasePrice || 0,
            stockQuantity: m.stockQuantity,
            discountPercent: m.discountPercent, batch: m.batch
        });
        try { localStorage.setItem(RECENT_KEY, JSON.stringify(list.slice(0, RECENT_MAX))); }
        catch (e) { console.error('Failed to save recent medicines', e); }
    }

    function showRecent() {
        const list = loadRecent();
        if (!list.length) { closePopup(); return; }
        popupBody.innerHTML = `
            <div class="medicine-popup-section">
                <svg width="11" height="11" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5">
                    <circle cx="12" cy="12" r="10"/><polyline points="12 6 12 12 16 14"/>
                </svg>
                Recently Used
            </div>`;
        popupItems = list;
        const frag = document.createDocumentFragment();
        list.forEach(m => {
            const isLow = m.stockQuantity <= 5;
            const isOut = m.stockQuantity === 0;
            const stockBadgeHtml = isOut
                ? `<span class="purchase-stock-badge out-of-stock">Out</span>`
                : isLow
                    ? `<span class="purchase-stock-badge low-stock">⚠ ${m.stockQuantity}</span>`
                    : `<span class="purchase-stock-ok">${m.stockQuantity}</span>`;

            const row = document.createElement('div');
            row.className = 'medicine-popup-row' + (isLow && !isOut ? ' low-stock-row' : '');
            row.setAttribute('role', 'option');
            row.innerHTML = `
                <div style="flex:1;min-width:0">
                    <div class="fw-semibold" style="white-space:nowrap;overflow:hidden;text-overflow:ellipsis;color:var(--rx-heading)">${escapeHtml(m.name)}</div>
                    ${m.genericName ? `<div style="font-size:0.78rem;opacity:0.65">${escapeHtml(m.genericName)}</div>` : ''}
                </div>
                <div style="text-align:right;flex-shrink:0;padding-left:0.5rem">
                    <div style="font-size:0.8rem;opacity:0.75">Rs. ${Number(m.mrp || 0).toFixed(2)}</div>
                    <div>${stockBadgeHtml}</div>
                </div>`;
            row.addEventListener('mousedown', e => { e.preventDefault(); selectMedicine(m); });
            frag.appendChild(row);
        });
        popupBody.appendChild(frag);
        activeIndex = -1;
        openPopup();
    }

    function escapeHtml(s) {
        return String(s ?? '')
            .replaceAll('&', '&amp;')
            .replaceAll('<', '&lt;')
            .replaceAll('>', '&gt;')
            .replaceAll('"', '&quot;')
            .replaceAll("'", '&#039;');
    }

    function positionPopup() {
        const rect = searchInput.getBoundingClientRect();
        const viewH = window.innerHeight;
        const popH  = Math.min(420, viewH * 0.6);
        const spaceBelow = viewH - rect.bottom - 8;
        const spaceAbove = rect.top - 8;

        popup.style.left  = rect.left + 'px';
        popup.style.width = rect.width + 'px';

        if (spaceBelow >= 160 || spaceBelow >= spaceAbove) {
            popup.style.top    = (rect.bottom + 4) + 'px';
            popup.style.bottom = 'auto';
            popupBody.style.maxHeight = Math.min(340, spaceBelow - 80) + 'px';
        } else {
            popup.style.bottom = (viewH - rect.top + 4) + 'px';
            popup.style.top    = 'auto';
            popupBody.style.maxHeight = Math.min(340, spaceAbove - 80) + 'px';
        }
    }

    function openPopup() {
        positionPopup();
        popup.style.display = '';
        searchInput.setAttribute('aria-expanded', 'true');
        isOpen = true;
    }

    function closePopup() {
        popup.style.display = 'none';
        searchInput.setAttribute('aria-expanded', 'false');
        popupItems  = [];
        activeIndex = -1;
        isOpen      = false;
    }

    function setActiveRow(i) {
        const count = popupItems.length;
        if (!count) return;
        activeIndex = ((i % count) + count) % count;
        const rows = popupBody.querySelectorAll('.medicine-popup-row');
        rows.forEach((r, idx) => {
            r.classList.toggle('active', idx === activeIndex);
            if (idx === activeIndex) r.scrollIntoView({ block: 'nearest' });
        });
    }

    function stockBadge(qty) {
        const n = Number(qty || 0);
        if (n === 0) return `<span class="purchase-stock-badge out-of-stock">Out</span>`;
        let cls = '';
        if (n <= 5)  cls = 'low';
        else if (n <= 20) cls = 'medium';
        return `<span class="mpc-stock-badge ${cls}">${n}</span>`;
    }

    function renderLoading() {
        popupBody.innerHTML = `
            <div class="medicine-popup-loading">
                <div class="spinner"></div>
                <span>Searching…</span>
            </div>`;
        openPopup();
    }

    function renderEmpty() {
        popupBody.innerHTML = `
            <div class="medicine-popup-empty">
                <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
                    <circle cx="11" cy="11" r="8"/><path d="m21 21-4.35-4.35"/>
                </svg>
                No medicines found
            </div>`;
        openPopup();
    }

    function renderPopup(medicines) {
        popupBody.innerHTML = '';

        if (!medicines.length) { renderEmpty(); return; }

        const frag = document.createDocumentFragment();
        medicines.forEach((m) => {
            const isMaster = m.isMaster === true;
            const isLow = m.stockQuantity <= 5;
            const isOut = m.stockQuantity === 0;
            const stockBadgeHtml = isOut
                ? `<span class="purchase-stock-badge out-of-stock">Out</span>`
                : isLow
                    ? `<span class="purchase-stock-badge low-stock">⚠ ${m.stockQuantity}</span>`
                    : `<span class="purchase-stock-ok">${m.stockQuantity}</span>`;

            const row = document.createElement('div');
            row.className = 'medicine-popup-row' + (isMaster ? ' master-row' : '') + (isLow && !isOut ? ' low-stock-row' : '');
            row.setAttribute('role', 'option');
            row.innerHTML = `
                <div style="flex:1;min-width:0">
                    <div class="fw-semibold" style="white-space:nowrap;overflow:hidden;text-overflow:ellipsis;color:var(--rx-heading)">${escapeHtml(m.name)}</div>
                    ${m.genericName ? `<div style="font-size:0.78rem;opacity:0.65">${escapeHtml(m.genericName)}${m.formType ? ' · ' + escapeHtml(m.formType) : ''}</div>` : ''}
                    ${m.manufacturer ? `<div style="font-size:0.72rem;opacity:0.5">${escapeHtml(m.manufacturer)}</div>` : ''}
                    ${isMaster ? '<div style="font-size:0.65rem;color:#60A5FA;font-weight:600;margin-top:1px">Catalog · No stock</div>' : ''}
                </div>
                <div style="text-align:right;flex-shrink:0;padding-left:0.5rem">
                    ${isMaster ? '' : `<div style="font-size:0.8rem;opacity:0.75">Rs. ${Number(m.mrp || 0).toFixed(2)}</div>`}
                    <div>${isMaster ? '' : stockBadgeHtml}</div>
                </div>`;
            row.addEventListener('mousedown', e => {
                e.preventDefault();
                selectMedicine(m);
            });
            frag.appendChild(row);
        });

        popupBody.appendChild(frag);
        activeIndex = -1;
        setActiveRow(0);
        openPopup();
    }

    function selectMedicine(medicine) {
        if (!medicine) return;

        // Master catalog entries — redirect to add stock or show info
        if (medicine.isMaster === true) {
            const name = medicine.genericName || medicine.name;
            if (confirm(`"${name}" is in the master catalog but has no stock.\n\nGo to Purchase → New Purchase to add stock?`)) {
                window.location.href = '/Purchase/Create';
            }
            closePopup();
            searchInput.value = '';
            return;
        }

        saveRecent(medicine);
        addItem({
            id: Number(medicine.id),
            name: medicine.name,
            mrp: Number(medicine.mrp || 0),
            purchasePrice: Number(medicine.purchasePrice || 0),
            stockQuantity: Number(medicine.stockQuantity || 0),
            discountPercent: Number(medicine.discountPercent || 0)
        });
        closePopup();
        searchInput.value = '';
    }

    function selectActive() {
        if (activeIndex < 0 || activeIndex >= popupItems.length) return;
        selectMedicine(popupItems[activeIndex]);
    }

    function fetchAndRender(term) {
        const query = (term || '').trim();
        if (!query) { closePopup(); return; }

        fetchSeq++;
        const seq = fetchSeq;

        renderLoading();

        fetch(`?handler=Search&term=${encodeURIComponent(query)}`, {
            headers: { 'Accept': 'application/json' }
        })
            .then(r => { if (!r.ok) throw new Error('HTTP ' + r.status); return r.json(); })
            .then(results => {
                if (seq !== fetchSeq) return;
                popupItems = (results || []).slice(0, 10).map(m => ({
                    id: String(m.id ?? m.Id),
                    name: m.name ?? m.Name,
                    genericName: m.genericName ?? m.GenericName,
                    manufacturer: m.manufacturer ?? m.Manufacturer,
                    formType: m.formType ?? m.FormType,
                    mrp: Number(m.mrp ?? m.MRP ?? 0),
                    purchasePrice: Number(m.purchasePrice ?? m.PurchasePrice ?? 0),
                    stockQuantity: Number(m.stockQuantity ?? m.StockQuantity ?? 0),
                    discountPercent: Number(m.discountPercent ?? m.DiscountPercent ?? 0),
                    batch: m.batch ?? m.Batch,
                    isMaster: m.isMaster === true
                }));
                renderPopup(popupItems);
            })
            .catch(err => {
                console.error('Medicine search failed', err);
                if (seq !== fetchSeq) return;
                popupItems = [];
                renderEmpty();
            });
    }

    searchInput.addEventListener('input', () => {
        clearTimeout(debounceTimer);
        const q = searchInput.value.trim();
        if (!q) { debounceTimer = setTimeout(() => showRecent(), 80); return; }
        debounceTimer = setTimeout(() => fetchAndRender(q), 180);
    });

    searchInput.addEventListener('focus', () => {
        const q = searchInput.value.trim();
        if (q) fetchAndRender(q);
        else showRecent();
    });

    searchInput.addEventListener('keydown', e => {
        if (e.key === 'Escape') {
            e.preventDefault();
            closePopup();
            return;
        }
        if (!isOpen) {
            if (e.key === 'Enter') { e.preventDefault(); return; }

            // Quick-qty shortcut: 1–9 while popup is closed and input is empty
            if (/^[1-9]$/.test(e.key) && !e.ctrlKey && !e.altKey && !e.metaKey
                    && !searchInput.value && lastAddedTr && document.contains(lastAddedTr)) {
                e.preventDefault();
                const item = items.find(x => x.index === Number(lastAddedTr.dataset.index));
                const qtyInput = lastAddedTr.querySelector('.qty-input');
                if (item && qtyInput) {
                    const n = Math.min(parseInt(e.key), item.availableStock);
                    qtyInput.value = n;
                    item.quantity = n;
                    updateLineTotal(lastAddedTr, item);
                    recalcTotals();
                    qtyInput.classList.add('qty-flash');
                    setTimeout(() => qtyInput.classList.remove('qty-flash'), 400);
                }
            }
            return;
        }
        if (e.key === 'ArrowDown') {
            e.preventDefault();
            setActiveRow(activeIndex + 1);
            return;
        }
        if (e.key === 'ArrowUp') {
            e.preventDefault();
            setActiveRow(activeIndex - 1);
            return;
        }
        if (e.key === 'Enter') {
            e.preventDefault();
            selectActive();
        }
    });

    searchInput.addEventListener('blur', () => {
        setTimeout(() => {
            if (!popup.contains(document.activeElement)) closePopup();
        }, 150);
    });

    document.addEventListener('mousedown', e => {
        if (!searchInput.contains(e.target) && !popup.contains(e.target)) closePopup();
    });

    window.addEventListener('resize', () => { if (isOpen) positionPopup(); });
    window.addEventListener('scroll', () => { if (isOpen) positionPopup(); }, true);

    // ── Floating Calculator ─────────────────────────────────────
    const floatingCalc = document.getElementById('floatingCalc');
    let calcTarget = null;

    function showCalc(target) {
        calcTarget = target;
        const rect = target.getBoundingClientRect();
        floatingCalc.style.left = Math.max(4, rect.left) + 'px';
        floatingCalc.style.top = (rect.bottom + 4) + 'px';
        floatingCalc.style.display = '';
        // Keep within viewport
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
            if (active && (active.classList.contains('rate-input') || active.classList.contains('qty-input'))) {
                if (floatingCalc.style.display === 'none') {
                    showCalc(active);
                } else {
                    hideCalc();
                }
            }
        }
    });

    document.addEventListener('contextmenu', e => {
        const input = e.target.closest('.rate-input, .qty-input');
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
            if (calcTarget.classList.contains('rate-input') && tr) {
                tr.querySelector('.qty-input').focus();
            } else if (calcTarget.classList.contains('qty-input') && tr) {
                tr.querySelector('.disc-percent-input').focus();
            } else if (calcTarget.classList.contains('disc-percent-input')) {
                searchInput.focus();
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
    });

    document.addEventListener('mousedown', e => {
        if (floatingCalc.style.display !== 'none' && !floatingCalc.contains(e.target) && e.target !== calcTarget) {
            hideCalc();
        }
    });
})();
