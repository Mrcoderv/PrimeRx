// PrimeRx — Return to Supplier Modal
(function () {
    const SAMPLE_MEDICINES = [
        { name: 'Amoxicillin 500mg Capsules', batch: 'B2409-AMX', qty: 150, price: 28.50 },
        { name: 'Ciprofloxacin 250mg Tablets', batch: 'B2410-CIP', qty: 200, price: 18.00 },
        { name: 'Metformin 500mg Tablets', batch: 'B2408-MET', qty: 300, price: 12.00 },
        { name: 'Omeprazole 20mg Capsules', batch: 'B2411-OME', qty: 180, price: 22.00 },
        { name: 'Paracetamol 650mg Tablets', batch: 'B2407-PCM', qty: 500, price: 8.50 },
    ];

    let returnItems = [];
    let isOpen = false;

    const supplierName = document.getElementById('returnModalSupplierName');

    function openModal(supplier) {
        if (supplierName) supplierName.textContent = supplier;

        returnItems = [];
        renderTable();
        resetTotals();

        const modal = document.getElementById('returnToSupplierModal');
        if (modal) {
            const bsModal = new bootstrap.Modal(modal, { backdrop: 'static' });
            bsModal.show();
            isOpen = true;
        }
    }

    function closeModal() {
        const modal = document.getElementById('returnToSupplierModal');
        if (modal) {
            const bsModal = bootstrap.Modal.getInstance(modal);
            if (bsModal) bsModal.hide();
        }
        isOpen = false;
    }

    function esc(s) {
        return String(s ?? '').replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;').replace(/'/g, '&#039;');
    }

    function addReturnItem(med) {
        returnItems.push({
            id: Date.now() + Math.random(),
            medicineName: med.name,
            batchNumber: med.batch,
            batchQty: med.qty,
            returnQty: 1,
            adjustedPrice: med.price,
            returnCost: med.price,
            reason: 'Expired'
        });
        renderTable();
        updateTotals();
    }

    function removeReturnItem(id) {
        returnItems = returnItems.filter(i => i.id !== id);
        renderTable();
        updateTotals();
    }

    function renderTable() {
        const tbody = document.getElementById('returnItemsBody');
        const emptyRow = document.getElementById('returnEmptyRow');
        if (!tbody) return;

        if (!returnItems.length) {
            tbody.innerHTML = '';
            if (emptyRow) emptyRow.style.display = '';
            updateTotals();
            return;
        }

        if (emptyRow) emptyRow.style.display = 'none';

        const reasons = ['Expired', 'Damaged', 'Wrong Supply', 'Product Recall', 'Overstock', 'Other'];

        tbody.innerHTML = returnItems.map(item => `
            <tr data-id="${item.id}">
                <td><span class="medicine-name-cell">${esc(item.medicineName)}</span></td>
                <td><span class="batch-cell">${esc(item.batchNumber)}</span></td>
                <td class="qty-batch-cell">${item.batchQty}</td>
                <td><input type="number" class="return-input return-qty-input" value="${item.returnQty}" min="1" max="${item.batchQty}" step="1" data-id="${item.id}"></td>
                <td><input type="number" class="return-input return-price-input" value="${item.adjustedPrice.toFixed(2)}" min="0" step="0.01" data-id="${item.id}"></td>
                <td class="return-cost-cell return-cost-display">Rs. ${item.returnCost.toFixed(2)}</td>
                <td>
                    <select class="return-reason-select" data-id="${item.id}">
                        ${reasons.map(r => `<option value="${r}"${r === item.reason ? ' selected' : ''}>${r}</option>`).join('')}
                    </select>
                </td>
                <td>
                    <button type="button" class="btn-remove-return-item" data-id="${item.id}" title="Remove item">
                        <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round"><line x1="18" y1="6" x2="6" y2="18"/><line x1="6" y1="6" x2="18" y2="18"/></svg>
                    </button>
                </td>
            </tr>
        `).join('');

        // Attach event listeners
        tbody.querySelectorAll('.return-qty-input').forEach(inp => {
            inp.addEventListener('input', function () {
                const item = returnItems.find(i => i.id == this.dataset.id);
                if (!item) return;
                let qty = parseInt(this.value) || 0;
                if (qty < 1) qty = 1;
                if (qty > item.batchQty) qty = item.batchQty;
                this.value = qty;
                item.returnQty = qty;
                item.returnCost = qty * item.adjustedPrice;
                const costDisplay = this.closest('tr')?.querySelector('.return-cost-display');
                if (costDisplay) costDisplay.textContent = 'Rs. ' + item.returnCost.toFixed(2);
                updateTotals();
            });
        });

        tbody.querySelectorAll('.return-price-input').forEach(inp => {
            inp.addEventListener('input', function () {
                const item = returnItems.find(i => i.id == this.dataset.id);
                if (!item) return;
                let price = parseFloat(this.value) || 0;
                if (price < 0) price = 0;
                item.adjustedPrice = price;
                item.returnCost = item.returnQty * price;
                const costDisplay = this.closest('tr')?.querySelector('.return-cost-display');
                if (costDisplay) costDisplay.textContent = 'Rs. ' + item.returnCost.toFixed(2);
                updateTotals();
            });
        });

        tbody.querySelectorAll('.return-reason-select').forEach(sel => {
            sel.addEventListener('change', function () {
                const item = returnItems.find(i => i.id == this.dataset.id);
                if (item) item.reason = this.value;
            });
        });

        tbody.querySelectorAll('.btn-remove-return-item').forEach(btn => {
            btn.addEventListener('click', function () {
                removeReturnItem(parseFloat(this.dataset.id));
            });
        });
    }

    function resetTotals() {
        const subtotalEl = document.getElementById('returnSubtotal');
        const adjDisplay = document.getElementById('returnAdjDisplay');
        const adjInput = document.getElementById('returnAdjInput');
        const finalEl = document.getElementById('returnFinalAmount');
        if (subtotalEl) subtotalEl.textContent = 'Rs. 0.00';
        if (adjDisplay) adjDisplay.textContent = 'Rs. 0.00';
        if (adjInput) adjInput.value = '0.00';
        if (finalEl) finalEl.textContent = 'Rs. 0.00';
        updateConfirmBtn();
    }

    function updateTotals() {
        const subtotal = returnItems.reduce((s, i) => s + i.returnCost, 0);
        const adjInput = document.getElementById('returnAdjInput');
        const adjSign = document.getElementById('returnAdjSign');
        const adjValue = parseFloat(adjInput?.value || '0');
        const isNegative = adjSign?.dataset.sign === 'minus';
        const adjustment = isNegative ? -adjValue : adjValue;
        const netAmount = subtotal + adjustment;

        const subtotalEl = document.getElementById('returnSubtotal');
        const adjDisplay = document.getElementById('returnAdjDisplay');
        const finalEl = document.getElementById('returnFinalAmount');
        const batchSubtotalEl = document.getElementById('returnBatchSubtotal');
        const itemCountEl = document.getElementById('returnItemCount');
        const batchCountEl = document.getElementById('returnBatchCount');

        if (subtotalEl) subtotalEl.textContent = 'Rs. ' + subtotal.toFixed(2);
        if (batchSubtotalEl) batchSubtotalEl.textContent = 'Rs. ' + subtotal.toFixed(2);
        if (itemCountEl) itemCountEl.textContent = returnItems.length;
        if (batchCountEl) batchCountEl.textContent = returnItems.filter(i => i.batchNumber).length;
        if (adjDisplay) {
            adjDisplay.textContent = (isNegative ? '- ' : '+ ') + 'Rs. ' + adjValue.toFixed(2);
            adjDisplay.className = 'adj-display' + (isNegative ? ' adj-negative' : ' adj-positive');
        }
        if (finalEl) finalEl.textContent = 'Rs. ' + Math.max(0, netAmount).toFixed(2);

        updateConfirmBtn();
    }

    function updateConfirmBtn() {
        const btn = document.getElementById('returnConfirmBtn');
        if (btn) btn.disabled = returnItems.length === 0;
    }

    // ── Add More Medicine search ─────────────────────────────
    function toggleAddMore(open) {
        const container = document.getElementById('addMoreSearchContainer');
        const input = document.getElementById('addMoreSearchInput');
        const dropdown = document.getElementById('addMoreDropdown');
        if (!container) return;

        if (open) {
            container.classList.add('open');
            setTimeout(() => input?.focus(), 100);
        } else {
            container.classList.remove('open');
            if (dropdown) dropdown.innerHTML = '';
            if (input) input.value = '';
        }
    }

    function filterMedicines(term) {
        const dropdown = document.getElementById('addMoreDropdown');
        if (!dropdown) return;

        const q = (term || '').trim().toLowerCase();
        if (!q) { dropdown.innerHTML = ''; return; }

        const filtered = SAMPLE_MEDICINES.filter(m =>
            m.name.toLowerCase().includes(q) &&
            !returnItems.some(i => i.medicineName === m.name)
        );

        if (!filtered.length) {
            dropdown.innerHTML = '<div class="add-more-dropdown-item" style="opacity:0.5;cursor:default">No matches found</div>';
            return;
        }

        filtered.sort((a, b) => a.name.localeCompare(b.name));

        dropdown.innerHTML = filtered.map(m =>
            `<div class="add-more-dropdown-item" data-name="${esc(m.name)}" data-batch="${esc(m.batch)}" data-qty="${m.qty}" data-price="${m.price}">
                <span class="amd-name">${esc(m.name)}</span>
                <span class="amd-meta">Batch: ${esc(m.batch)} · Stock: ${m.qty} · Rs.${m.price.toFixed(2)}</span>
            </div>`
        ).join('');

        dropdown.querySelectorAll('.add-more-dropdown-item').forEach(el => {
            el.addEventListener('mousedown', function (e) {
                e.preventDefault();
                addReturnItem({
                    name: this.dataset.name,
                    batch: this.dataset.batch,
                    qty: parseInt(this.dataset.qty),
                    price: parseFloat(this.dataset.price)
                });
                toggleAddMore(false);
            });
        });
    }

    // ── Adjustment controls ──────────────────────────────────
    function toggleAdjSign() {
        const sign = document.getElementById('returnAdjSign');
        if (!sign) return;
        const isMinus = sign.dataset.sign === 'minus';
        sign.dataset.sign = isMinus ? 'plus' : 'minus';
        sign.innerHTML = isMinus
            ? '<svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="3" stroke-linecap="round"><line x1="5" y1="12" x2="19" y2="12"/><line x1="12" y1="5" x2="12" y2="19"/></svg>'
            : '<svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="3" stroke-linecap="round"><line x1="5" y1="12" x2="19" y2="12"/></svg>';
        sign.className = 'adj-sign-btn' + (isMinus ? ' adj-positive' : '');
        updateTotals();
    }

    // ── Expose to global scope ───────────────────────────────
    window.ReturnModal = {
        open: openModal,
        close: closeModal,
        toggleAddMore: toggleAddMore,
        filterMedicines: filterMedicines,
        toggleAdjSign: toggleAdjSign,
        updateTotals: updateTotals,
        addReturnItem: addReturnItem,
    };

    // ── Bind adjustment input ────────────────────────────────
    document.addEventListener('DOMContentLoaded', function () {
        const adjInput = document.getElementById('returnAdjInput');
        if (adjInput) {
            adjInput.addEventListener('input', updateTotals);
        }
    });
})();
