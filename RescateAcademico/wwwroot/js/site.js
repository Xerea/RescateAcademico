// ============================================
// Rescate Academico — Theme, Notifications, Toasts, Semaforo, CountUp
// ============================================

(function () {
    'use strict';

    // --- Theme Management ---
    const STORAGE_KEY = 'ra-theme';
    const html = document.documentElement;
    const toggleBtn = document.getElementById('themeToggle');
    const toggleIcon = document.getElementById('themeIcon');

    function getStoredTheme() {
        try { return localStorage.getItem(STORAGE_KEY); } catch { return null; }
    }
    function setStoredTheme(theme) {
        try { localStorage.setItem(STORAGE_KEY, theme); } catch { }
    }
    function getPreferredTheme() {
        const stored = getStoredTheme();
        if (stored) return stored;
        return window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
    }
    function applyTheme(theme) {
        html.setAttribute('data-bs-theme', theme);
        if (toggleIcon) {
            toggleIcon.className = theme === 'dark' ? 'bi bi-moon-fill' : 'bi bi-sun-fill';
        }
    }
    function toggleTheme() {
        const current = html.getAttribute('data-bs-theme') || 'light';
        const next = current === 'light' ? 'dark' : 'light';
        applyTheme(next);
        setStoredTheme(next);
    }
    applyTheme(getPreferredTheme());
    if (toggleBtn) toggleBtn.addEventListener('click', toggleTheme);
    window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', e => {
        if (!getStoredTheme()) applyTheme(e.matches ? 'dark' : 'light');
    });

    // --- Auto-dismiss alerts after 6 seconds ---
    document.querySelectorAll('.alert-dismissible').forEach(function (alert) {
        setTimeout(function () {
            var bsAlert = bootstrap.Alert.getOrCreateInstance(alert);
            if (bsAlert) bsAlert.close();
        }, 6000);
    });

    // --- Toast System ---
    window.RaToast = {
        container: document.getElementById('raToastContainer'),
        show: function(message, type, duration) {
            type = type || 'info';
            duration = duration || 5000;
            if (!this.container) this.container = document.getElementById('raToastContainer');
            if (!this.container) return;

            var icons = { success: 'bi-check-circle-fill', danger: 'bi-x-circle-fill', warning: 'bi-exclamation-triangle-fill', info: 'bi-info-circle-fill' };
            var toast = document.createElement('div');
            toast.className = 'ra-toast ra-toast-' + type;

            var icon = document.createElement('i');
            icon.className = 'bi ' + (icons[type] || icons.info) + ' ra-toast-icon';

            var body = document.createElement('div');
            body.style.flex = '1';
            body.style.lineHeight = '1.4';
            body.textContent = message || '';

            var close = document.createElement('button');
            close.type = 'button';
            close.className = 'ra-toast-close';
            close.setAttribute('aria-label', 'Cerrar');
            var closeIcon = document.createElement('i');
            closeIcon.className = 'bi bi-x';
            close.appendChild(closeIcon);

            toast.appendChild(icon);
            toast.appendChild(body);
            toast.appendChild(close);

            toast.querySelector('.ra-toast-close').addEventListener('click', function() { RaToast.dismiss(toast); });
            this.container.appendChild(toast);

            // Animate in with GSAP or fallback
            if (typeof gsap !== 'undefined') {
                gsap.to(toast, { opacity: 1, x: 0, duration: 0.4, ease: 'power3.out' });
            } else {
                toast.style.opacity = '1';
                toast.style.transform = 'translateX(0)';
            }

            if (duration > 0) {
                setTimeout(function() { RaToast.dismiss(toast); }, duration);
            }
            return toast;
        },
        dismiss: function(toast) {
            if (!toast || toast._dismissing) return;
            toast._dismissing = true;
            if (typeof gsap !== 'undefined') {
                gsap.to(toast, { opacity: 0, x: 40, duration: 0.3, ease: 'power3.in', onComplete: function() { if (toast.parentNode) toast.parentNode.removeChild(toast); } });
            } else {
                toast.style.opacity = '0';
                toast.style.transform = 'translateX(40px)';
                setTimeout(function() { if (toast.parentNode) toast.parentNode.removeChild(toast); }, 300);
            }
        }
    };

    // --- Notification Badge Polling ---
    (function initNotificationPolling() {
        var badgeEl = document.getElementById('raNotifBadge');
        if (!badgeEl) return;

        function updateBadge() {
            fetch('/Notificaciones/GetConteoNoLeidas', { method: 'GET', credentials: 'same-origin' })
                .then(function(r) { return r.text(); })
                .then(function(count) {
                    var n = parseInt(count, 10) || 0;
                    if (n > 0) {
                        badgeEl.textContent = n > 99 ? '99+' : n;
                        badgeEl.style.display = 'inline-flex';
                    } else {
                        badgeEl.style.display = 'none';
                    }
                })
                .catch(function() { /* silent */ });
        }
        updateBadge();
        setInterval(updateBadge, 30000); // every 30s
    })();

    // --- CountUp Initialization ---
    window.RaCountUp = {
        init: function() {
            if (typeof countUp === 'undefined') return;
            document.querySelectorAll('[data-countup]').forEach(function(el) {
                var target = parseFloat(el.dataset.countup);
                var suffix = el.dataset.countupSuffix || '';
                var prefix = el.dataset.countupPrefix || '';
                var decimals = parseInt(el.dataset.countupDecimals, 10);
                if (isNaN(decimals)) decimals = 0;
                if (isNaN(target)) return;

                var opts = {
                    decimalPlaces: decimals,
                    suffix: suffix,
                    prefix: prefix,
                    duration: 1.5,
                    useEasing: true
                };
                var c = new countUp.CountUp(el, target, opts);
                // Use IntersectionObserver to trigger when visible
                var observer = new IntersectionObserver(function(entries) {
                    entries.forEach(function(entry) {
                        if (entry.isIntersecting) {
                            c.start();
                            observer.unobserve(el);
                        }
                    });
                }, { threshold: 0.3 });
                observer.observe(el);
            });
        }
    };
    document.addEventListener('DOMContentLoaded', function() {
        RaCountUp.init();
    });

    // --- Semaforo Web Component ---
    class RaSemaforo extends HTMLElement {
        constructor() {
            super();
        }
        connectedCallback() {
            this.render();
        }
        static get observedAttributes() { return ['riesgo', 'size', 'tooltip']; }
        attributeChangedCallback() {
            this.render();
        }
        render() {
            var riesgo = (this.getAttribute('riesgo') || 'Verde').toLowerCase();
            var size = this.getAttribute('size') || '';
            var tooltip = this.getAttribute('tooltip') || '';
            var verdeActive = riesgo === 'verde' ? 'active' : '';
            var amarilloActive = riesgo === 'amarillo' ? 'active' : '';
            var rojoActive = riesgo === 'rojo' ? 'active' : '';

            var sizeClass = size ? ' ra-semaforo-' + size : '';
            this.className = 'ra-semaforo' + sizeClass;
            this.replaceChildren();
            [
                ['verde', verdeActive],
                ['amarillo', amarilloActive],
                ['rojo', rojoActive]
            ].forEach(function(item) {
                var dot = document.createElement('span');
                dot.className = 'ra-semaforo-dot ' + item[0] + ' ' + item[1];
                this.appendChild(dot);
            }, this);
            if (tooltip) {
                var tooltipEl = document.createElement('span');
                tooltipEl.className = 'ra-semaforo-tooltip';
                tooltipEl.textContent = tooltip;
                this.appendChild(tooltipEl);
            }
        }
    }
    if (!customElements.get('ra-semaforo')) {
        customElements.define('ra-semaforo', RaSemaforo);
    }

    // --- Page content fade-in ---
    document.addEventListener('DOMContentLoaded', function() {
        document.querySelectorAll('.ra-page-content').forEach(function(el) {
            el.classList.add('loaded');
        });
    });

    // --- GSAP ScrollTrigger registration ---
    if (typeof gsap !== 'undefined' && typeof ScrollTrigger !== 'undefined') {
        gsap.registerPlugin(ScrollTrigger);
    }
})();
