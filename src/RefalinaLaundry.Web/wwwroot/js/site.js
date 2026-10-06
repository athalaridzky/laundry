(() => {
  'use strict';

  const navToggle = document.querySelector('[data-nav-toggle]');
  const sidebar = document.querySelector('[data-sidebar]');
  navToggle?.addEventListener('click', () => {
    const open = document.body.classList.toggle('nav-open');
    navToggle.setAttribute('aria-expanded', String(open));
    sidebar?.setAttribute('aria-hidden', String(!open));
  });

  document.addEventListener('click', event => {
    const target = event.target.closest('[data-confirm]');
    if (target && !window.confirm(target.dataset.confirm)) {
      event.preventDefault();
    }
  });

  const filterForm = document.querySelector('[data-auto-filter]');
  const filterSearch = filterForm?.querySelector('input[name="search"]');
  let filterTimer;
  filterSearch?.addEventListener('input', () => {
    window.clearTimeout(filterTimer);
    const value = filterSearch.value.trim();
    if (value.length === 1) return;
    filterTimer = window.setTimeout(() => filterForm.requestSubmit(), 450);
  });

  const detailMethod = document.querySelector('[data-detail-payment-method]');
  const detailCash = document.querySelector('[data-detail-cash]');
  const detailReference = document.querySelector('[data-detail-reference]');
  const syncDetailPayment = () => {
    if (!detailMethod) return;
    const cash = detailMethod.value === 'Cash';
    detailCash?.toggleAttribute('hidden', !cash);
    detailReference?.toggleAttribute('hidden', cash);
  };
  detailMethod?.addEventListener('change', syncDetailPayment);
  syncDetailPayment();
})();
