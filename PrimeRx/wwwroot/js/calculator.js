/**
 * calculator.js
 * Floating, draggable, in-app calculator widget with keyboard support and active input insertion.
 */

(function () {
  'use strict';

  // Prevent multiple initializations
  if (document.getElementById('rxCalculatorToggle')) return;

  // Track the last focused input field to insert the calculator result into it
  let lastActiveInput = null;

  document.addEventListener('focusin', function (e) {
    if (e.target && (e.target.tagName === 'INPUT' || e.target.tagName === 'TEXTAREA')) {
      const type = e.target.type || 'text';
      // Only target editable text/number fields
      if (['text', 'number', 'tel'].includes(type) && !e.target.readOnly && !e.target.disabled) {
        lastActiveInput = e.target;
      }
    }
  });

  // Create Calculator DOM Elements
  const toggleBtn = document.createElement('button');
  toggleBtn.id = 'rxCalculatorToggle';
  toggleBtn.className = 'rx-calc-toggle-btn';
  toggleBtn.title = 'Open Calculator';
  toggleBtn.innerHTML = '<i class="bi bi-calculator"></i>';
  document.body.appendChild(toggleBtn);

  const container = document.createElement('div');
  container.id = 'rxCalculator';
  container.className = 'rx-calc-container';
  container.style.display = 'none';
  container.innerHTML = `
    <div class="rx-calc-header" id="rxCalcHeader">
      <div class="rx-calc-title">
        <i class="bi bi-calculator"></i> Calculator
      </div>
      <div class="rx-calc-controls">
        <button class="rx-calc-header-btn" id="rxCalcMinimize" title="Minimize">
          <i class="bi bi-dash-lg"></i>
        </button>
      </div>
    </div>
    <div class="rx-calc-display-section">
      <div class="rx-calc-history" id="rxCalcHistory"></div>
      <div class="rx-calc-display" id="rxCalcDisplay">0</div>
    </div>
    <div class="rx-calc-buttons">
      <button class="rx-calc-btn rx-calc-btn-action" data-key="clear">C</button>
      <button class="rx-calc-btn rx-calc-btn-action" data-key="backspace"><i class="bi bi-backspace"></i></button>
      <button class="rx-calc-btn rx-calc-btn-operator" data-key="%">%</button>
      <button class="rx-calc-btn rx-calc-btn-operator" data-key="/">÷</button>
      
      <button class="rx-calc-btn" data-key="7">7</button>
      <button class="rx-calc-btn" data-key="8">8</button>
      <button class="rx-calc-btn" data-key="9">9</button>
      <button class="rx-calc-btn rx-calc-btn-operator" data-key="*">×</button>
      
      <button class="rx-calc-btn" data-key="4">4</button>
      <button class="rx-calc-btn" data-key="5">5</button>
      <button class="rx-calc-btn" data-key="6">6</button>
      <button class="rx-calc-btn rx-calc-btn-operator" data-key="-">−</button>
      
      <button class="rx-calc-btn" data-key="1">1</button>
      <button class="rx-calc-btn" data-key="2">2</button>
      <button class="rx-calc-btn" data-key="3">3</button>
      <button class="rx-calc-btn rx-calc-btn-operator" data-key="+">+</button>
      
      <button class="rx-calc-btn" data-key="0">0</button>
      <button class="rx-calc-btn" data-key=".">.</button>
      <button class="rx-calc-btn rx-calc-btn-operator" id="rxCalcUse" title="Insert value into active input" style="color:#22c55e;"><i class="bi bi-arrow-down-left-square-fill"></i> Use</button>
      <button class="rx-calc-btn rx-calc-btn-operator" data-key="=" style="background:rgba(99,102,241,0.25);">=</button>
    </div>
    <div class="rx-calc-hint" id="rxCalcHint">Click 'Use' to insert into field</div>
  `;
  document.body.appendChild(container);

  const displayEl = document.getElementById('rxCalcDisplay');
  const historyEl = document.getElementById('rxCalcHistory');
  const hintEl = document.getElementById('rxCalcHint');

  let currentExpression = '';
  let justEvaluated = false;

  // Toggle Visibility
  toggleBtn.addEventListener('click', function () {
    if (container.style.display === 'none') {
      container.style.display = 'flex';
      container.style.opacity = '0';
      container.style.transform = 'scale(0.9) translateY(10px)';
      setTimeout(() => {
        container.style.opacity = '1';
        container.style.transform = 'scale(1) translateY(0)';
      }, 10);
      updateInsertHint();
    } else {
      closeCalculator();
    }
  });

  document.getElementById('rxCalcMinimize').addEventListener('click', closeCalculator);

  function closeCalculator() {
    container.style.opacity = '0';
    container.style.transform = 'scale(0.9) translateY(10px)';
    setTimeout(() => {
      container.style.display = 'none';
    }, 200);
  }

  // Update insert capability hint text
  function updateInsertHint() {
    if (lastActiveInput && document.body.contains(lastActiveInput)) {
      const name = lastActiveInput.placeholder || lastActiveInput.name || lastActiveInput.id || 'focused field';
      hintEl.textContent = `Active field: "${name}"`;
      document.getElementById('rxCalcUse').disabled = false;
      document.getElementById('rxCalcUse').style.opacity = '1';
    } else {
      hintEl.textContent = 'No input field active. Click a field first.';
      document.getElementById('rxCalcUse').disabled = true;
      document.getElementById('rxCalcUse').style.opacity = '0.5';
    }
  }

  // Handle calculator input
  function handleInput(key) {
    if (key === 'clear') {
      currentExpression = '';
      displayEl.textContent = '0';
      historyEl.textContent = '';
      justEvaluated = false;
    } else if (key === 'backspace') {
      if (justEvaluated) {
        currentExpression = '';
        displayEl.textContent = '0';
        justEvaluated = false;
      } else {
        currentExpression = currentExpression.slice(0, -1);
        displayEl.textContent = currentExpression || '0';
      }
    } else if (key === '=') {
      evaluate();
    } else if (['+', '-', '*', '/', '%'].includes(key)) {
      if (justEvaluated) {
        currentExpression = displayEl.textContent;
        justEvaluated = false;
      }
      // Avoid consecutive operators
      const lastChar = currentExpression.slice(-1);
      if (['+', '-', '*', '/', '%'].includes(lastChar)) {
        currentExpression = currentExpression.slice(0, -1);
      }
      currentExpression += key;
      displayEl.textContent = currentExpression;
    } else {
      if (justEvaluated) {
        currentExpression = '';
        justEvaluated = false;
      }
      // Prevent multiple decimals in a single number block
      if (key === '.') {
        const parts = currentExpression.split(/[\+\-\*\/%]/);
        const lastPart = parts[parts.length - 1];
        if (lastPart.includes('.')) return;
      }
      currentExpression += key;
      displayEl.textContent = currentExpression;
    }
  }

  // Evaluate Expression Safely
  function evaluate() {
    if (!currentExpression) return;
    try {
      let expression = currentExpression;
      // Sanitize math expression (allow only numbers, decimal, and basic operators)
      if (/^[0-9+\-*/%.() ]+$/.test(expression)) {
        historyEl.textContent = expression + ' =';
        // Run safe mathematical function evaluation
        let result = Function(`"use strict"; return (${expression})`)();
        
        // Handle float precision errors (e.g. 0.1 + 0.2)
        if (typeof result === 'number' && !isNaN(result)) {
          if (!Number.isInteger(result)) {
            result = parseFloat(result.toFixed(8));
          }
          displayEl.textContent = result;
          currentExpression = String(result);
          justEvaluated = true;
        } else {
          throw new Error('Invalid output');
        }
      } else {
        throw new Error('Safety check failed');
      }
    } catch (e) {
      displayEl.textContent = 'Error';
      currentExpression = '';
      justEvaluated = true;
    }
  }

  // Insert value into active page input
  function insertValue() {
    const val = parseFloat(displayEl.textContent);
    if (isNaN(val)) return;

    if (lastActiveInput && document.body.contains(lastActiveInput)) {
      lastActiveInput.value = val;
      
      // Fire events so framework (like Angular/Vue/Vanilla JS listeners) updates totals
      lastActiveInput.dispatchEvent(new Event('input', { bubbles: true }));
      lastActiveInput.dispatchEvent(new Event('change', { bubbles: true }));
      
      // Flash input background green to indicate insertion
      const originalBg = lastActiveInput.style.backgroundColor;
      lastActiveInput.style.transition = 'background-color 0.2s';
      lastActiveInput.style.backgroundColor = 'rgba(34, 197, 94, 0.2)';
      setTimeout(() => {
        lastActiveInput.style.backgroundColor = originalBg;
      }, 300);
    }
  }

  document.getElementById('rxCalcUse').addEventListener('click', insertValue);

  // Wire up button click events
  container.addEventListener('click', function (e) {
    const btn = e.target.closest('.rx-calc-btn');
    if (!btn) return;
    const key = btn.dataset.key;
    if (key) {
      handleInput(key);
    }
  });

  // Keyboard navigation support
  document.addEventListener('keydown', function (e) {
    if (container.style.display === 'none') return;

    // Check if the user is typing into another input field
    if (e.target && e.target !== document.body && e.target.tagName === 'INPUT') {
      // If typing numbers/operators in page inputs, do not intercept
      // unless pressing Alt + C (shortcut to insert calculator value)
      if (e.altKey && e.key.toLowerCase() === 'c') {
        e.preventDefault();
        insertValue();
      }
      return;
    }

    if (e.key >= '0' && e.key <= '9') {
      e.preventDefault();
      handleInput(e.key);
    } else if (e.key === '.') {
      e.preventDefault();
      handleInput(e.key);
    } else if (e.key === '+' || e.key === '-' || e.key === '*' || e.key === '/' || e.key === '%') {
      e.preventDefault();
      handleInput(e.key);
    } else if (e.key === 'Enter' || e.key === '=') {
      e.preventDefault();
      handleInput('=');
    } else if (e.key === 'Backspace') {
      e.preventDefault();
      handleInput('backspace');
    } else if (e.key === 'Escape' || e.key.toLowerCase() === 'c') {
      e.preventDefault();
      handleInput('clear');
    } else if (e.key.toLowerCase() === 'u') {
      e.preventDefault();
      insertValue();
    }
  });

  // Make Calculator Draggable
  const header = document.getElementById('rxCalcHeader');
  let isDragging = false;
  let startX, startY, initialX, initialY;

  header.addEventListener('mousedown', dragStart);
  document.addEventListener('mousemove', drag);
  document.addEventListener('mouseup', dragEnd);

  function dragStart(e) {
    // Only drag with left click
    if (e.button !== 0) return;
    
    // Do not drag if clicking minimized button
    if (e.target.closest('.rx-calc-header-btn')) return;

    isDragging = true;
    container.style.transition = 'none'; // Disable transition during drag
    
    startX = e.clientX;
    startY = e.clientY;

    const rect = container.getBoundingClientRect();
    initialX = rect.left;
    initialY = rect.top;

    e.preventDefault();
  }

  function drag(e) {
    if (!isDragging) return;

    const dx = e.clientX - startX;
    const dy = e.clientY - startY;

    let newX = initialX + dx;
    let newY = initialY + dy;

    // Boundary containment
    const viewW = window.innerWidth;
    const viewH = window.innerHeight;
    const rect = container.getBoundingClientRect();

    newX = Math.max(0, Math.min(newX, viewW - rect.width));
    newY = Math.max(0, Math.min(newY, viewH - rect.height));

    container.style.left = newX + 'px';
    container.style.top = newY + 'px';
    container.style.bottom = 'auto';
    container.style.right = 'auto';
  }

  function dragEnd() {
    isDragging = false;
    container.style.transition = '';
  }

  // Periodic check for focused inputs
  setInterval(updateInsertHint, 1000);

})();
