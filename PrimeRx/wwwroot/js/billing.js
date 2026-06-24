// PrimeRx - client-side bill builder
(function () {
    const items = [];
    let itemIndex = 0;

    const itemsBody = document.getElementById('itemsBody');
    const emptyRow = document.getElementById('emptyRow');
    const itemsJson = document.getElementById('itemsJson');
    const generateBtn = document.getElementById('generateBtn');

    function formatMoney(n) {
        return Number(n).toFixed(2);
    }

    function recalcTotals() {
        let subtotal = 0;
        let itemDiscount = 0;

        items.forEach(item => {
            const gross = item.rate * item.quantity;
            const discountAmount = gross * (item.discountPercent / 100);
            item.discountAmount = discountAmount;
            subtotal += gross;
            itemDiscount += discountAmount;
        });

        // Overall (bill-level) DiscountAmount input is removed from UI.
        // Keep only per-item discounts here.
        const billDiscount = 0;
        const totalDiscount = itemDiscount + billDiscount;
        const total = subtotal - totalDiscount;


        document.getElementById('subtotalDisplay').textContent = formatMoney(subtotal);
        document.getElementById('discountDisplay').textContent = formatMoney(totalDiscount);
        document.getElementById('totalDisplay').textContent = formatMoney(Math.max(0, total));

        itemsJson.value = JSON.stringify(items.map(i => ({
            medicineId: i.medicineId,
            medicineName: i.medicineName,
            rate: i.rate,
            quantity: i.quantity,
            availableStock: i.availableStock,
            discountPercent: i.discountPercent,
            discountAmount: i.discountAmount
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
            quantity: 1,
            availableStock: medicine.stockQuantity,
            discountPercent: medicine.discountPercent || 0,
            discountAmount: 0
        });

        renderRow(items[items.length - 1]);
        recalcTotals();
    }

    function renderRow(item) {
        const tr = document.createElement('tr');
        tr.dataset.index = item.index;
        tr.innerHTML = `
            <td>${item.medicineName}<br><small>Stock: ${item.availableStock}</small></td>
            <td><input type="number" class="form-control form-control-sm rate-input" value="${item.rate}" step="0.01" min="0"></td>
            <td><input type="number" class="form-control form-control-sm qty-input" value="${item.quantity}" min="1" max="${item.availableStock}"></td>
            <td><input type="number" class="form-control form-control-sm disc-percent-input" value="${item.discountPercent}" step="0.1" min="0" max="100"></td>
            <td class="disc-amount">${formatMoney(item.discountAmount)}</td>
            <td class="line-total">${formatMoney(item.rate * item.quantity - item.discountAmount)}</td>
            <td><button type="button" class="btn btn-sm btn-outline-danger remove-btn">&times;</button></td>
        `;

        tr.querySelector('.rate-input').addEventListener('input', e => {
            item.rate = parseFloat(e.target.value) || 0;
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
            updateLineTotal(tr, item);
            recalcTotals();
        });

        tr.querySelector('.remove-btn').addEventListener('click', () => {
            const i = items.findIndex(x => x.index === item.index);
            if (i >= 0) items.splice(i, 1);
            tr.remove();
            recalcTotals();
        });

        itemsBody.appendChild(tr);
    }

    function updateLineTotal(tr, item) {
        const gross = item.rate * item.quantity;
        const discountAmount = gross * (item.discountPercent / 100);
        item.discountAmount = discountAmount;
        tr.querySelector('.disc-amount').textContent = formatMoney(discountAmount);
        tr.querySelector('.line-total').textContent = formatMoney(gross - discountAmount);
    }

    document.getElementById('billDiscount')?.addEventListener('input', recalcTotals);

    // Conditional validation for CustomerPhone field when payment is Due
    const paymentMethod = document.getElementById('paymentMethod');
    const customerPhone = document.getElementById('customerPhone');
    const customerPhoneHint = document.getElementById('customerPhoneHint');

    function updateCustomerPhoneRequirement() {
        if (paymentMethod.value === 'Due') {
            customerPhone.setAttribute('required', 'required');
            customerPhoneHint.textContent = 'Required when payment is Due';
            customerPhoneHint.classList.add('text-danger');
            customerPhoneHint.classList.remove('text-muted');
        } else {
            customerPhone.removeAttribute('required');
            customerPhoneHint.textContent = 'Required when payment is Due';
            customerPhoneHint.classList.remove('text-danger');
            customerPhoneHint.classList.add('text-muted');
        }
    }

    paymentMethod?.addEventListener('change', updateCustomerPhoneRequirement);

    // Initialize on page load
    updateCustomerPhoneRequirement();

    // Pharmacy-style floating suggestions popup (replaces TomSelect)
    const searchSelect = document.getElementById('medicineSearch');
    const popup = document.createElement('div');
    popup.id = 'medicineSearchPopup';
    popup.className = 'medicine-popup';
    popup.style.display = 'none';

    popup.innerHTML = `
      <div class="medicine-popup-header">Search Results</div>
      <div class="medicine-popup-body">
        <table class="medicine-popup-table">
          <thead>
            <tr>
              <th style="width:52%">NAME</th>
              <th style="width:16%">BATCH</th>
              <th style="width:14%">STOCK</th>
              <th style="width:18%">RATE</th>
            </tr>
          </thead>
          <tbody id="medicinePopupTbody"></tbody>
        </table>
      </div>
      <div class="medicine-popup-footer">
        <span>↑ ↓ Navigate</span>
        <span>Enter Select</span>
        <span>Esc Close</span>
      </div>
    `;

    document.body.appendChild(popup);

    const popupTbody = popup.querySelector('#medicinePopupTbody');

    let popupItems = [];
    let activeIndex = -1;
    let lastQuery = '';
    let fetchSeq = 0;
    let debounceTimer = null;

    function escapeHtml(s) {
        return String(s ?? '')
            .replaceAll('&', '&amp;')
            .replaceAll('<', '<')
            .replaceAll('>', '>')
            .replaceAll('"', '"')
            .replaceAll("'", '&#039;');
    }


    // This page renders #medicineSearch as a <select>. For POS-like typing, we replace it with a text input.
    function setInputProxyFromSelection() {
        // Keep compatibility: allow user to type into SELECT? Not possible.
        // So we ensure there is an actual text input inside the same panel.
        // If not present, create it.
        let input = searchSelect.parentElement.querySelector('input#medicineSearchText');
        if (!input) {
            input = document.createElement('input');
            input.id = 'medicineSearchText';
            input.type = 'text';
            input.className = 'form-control medicine-search-input';
            input.placeholder = 'Type to search medicines...';
            searchSelect.replaceWith(input);

            // Re-bind searchSelect reference to keep addItem logic.
            // We'll keep searchSelect removed; backend search is term-based.
            // Update popup positioning triggers from input.
            attachInputEvents(input);
        }
    }

    function positionPopup() {
        const anchor = popupAnchorInput();
        if (!anchor) return;
        const rect = anchor.getBoundingClientRect();

        popup.style.left = `${Math.max(8, rect.left)}px`;
        popup.style.top = `${rect.bottom + 6}px`;
        popup.style.width = `${rect.width}px`;
    }

    function popupAnchorInput() {
        return document.getElementById('medicineSearchText');
    }

    function renderPopup() {
        popupTbody.innerHTML = '';
        popupItems = popupItems || [];


        if (!popupItems.length) {
            const tr = document.createElement('tr');
            tr.className = 'medicine-popup-row';
            tr.innerHTML = `<td colspan="4" class="medicine-popup-empty">No medicines found</td>`;
            popupTbody.appendChild(tr);
            activeIndex = -1;
            return;
        }

        const max = popupItems.length;
        const frag = document.createDocumentFragment();
        for (let i = 0; i < max; i++) {
            const m = popupItems[i];
            const tr = document.createElement('tr');
            tr.className = 'medicine-popup-row';
            tr.dataset.index = String(i);

            // Batch not currently returned by Search endpoint; display B-? fallback.
            const batch = m.batch ?? m.Batch ?? m.batchCode ?? '—';

            tr.innerHTML = `
              <td>
                <div class="med-name">${escapeHtml(m.name)}</div>
                ${m.genericName ? `<div class="med-generic">${escapeHtml(m.genericName)}</div>` : ''}
              </td>
              <td>${escapeHtml(batch)}</td>
              <td>${Number(m.stockQuantity ?? m.stockQuantity ?? 0)}</td>
              <td>Rs. ${Number(m.mrp ?? m.mrp ?? 0).toFixed(2)}</td>
            `;

            tr.addEventListener('click', () => {
                setActiveRow(i);
                selectActive();
            });
            tr.addEventListener('dblclick', () => {
                setActiveRow(i);
                selectActive();
            });

            frag.appendChild(tr);
        }
        popupTbody.appendChild(frag);

        // clamp activeIndex
        if (activeIndex >= popupItems.length) activeIndex = popupItems.length - 1;
        setActiveRow(activeIndex);
    }

    function setActiveRow(i) {
        activeIndex = i;
        const rows = popupTbody.querySelectorAll('.medicine-popup-row');
        rows.forEach(r => r.classList.remove('active'));
        if (i >= 0 && rows[i]) rows[i].classList.add('active');
    }

    function closePopup() {
        popup.style.display = 'none';
        popupItems = [];
        activeIndex = -1;
        lastQuery = '';
    }

    function openPopup() {
        popup.style.display = '';
        positionPopup();
    }


    function selectMedicine(medicine) {
        if (!medicine) return;
        addItem({
            id: Number(medicine.id ?? medicine.Id),
            name: medicine.name ?? medicine.Name ?? medicine.text,
            mrp: Number(medicine.mrp ?? medicine.MRP ?? 0),
            stockQuantity: Number(medicine.stockQuantity ?? medicine.StockQuantity ?? 0),
            discountPercent: Number(medicine.discountPercent ?? medicine.DiscountPercent ?? 0)
        });
        closePopup();
        const input = popupAnchorInput();
        if (input) {
            input.value = '';
            input.focus();
        }
    }

    function selectActive() {
        if (activeIndex < 0 || activeIndex >= popupItems.length) return;
        selectMedicine(popupItems[activeIndex]);
    }

    function search(term) {
        const query = (term ?? '').trim();
        if (!query) {
            closePopup();
            return;
        }
        lastQuery = query;

        fetchSeq++;
        const seq = fetchSeq;

        fetch(`?handler=Search&term=${encodeURIComponent(query)}`, {
            headers: { 'Accept': 'application/json' }
        })
            .then(r => {
                if (!r.ok) throw new Error('HTTP ' + r.status);
                return r.json();
            })
            .then(results => {
                if (seq !== fetchSeq) return;
                const normalized = (results || []).map(m => ({
                    id: String(m.id ?? m.Id),
                    name: m.name ?? m.Name,
                    genericName: m.genericName ?? m.GenericName,
                    mrp: Number(m.mrp ?? m.MRP ?? 0),
                    stockQuantity: Number(m.stockQuantity ?? m.StockQuantity ?? 0),
                    discountPercent: Number(m.discountPercent ?? m.DiscountPercent ?? 0),
                    batch: m.batch ?? m.Batch
                }));

                popupItems = normalized.slice(0, 10);
                activeIndex = 0;
                renderPopup();
                openPopup();
            })
            .catch(() => {
                if (seq !== fetchSeq) return;
                popupItems = [];
                activeIndex = -1;
                renderPopup();
                openPopup();
            });
    }

    function attachInputEvents(input) {
        // Close on outside click
        document.addEventListener('mousedown', (e) => {
            if (popup.style.display === 'none') return;
            if (!popup.contains(e.target) && e.target !== input) closePopup();
        });

        input.addEventListener('input', () => {
            const val = input.value;
            if (debounceTimer) clearTimeout(debounceTimer);
            debounceTimer = setTimeout(() => {
                search(val);
            }, 180);
        });

        input.addEventListener('focus', () => {
            if (input.value.trim()) search(input.value);
        });

        input.addEventListener('keydown', (e) => {
            if (e.key === 'Escape') {
                e.preventDefault();
                closePopup();
                return;
            }

            if (popup.style.display === 'none') {
                if (e.key === 'Enter') {
                    // ignore
                    e.preventDefault();
                }
                return;
            }

            if (e.key === 'ArrowDown') {
                e.preventDefault();
                if (popupItems.length) setActiveRow(Math.min(popupItems.length - 1, activeIndex + 1));
                return;
            }

            if (e.key === 'ArrowUp') {
                e.preventDefault();
                if (popupItems.length) setActiveRow(Math.max(0, activeIndex - 1));
                return;
            }

            if (e.key === 'Enter') {
                e.preventDefault();
                selectActive();
                return;
            }
        });

        window.addEventListener('scroll', () => {
            if (popup.style.display !== 'none') positionPopup();
        }, { passive: true });

        window.addEventListener('resize', () => {
            if (popup.style.display !== 'none') positionPopup();
        });

        // If user tabs away, close.
        input.addEventListener('blur', () => {
            // Delay so click can register
            setTimeout(() => {
                if (!popup.contains(document.activeElement)) {
                    closePopup();
                }
            }, 150);
        });
    }

    // Initialize
    setInputProxyFromSelection();

    // If input proxy already exists (e.g. HMR), attach events.
    const existing = popupAnchorInput();
    if (existing) attachInputEvents(existing);
})();
