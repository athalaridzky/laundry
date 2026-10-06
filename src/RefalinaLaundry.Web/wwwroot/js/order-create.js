(() => {
  'use strict';

  const form = document.querySelector('[data-order-form]');
  if (!form) return;

  const services = JSON.parse(document.getElementById('services-data')?.textContent || '[]');
  const initialItems = JSON.parse(document.getElementById('order-items-data')?.textContent || '[]');
  const rows = document.querySelector('[data-order-items]');
  const totalOutput = document.querySelector('[data-order-total]');
  const estimateOutput = document.querySelector('[data-order-estimate]');
  const itemCount = document.querySelector('[data-item-count]');
  const paymentAmount = document.querySelector('[data-payment-amount]');
  const paymentMethod = document.querySelector('[data-payment-method]');
  const cashTendered = document.querySelector('[data-cash-tendered]');
  const cashField = document.querySelector('[data-cash-field]');
  const referenceField = document.querySelector('[data-reference-field]');
  const changeOutput = document.querySelector('[data-change]');
  const rupiah = new Intl.NumberFormat('id-ID', { style: 'currency', currency: 'IDR', maximumFractionDigits: 0 });

  const numberValue = value => {
    const normalized = String(value ?? '').trim().replace(/\.(?=\d{3}(?:\D|$))/g, '').replace(',', '.');
    const parsed = Number.parseFloat(normalized);
    return Number.isFinite(parsed) ? parsed : 0;
  };

  const serviceById = id => services.find(service => Number(service.id) === Number(id));
  const formatDate = date => new Intl.DateTimeFormat('id-ID', { day: 'numeric', month: 'short', year: 'numeric', hour: '2-digit', minute: '2-digit' }).format(date).replace('.', ':');

  const serviceOptions = selectedId => [
    '<option value="">Pilih layanan</option>',
    ...services.map(service => `<option value="${service.id}" ${Number(selectedId) === Number(service.id) ? 'selected' : ''}>${service.name}</option>`),
  ].join('');

  const addRow = (item = {}) => {
    const index = rows.children.length;
    const row = document.createElement('tr');
    row.innerHTML = `
      <td><select name="Form.Items[${index}].ServiceId" aria-label="Layanan" required>${serviceOptions(item.serviceId)}</select><small data-unit-label></small></td>
      <td><div class="quantity-input"><input name="Form.Items[${index}].Quantity" value="${item.quantity || ''}" inputmode="decimal" aria-label="Berat atau jumlah" required data-quantity /><span data-unit>kg</span></div></td>
      <td class="money" data-price>Rp 0</td>
      <td class="money" data-subtotal>Rp 0</td>
      <td><button type="button" class="remove-row" aria-label="Hapus layanan" data-remove-row>×</button></td>`;
    rows.appendChild(row);
    const select = row.querySelector('select');
    const quantity = row.querySelector('[data-quantity]');
    select.addEventListener('change', () => { syncRow(row); recalculate(); });
    quantity.addEventListener('input', recalculate);
    row.querySelector('[data-remove-row]').addEventListener('click', () => {
      if (rows.children.length === 1) {
        select.value = '';
        quantity.value = '';
        syncRow(row);
      } else {
        row.remove();
        reindexRows();
      }
      recalculate();
    });
    syncRow(row);
  };

  const syncRow = row => {
    const selected = serviceById(row.querySelector('select').value);
    const quantity = row.querySelector('[data-quantity]');
    const unit = row.querySelector('[data-unit]');
    const label = row.querySelector('[data-unit-label]');
    const perKilo = selected?.category === 0 || selected?.category === 'PerKilo';
    quantity.step = perKilo ? '0.1' : '1';
    quantity.min = perKilo ? '0.1' : '1';
    unit.textContent = perKilo ? 'kg' : 'pcs';
    label.textContent = selected ? `${perKilo ? 'Per kilogram' : 'Per satuan'} · ${selected.durationDays === 0 ? 'selesai hari ini' : selected.durationDays ? `${selected.durationDays} hari` : 'tanpa estimasi'}` : '';
    row.querySelector('[data-price]').textContent = selected ? rupiah.format(selected.unitPrice) : rupiah.format(0);
  };

  const reindexRows = () => {
    [...rows.children].forEach((row, index) => {
      row.querySelector('select').name = `Form.Items[${index}].ServiceId`;
      row.querySelector('[data-quantity]').name = `Form.Items[${index}].Quantity`;
    });
  };

  const recalculate = () => {
    let total = 0;
    let count = 0;
    const durations = [];
    [...rows.children].forEach(row => {
      const selected = serviceById(row.querySelector('select').value);
      const quantity = numberValue(row.querySelector('[data-quantity]').value);
      const subtotal = selected && quantity > 0 ? Number(selected.unitPrice) * quantity : 0;
      row.querySelector('[data-subtotal]').textContent = rupiah.format(subtotal);
      if (selected && quantity > 0) {
        total += subtotal;
        count++;
        if (selected.durationDays !== null && selected.durationDays !== undefined) durations.push(Number(selected.durationDays));
      }
    });
    totalOutput.textContent = rupiah.format(total);
    itemCount.textContent = String(count);
    if (durations.length) {
      const longest = Math.max(...durations);
      const estimate = new Date();
      if (longest === 0) estimate.setHours(23, 59, 0, 0);
      else estimate.setDate(estimate.getDate() + longest);
      estimateOutput.textContent = formatDate(estimate);
    } else {
      estimateOutput.textContent = 'Belum ditentukan';
    }
    const paid = numberValue(paymentAmount.value);
    const cash = numberValue(cashTendered.value);
    changeOutput.textContent = rupiah.format(paymentMethod.value === 'Cash' ? Math.max(0, cash - Math.min(paid, total)) : 0);
  };

  document.querySelector('[data-add-order-item]')?.addEventListener('click', () => {
    addRow();
    rows.lastElementChild?.querySelector('select')?.focus();
  });
  (initialItems.length ? initialItems : [{}]).forEach(addRow);
  recalculate();

  const syncPaymentMethod = () => {
    const cash = paymentMethod.value === 'Cash';
    cashField.toggleAttribute('hidden', !cash);
    referenceField.toggleAttribute('hidden', cash);
    recalculate();
  };
  paymentMethod.addEventListener('change', syncPaymentMethod);
  paymentAmount.addEventListener('input', recalculate);
  cashTendered.addEventListener('input', recalculate);
  syncPaymentMethod();

  const customerInput = document.querySelector('[data-customer-search]');
  const customerId = document.querySelector('[data-customer-id]');
  const customerResults = document.querySelector('[data-customer-results]');
  let customerTimer;
  customerInput.addEventListener('input', () => {
    customerId.value = '';
    window.clearTimeout(customerTimer);
    const query = customerInput.value.trim();
    if (query.length < 2) {
      customerResults.hidden = true;
      return;
    }
    customerTimer = window.setTimeout(async () => {
      try {
        const response = await fetch(`/Customers/Search?q=${encodeURIComponent(query)}`, { headers: { Accept: 'application/json' } });
        const data = await response.json();
        customerResults.innerHTML = data.length
          ? data.map(customer => `<button type="button" role="option" data-customer-option data-id="${customer.id}" data-label="${customer.label.replace(/"/g, '&quot;')}"><strong>${customer.name}</strong><span>${customer.phoneNumber || 'Tanpa nomor HP'}</span></button>`).join('')
          : '<p>Tidak ada pelanggan yang cocok.</p>';
        customerResults.hidden = false;
      } catch {
        customerResults.innerHTML = '<p>Pencarian gagal. Coba kembali.</p>';
        customerResults.hidden = false;
      }
    }, 250);
  });
  customerResults.addEventListener('click', event => {
    const option = event.target.closest('[data-customer-option]');
    if (!option) return;
    customerId.value = option.dataset.id;
    customerInput.value = option.dataset.label;
    customerResults.hidden = true;
    rows.querySelector('select')?.focus();
  });
  document.addEventListener('click', event => {
    if (!event.target.closest('.customer-picker')) customerResults.hidden = true;
  });

  const dialog = document.querySelector('[data-quick-dialog]');
  document.querySelector('[data-open-quick-customer]')?.addEventListener('click', () => dialog.showModal());
  document.querySelector('[data-quick-customer]')?.addEventListener('submit', async event => {
    event.preventDefault();
    const quickForm = event.currentTarget;
    const error = quickForm.querySelector('[data-quick-error]');
    error.textContent = '';
    try {
      const response = await fetch(quickForm.action, { method: 'POST', body: new FormData(quickForm), headers: { Accept: 'application/json' } });
      const data = await response.json();
      if (!response.ok) throw new Error(data.message || 'Pelanggan tidak dapat disimpan.');
      customerId.value = data.id;
      customerInput.value = data.phoneNumber ? `${data.name} · ${data.phoneNumber}` : data.name;
      quickForm.reset();
      dialog.close();
      rows.querySelector('select')?.focus();
    } catch (exception) {
      error.textContent = exception.message;
    }
  });


  document.addEventListener('keydown', event => {
    if (event.ctrlKey && event.key === 'Enter') {
      event.preventDefault();
      form.requestSubmit();
    }
  });
})();
