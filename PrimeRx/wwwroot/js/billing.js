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

        const totalDiscount = itemDiscount;
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
    updateCustomerPhoneRequirement();

    // ── Inline suggestion dropdown ──────────────────────────────────────────
    const searchInput = document.getElementById('medicineSearchText');
    const dropdown = document.getElementById('medicineSearchDropdown');
    const dropdownBody = document.getElementById('medicineDropdownBody');

    let dropdownItems = [];
    let activeIndex = -1;
    let debounceTimer = null;
    let fetchSeq = 0;

    function escapeHtml(s) {
        return String(s ?? '')
            .replaceAll('&', '&amp;')
            .replaceAll('<', '&lt;')
            .replaceAll('>', '&gt;')
            .replaceAll('"', '&quot;')
            .replaceAll("'", '&#039;');
    }

    function closeDropdown() {
        dropdown.style.display = 'none';
        dropdownItems = [];
        activeIndex = -1;
    }

    function openDropdown() {
        dropdown.style.display = '';
    }

    function setActiveRow(i) {
        activeIndex = i;
        const rows = dropdownBody.querySelectorAll('.medicine-dropdown-row');
        rows.forEach((r, idx) => {
            r.classList.toggle('active', idx === i);
            if (idx === i) r.scrollIntoView({ block: 'nearest' });
        });
    }

    function renderDropdown(medicines) {
        dropdownBody.innerHTML = '';

        if (!medicines.length) {
            dropdownBody.innerHTML = '<div class="medicine-dropdown-empty">No medicines found</div>';
            openDropdown();
            return;
        }

        const frag = document.createDocumentFragment();
        medicines.forEach((m, i) => {
            const row = document.createElement('div');
            row.className = 'medicine-dropdown-row';
            row.innerHTML = `
                <span class="med-col-name">
                    <span class="med-name">${escapeHtml(m.name)}</span>
                    ${m.genericName ? `<span class="med-generic">${escapeHtml(m.genericName)}</span>` : ''}
                </span>
                <span class="med-col-batch">${escapeHtml(m.batch || '—')}</span>
                <span class="med-col-stock">${Number(m.stockQuantity || 0)}</span>
                <span class="med-col-rate">Rs. ${Number(m.mrp || 0).toFixed(2)}</span>
            `;
            row.addEventListener('mousedown', e => {
                e.preventDefault();
                selectMedicine(m);
            });
            frag.appendChild(row);
        });

        dropdownBody.appendChild(frag);
        activeIndex = 0;
        setActiveRow(0);
        openDropdown();
    }

    function selectMedicine(medicine) {
        if (!medicine) return;
        addItem({
            id: Number(medicine.id),
            name: medicine.name,
            mrp: Number(medicine.mrp || 0),
            stockQuantity: Number(medicine.stockQuantity || 0),
            discountPercent: Number(medicine.discountPercent || 0)
        });
        closeDropdown();
        searchInput.value = '';
        searchInput.focus();
    }

    function selectActive() {
        if (activeIndex < 0 || activeIndex >= dropdownItems.length) return;
        selectMedicine(dropdownItems[activeIndex]);
    }

    function fetchAndRender(term) {
        const query = (term || '').trim();
        if (!query) { closeDropdown(); return; }

        fetchSeq++;
        const seq = fetchSeq;

        fetch(`?handler=Search&term=${encodeURIComponent(query)}`, {
            headers: { 'Accept': 'application/json' }
        })
            .then(r => { if (!r.ok) throw new Error('HTTP ' + r.status); return r.json(); })
            .then(results => {
                if (seq !== fetchSeq) return;
                dropdownItems = (results || []).slice(0, 10).map(m => ({
                    id: String(m.id ?? m.Id),
                    name: m.name ?? m.Name,
                    genericName: m.genericName ?? m.GenericName,
                    mrp: Number(m.mrp ?? m.MRP ?? 0),
                    stockQuantity: Number(m.stockQuantity ?? m.StockQuantity ?? 0),
                    discountPercent: Number(m.discountPercent ?? m.DiscountPercent ?? 0),
                    batch: m.batch ?? m.Batch
                }));
                renderDropdown(dropdownItems);
            })
            .catch(() => {
                if (seq !== fetchSeq) return;
                dropdownItems = [];
                renderDropdown([]);
            });
    }

    searchInput.addEventListener('input', () => {
        clearTimeout(debounceTimer);
        debounceTimer = setTimeout(() => fetchAndRender(searchInput.value), 180);
    });

    searchInput.addEventListener('focus', () => {
        if (searchInput.value.trim()) fetchAndRender(searchInput.value);
    });

    searchInput.addEventListener('keydown', e => {
        if (e.key === 'Escape') {
            e.preventDefault();
            closeDropdown();
            return;
        }
        if (dropdown.style.display === 'none') {
            if (e.key === 'Enter') e.preventDefault();
            return;
        }
        if (e.key === 'ArrowDown') {
            e.preventDefault();
            if (dropdownItems.length) setActiveRow(Math.min(dropdownItems.length - 1, activeIndex + 1));
            return;
        }
        if (e.key === 'ArrowUp') {
            e.preventDefault();
            if (dropdownItems.length) setActiveRow(Math.max(0, activeIndex - 1));
            return;
        }
        if (e.key === 'Enter') {
            e.preventDefault();
            selectActive();
        }
    });

    searchInput.addEventListener('blur', () => {
        setTimeout(() => {
            if (!dropdown.contains(document.activeElement)) closeDropdown();
        }, 150);
    });

    document.addEventListener('mousedown', e => {
        if (!searchInput.contains(e.target) && !dropdown.contains(e.target)) closeDropdown();
    });
})();
