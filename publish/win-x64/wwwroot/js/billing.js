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
            subtotal += item.rate * item.quantity;
            itemDiscount += item.discountPerItem * item.quantity;
        });

        const billDiscount = parseFloat(document.getElementById('billDiscount').value) || 0;
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
            discountPerItem: i.discountPerItem
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
            discountPerItem: 0
        });

        renderRow(items[items.length - 1]);
        recalcTotals();
    }

    function renderRow(item) {
        const tr = document.createElement('tr');
        tr.dataset.index = item.index;
        tr.innerHTML = `
            <td>${item.medicineName}<br><small class="text-muted">Stock: ${item.availableStock}</small></td>
            <td><input type="number" class="form-control form-control-sm rate-input" value="${item.rate}" step="0.01" min="0"></td>
            <td><input type="number" class="form-control form-control-sm qty-input" value="${item.quantity}" min="1" max="${item.availableStock}"></td>
            <td><input type="number" class="form-control form-control-sm disc-input" value="${item.discountPerItem}" step="0.01" min="0"></td>
            <td class="line-total">${formatMoney(item.rate * item.quantity - item.discountPerItem * item.quantity)}</td>
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

        tr.querySelector('.disc-input').addEventListener('input', e => {
            item.discountPerItem = parseFloat(e.target.value) || 0;
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
        tr.querySelector('.line-total').textContent = formatMoney(
            item.rate * item.quantity - item.discountPerItem * item.quantity
        );
    }

    document.getElementById('billDiscount')?.addEventListener('input', recalcTotals);

    new TomSelect('#medicineSearch', {
        valueField: 'id',
        labelField: 'text',
        searchField: ['name', 'genericName'],
        placeholder: 'Type medicine name...',
        load: function (query, callback) {
            if (!query.length) return callback();
            fetch(`?handler=Search&term=${encodeURIComponent(query)}`)
                .then(r => r.json())
                .then(data => callback(data))
                .catch(() => callback());
        },
        onChange: function (value) {
            if (!value) return;
            const option = this.options[value];
            if (option) {
                addItem({
                    id: parseInt(option.id),
                    name: option.name,
                    mrp: parseFloat(option.mrp),
                    stockQuantity: parseInt(option.stockQuantity)
                });
            }
            this.clear(true);
            this.clearOptions();
        }
    });
})();
