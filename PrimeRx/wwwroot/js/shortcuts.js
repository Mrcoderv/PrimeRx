/**
 * shortcuts.js — Global keyboard shortcuts & help modal for PrimeRx
 * Ctrl+N New Bill, Ctrl+P Print, Ctrl+S Save, F1 Help, F2 Batch, F4 Calculator
 */
(function () {
    'use strict';

    /* ── Help Modal ────────────────────────────────────────────────────────── */
    const HELP_HTML = `
<div class="rx-help-overlay" id="rxHelpOverlay" style="display:none">
  <div class="rx-help-modal">
    <div class="rx-help-header">
      <h5 class="mb-0"><i class="bi bi-keyboard me-2"></i>Keyboard Shortcuts</h5>
      <button type="button" class="btn-close btn-close-white" id="rxHelpClose" aria-label="Close"></button>
    </div>
    <div class="rx-help-body">
      <input type="text" class="form-control form-control-sm mb-3" id="rxHelpSearch"
             placeholder="Search shortcuts..." autocomplete="off" />
      <div class="rx-help-scroll" id="rxHelpScroll">

        <div class="rx-help-group" data-group="global">
          <div class="rx-help-group-title">Global</div>
          <div class="rx-help-row"><span class="rx-help-desc">Open global search</span><span class="rx-help-keys"><kbd>Ctrl</kbd>+<kbd>K</kbd></span></div>
          <div class="rx-help-row"><span class="rx-help-desc">New bill (POS)</span><span class="rx-help-keys"><kbd>Ctrl</kbd>+<kbd>N</kbd></span></div>
          <div class="rx-help-row"><span class="rx-help-desc">New purchase</span><span class="rx-help-keys"><kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>N</kbd></span></div>
          <div class="rx-help-row"><span class="rx-help-desc">Save / Submit</span><span class="rx-help-keys"><kbd>Ctrl</kbd>+<kbd>S</kbd></span></div>
          <div class="rx-help-row"><span class="rx-help-desc">Print page</span><span class="rx-help-keys"><kbd>Ctrl</kbd>+<kbd>P</kbd></span></div>
          <div class="rx-help-row"><span class="rx-help-desc">Keyboard shortcuts help</span><span class="rx-help-keys"><kbd>F1</kbd></span></div>
          <div class="rx-help-row"><span class="rx-help-desc">Close popup / modal</span><span class="rx-help-keys"><kbd>Esc</kbd></span></div>
        </div>

        <div class="rx-help-group" data-group="billing">
          <div class="rx-help-group-title">Billing (POS)</div>
          <div class="rx-help-row"><span class="rx-help-desc">Quick-set qty (1-9) on last item</span><span class="rx-help-keys"><kbd>1</kbd>-<kbd>9</kbd></span></div>
          <div class="rx-help-row"><span class="rx-help-desc">Open calculator on Rate / Qty / Disc</span><span class="rx-help-keys"><kbd>F4</kbd></span></div>
          <div class="rx-help-row"><span class="rx-help-desc">Open calculator (alternative)</span><span class="rx-help-keys"><kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>C</kbd></span></div>
          <div class="rx-help-row"><span class="rx-help-desc">Rate &rarr; Qty &rarr; Disc% &rarr; Search</span><span class="rx-help-keys"><kbd>Enter</kbd></span></div>
          <div class="rx-help-row"><span class="rx-help-desc">Navigate search results</span><span class="rx-help-keys"><kbd>&uarr;</kbd><kbd>&darr;</kbd></span></div>
          <div class="rx-help-row"><span class="rx-help-desc">Select medicine from popup</span><span class="rx-help-keys"><kbd>Enter</kbd></span></div>
        </div>

        <div class="rx-help-group" data-group="purchase">
          <div class="rx-help-group-title">Purchase Entry</div>
          <div class="rx-help-row"><span class="rx-help-desc">Smart field navigation</span><span class="rx-help-keys"><kbd>Enter</kbd></span></div>
          <div class="rx-help-row"><span class="rx-help-desc">&nbsp;&nbsp;Flow: Batch &rarr; Expiry &rarr; Qty &rarr; Free &rarr; Rate &rarr; Disc &rarr; CC &rarr; MRP</span><span class="rx-help-keys"></span></div>
          <div class="rx-help-row"><span class="rx-help-desc">Open calculator on Qty / Rate / CC</span><span class="rx-help-keys"><kbd>F4</kbd></span></div>
          <div class="rx-help-row"><span class="rx-help-desc">Show batch info panel</span><span class="rx-help-keys"><kbd>F2</kbd></span></div>
          <div class="rx-help-row"><span class="rx-help-desc">Navigate between rows</span><span class="rx-help-keys"><kbd>&uarr;</kbd><kbd>&darr;</kbd></span></div>
        </div>

        <div class="rx-help-group" data-group="calculator">
          <div class="rx-help-group-title">Floating Calculator</div>
          <div class="rx-help-row"><span class="rx-help-desc">Open calculator widget</span><span class="rx-help-keys"><i class="bi bi-calculator"></i> button</span></div>
          <div class="rx-help-row"><span class="rx-help-desc">Open on Qty / Rate / CC field</span><span class="rx-help-keys"><kbd>F4</kbd> / Right-click</span></div>
          <div class="rx-help-row"><span class="rx-help-desc">Insert result into field</span><span class="rx-help-keys"><kbd>U</kbd> or <kbd>Alt</kbd>+<kbd>C</kbd></span></div>
          <div class="rx-help-row"><span class="rx-help-desc">Evaluate expression</span><span class="rx-help-keys"><kbd>Enter</kbd> / <kbd>=</kbd></span></div>
          <div class="rx-help-row"><span class="rx-help-desc">Clear display</span><span class="rx-help-keys"><kbd>Esc</kbd> / <kbd>C</kbd></span></div>
        </div>

        <div class="rx-help-group" data-group="admin">
          <div class="rx-help-group-title">Inventory &amp; Admin</div>
          <div class="rx-help-row"><span class="rx-help-desc">Focus search bar</span><span class="rx-help-keys"><kbd>/</kbd></span></div>
          <div class="rx-help-row"><span class="rx-help-desc">Navigate ageing dues tabs</span><span class="rx-help-keys"><kbd>Alt</kbd>+<kbd>1</kbd>/<kbd>2</kbd></span></div>
          <div class="rx-help-row"><span class="rx-help-desc">Focus filter dropdown</span><span class="rx-help-keys"><kbd>Alt</kbd>+<kbd>F</kbd></span></div>
          <div class="rx-help-row"><span class="rx-help-desc">Clear all filters</span><span class="rx-help-keys"><kbd>Alt</kbd>+<kbd>C</kbd></span></div>
        </div>

      </div>
    </div>
    <div class="rx-help-footer">
      <span><kbd>↑↓</kbd> Search</span>
      <span><kbd>Esc</kbd> Close</span>
      <span class="text-muted">Press <kbd>F1</kbd> anywhere to open</span>
    </div>
  </div>
</div>`;

    // Inject modal into DOM
    const wrapper = document.createElement('div');
    wrapper.innerHTML = HELP_HTML;
    document.body.appendChild(wrapper.firstElementChild);

    const overlay  = document.getElementById('rxHelpOverlay');
    const helpClose = document.getElementById('rxHelpClose');
    const helpSearch = document.getElementById('rxHelpSearch');
    const helpScroll = document.getElementById('rxHelpScroll');

    function openHelp() {
        overlay.style.display = 'flex';
        setTimeout(() => { helpSearch.value = ''; filterHelp(''); helpSearch.focus(); }, 50);
    }

    function closeHelp() {
        overlay.style.display = 'none';
    }

    if (helpClose) helpClose.addEventListener('click', closeHelp);
    if (overlay) overlay.addEventListener('click', e => { if (e.target === overlay) closeHelp(); });

    // Search / filter shortcuts
    function filterHelp(term) {
        const t = term.toLowerCase();
        helpScroll.querySelectorAll('.rx-help-group').forEach(g => {
            let anyVisible = false;
            g.querySelectorAll('.rx-help-row').forEach(r => {
                const desc = r.querySelector('.rx-help-desc')?.textContent?.toLowerCase() || '';
                const match = !t || desc.includes(t);
                r.style.display = match ? '' : 'none';
                if (match) anyVisible = true;
            });
            g.style.display = anyVisible ? '' : 'none';
        });
    }

    if (helpSearch) helpSearch.addEventListener('input', () => filterHelp(helpSearch.value));

    /* ── Global Keyboard Shortcuts ─────────────────────────────────────────── */
    document.addEventListener('keydown', function (e) {
        const active = document.activeElement;
        const isInput = active && (active.tagName === 'INPUT' || active.tagName === 'TEXTAREA' || active.tagName === 'SELECT' || active.isContentEditable);
        const isModalOpen = overlay && overlay.style.display === 'flex';
        const isSearchOpen = document.getElementById('globalSearchOverlay')?.classList.contains('gs-open');

        // F1 — Help (always works, even in inputs)
        if (e.key === 'F1') {
            e.preventDefault();
            if (isModalOpen) closeHelp();
            else openHelp();
            return;
        }

        // Escape — close help
        if (e.key === 'Escape' && isModalOpen) {
            e.preventDefault();
            closeHelp();
            return;
        }

        // Don't intercept shortcuts when typing in inputs (except Ctrl combos)
        if (isInput && !e.ctrlKey && !e.altKey && !e.metaKey) {
            // F2/F4 still work in billing/purchase inputs — handled by billing.js/purchase.js
            return;
        }

        // Don't intercept when search or help is open
        if (isSearchOpen || isModalOpen) return;

        // Ctrl+N — New Bill
        if ((e.ctrlKey || e.metaKey) && e.key === 'n' && !e.shiftKey) {
            e.preventDefault();
            window.location.href = '/Billing/Index';
            return;
        }

        // Ctrl+Shift+N — New Purchase
        if ((e.ctrlKey || e.metaKey) && e.shiftKey && e.key.toLowerCase() === 'n') {
            e.preventDefault();
            window.location.href = '/Purchase/Create';
            return;
        }

        // Ctrl+S — Save (trigger form submit)
        if ((e.ctrlKey || e.metaKey) && e.key === 's') {
            e.preventDefault();
            const form = document.querySelector('form[method="post"]');
            if (form) {
                const btn = form.querySelector('button[type="submit"]');
                if (btn && !btn.disabled) btn.click();
            }
            return;
        }

        // Ctrl+P — Print
        if ((e.ctrlKey || e.metaKey) && e.key === 'p') {
            e.preventDefault();
            window.print();
            return;
        }

        // / (slash) — Focus medicine search when not in an input
        if (e.key === '/' && !isInput && !e.ctrlKey && !e.altKey) {
            const medSearch = document.getElementById('medicineSearchText');
            if (medSearch) {
                e.preventDefault();
                medSearch.focus();
            }
            return;
        }
    });

    /* ── Expose openHelp globally for navbar button ───────────────────────── */
    window.RxShortcuts = { openHelp, closeHelp };

})();
