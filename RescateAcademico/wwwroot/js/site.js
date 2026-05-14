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

        function getAntiForgeryToken() {
            var meta = document.querySelector('meta[name="request-verification-token"]');
            if (meta && meta.content) return meta.content;

            var input = document.querySelector('input[name="__RequestVerificationToken"]');
            return input ? input.value : '';
        }

        function updateBadge() {
            var token = getAntiForgeryToken();
            if (!token) return;

            fetch('/Notificaciones/GetConteoNoLeidas', {
                method: 'POST',
                credentials: 'same-origin',
                headers: {
                    'RequestVerificationToken': token,
                    'Accept': 'application/json'
                }
            })
                .then(function(r) { return r.ok ? r.json() : { count: 0 }; })
                .then(function(data) {
                    var n = parseInt(data && data.count, 10) || 0;
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


// ============================================
// Alumno Quick View — shared modal loader
// Used by Views/Shared/_AlumnoQuickView.cshtml.
// Supply role links via RaQuickView.config.
// ============================================
(function () {
    'use strict';

    function fmt(v) { return (v === undefined || v === null || v === '') ? '—' : v; }
    function colorByPromedio(p) {
        if (p >= 8) return 'var(--ra-success)';
        if (p >= 6) return 'var(--ra-warning)';
        return 'var(--ra-danger)';
    }

    window.RaQuickView = {
        /**
         * Configure which action buttons appear. Pass URL templates where
         * {matricula} is substituted. Any key omitted hides that button.
         * Call once per page, e.g.:
         *   RaQuickView.configure({
         *       profile: '/Profesor/HistorialAcademico?boleta={matricula}',
         *       intervencion: '/Intervenciones/Crear?matricula={matricula}'
         *   });
         */
        _cfg: {},
        configure: function (cfg) { this._cfg = Object.assign({}, cfg || {}); },

        open: async function (matricula) {
            var modalEl = document.getElementById('raQuickViewModal');
            if (!modalEl) { console.warn('[RaQuickView] modal partial not included'); return; }

            // Show modal first for perceived speed
            var existing = bootstrap.Modal.getInstance(modalEl);
            if (existing) existing.hide();
            var modal = new bootstrap.Modal(modalEl, { backdrop: true, keyboard: true });
            modal.show();

            modalEl.querySelector('#raQvLoading').style.display = 'block';
            modalEl.querySelector('#raQvContent').style.display = 'none';
            modalEl.querySelector('#raQvError').style.display = 'none';
            var titleSpan = modalEl.querySelector('#raQvTitle span');
            if (titleSpan) titleSpan.textContent = 'Cargando...';

            try {
                var response = await fetch('/Alumnos/QuickView?matricula=' + encodeURIComponent(matricula), { credentials: 'same-origin' });
                if (!response.ok) throw new Error('HTTP ' + response.status);
                var data = await response.json();

                if (titleSpan) titleSpan.textContent = (data.nombre || '') + ' ' + (data.apellidos || '');

                var fields = {
                    matricula: data.matricula,
                    carrera: fmt(data.carrera),
                    semestre: fmt(data.semestreActual),
                    grupo: fmt(data.grupo),
                    carga: (data.cargaAcademicaActual || 0) + ' materias',
                    actualizacion: fmt(data.fechaUltimaActualizacion),
                    reprobadas: data.materiasReprobadas || 0,
                    ausencias: data.ausencias || 0,
                    ets: data.etsPresentados || 0,
                    recursadas: data.recursamientos || 0
                };
                Object.keys(fields).forEach(function (key) {
                    var el = modalEl.querySelector('[data-qv="' + key + '"]');
                    if (el) el.textContent = fields[key];
                });

                var promedio = typeof data.promedioGlobal === 'number' ? data.promedioGlobal : parseFloat(data.promedioGlobal) || 0;
                var promedioEl = modalEl.querySelector('[data-qv="promedio"]');
                if (promedioEl) {
                    promedioEl.textContent = promedio.toFixed(2);
                    promedioEl.style.color = colorByPromedio(promedio);
                }

                var riesgoEl = modalEl.querySelector('[data-qv="riesgo"]');
                if (riesgoEl) {
                    riesgoEl.replaceChildren();
                    var sem = document.createElement('ra-semaforo');
                    sem.setAttribute('riesgo', data.riesgoAcademico || 'Verde');
                    sem.setAttribute('size', 'sm');
                    riesgoEl.appendChild(sem);
                }

                var cfg = this._cfg || {};
                ['profile', 'detalle', 'timeline', 'intervencion'].forEach(function (key) {
                    var link = modalEl.querySelector('[data-qv-link="' + key + '"]');
                    if (!link) return;
                    if (cfg[key]) {
                        link.href = cfg[key].replace('{matricula}', encodeURIComponent(matricula));
                        link.style.display = '';
                    } else {
                        link.style.display = 'none';
                    }
                });

                modalEl.querySelector('#raQvLoading').style.display = 'none';
                modalEl.querySelector('#raQvContent').style.display = 'block';
            } catch (err) {
                console.error('[RaQuickView]', err);
                modalEl.querySelector('#raQvLoading').style.display = 'none';
                modalEl.querySelector('#raQvError').style.display = '';
            }
        }
    };
})();

// ============================================
// Global Search (Ctrl/Cmd + K)
// Backend: /Home/GlobalSearch?q=...
// Returns [{ label, meta, url, icon, group }]
// Also supports purely client-side items added via RaSearch.register(...).
// ============================================
(function () {
    'use strict';

    var staticItems = [];
    var debounceTimer = null;
    var lastQuery = '';
    var activeIndex = 0;

    window.RaSearch = {
        register: function (items) {
            if (!Array.isArray(items)) return;
            staticItems = staticItems.concat(items);
        },
        open: function () {
            var bd = document.getElementById('raSearchBackdrop');
            if (!bd) return;
            bd.classList.add('active');
            var input = bd.querySelector('.ra-search-input');
            if (input) { input.value = ''; input.focus(); }
            renderResults('');
        },
        close: function () {
            var bd = document.getElementById('raSearchBackdrop');
            if (bd) bd.classList.remove('active');
        }
    };

    function renderResults(query) {
        var bd = document.getElementById('raSearchBackdrop');
        if (!bd) return;
        var container = bd.querySelector('.ra-search-results');
        if (!container) return;
        activeIndex = 0;
        var q = (query || '').trim().toLowerCase();

        var local = staticItems.filter(function (it) {
            if (!q) return true;
            return (it.label || '').toLowerCase().indexOf(q) !== -1 ||
                   (it.meta || '').toLowerCase().indexOf(q) !== -1;
        }).slice(0, 8);

        container.replaceChildren();
        renderGroup(container, 'Navegación', local);

        if (q.length >= 2) {
            fetch('/Home/GlobalSearch?q=' + encodeURIComponent(q), { credentials: 'same-origin' })
                .then(function (r) { return r.ok ? r.json() : []; })
                .then(function (results) {
                    if (q !== lastQuery) return; // stale
                    var byGroup = {};
                    (results || []).forEach(function (it) {
                        var g = it.group || 'Resultados';
                        (byGroup[g] = byGroup[g] || []).push(it);
                    });
                    Object.keys(byGroup).forEach(function (g) {
                        renderGroup(container, g, byGroup[g]);
                    });
                    if (!local.length && !(results || []).length) {
                        renderEmpty(container);
                    }
                    updateActive();
                })
                .catch(function () { /* silent */ });
        } else if (!local.length) {
            renderEmpty(container);
        }
        updateActive();
    }

    function renderGroup(parent, label, items) {
        if (!items || !items.length) return;
        var lbl = document.createElement('div');
        lbl.className = 'ra-search-group-label';
        lbl.textContent = label;
        parent.appendChild(lbl);
        items.forEach(function (it) {
            var a = document.createElement('a');
            a.className = 'ra-search-item';
            a.href = it.url;
            if (it.icon) {
                var ic = document.createElement('i');
                ic.className = 'bi ' + it.icon;
                a.appendChild(ic);
            }
            var txt = document.createElement('span');
            txt.style.flex = '1';
            txt.textContent = it.label || '';
            a.appendChild(txt);
            if (it.meta) {
                var m = document.createElement('span');
                m.className = 'ra-search-item-meta';
                m.textContent = it.meta;
                a.appendChild(m);
            }
            parent.appendChild(a);
        });
    }

    function renderEmpty(parent) {
        var empty = document.createElement('div');
        empty.className = 'ra-search-empty';
        empty.textContent = 'Sin resultados';
        parent.appendChild(empty);
    }

    function updateActive() {
        var bd = document.getElementById('raSearchBackdrop');
        if (!bd) return;
        var items = bd.querySelectorAll('.ra-search-item');
        items.forEach(function (it, idx) {
            it.classList.toggle('active', idx === activeIndex);
        });
    }

    document.addEventListener('DOMContentLoaded', function () {
        var trigger = document.getElementById('raSearchTrigger');
        if (trigger) trigger.addEventListener('click', function () { RaSearch.open(); });

        var bd = document.getElementById('raSearchBackdrop');
        if (!bd) return;
        var input = bd.querySelector('.ra-search-input');

        bd.addEventListener('click', function (e) {
            if (e.target === bd) RaSearch.close();
        });

        if (input) {
            input.addEventListener('input', function () {
                clearTimeout(debounceTimer);
                lastQuery = input.value.trim().toLowerCase();
                debounceTimer = setTimeout(function () { renderResults(input.value); }, 180);
            });
            input.addEventListener('keydown', function (e) {
                var items = bd.querySelectorAll('.ra-search-item');
                if (e.key === 'ArrowDown') {
                    e.preventDefault();
                    if (items.length) { activeIndex = (activeIndex + 1) % items.length; updateActive(); items[activeIndex].scrollIntoView({ block: 'nearest' }); }
                } else if (e.key === 'ArrowUp') {
                    e.preventDefault();
                    if (items.length) { activeIndex = (activeIndex - 1 + items.length) % items.length; updateActive(); items[activeIndex].scrollIntoView({ block: 'nearest' }); }
                } else if (e.key === 'Enter') {
                    e.preventDefault();
                    if (items[activeIndex]) items[activeIndex].click();
                } else if (e.key === 'Escape') {
                    e.preventDefault();
                    RaSearch.close();
                }
            });
        }

        document.addEventListener('keydown', function (e) {
            var isK = (e.key || '').toLowerCase() === 'k';
            if (isK && (e.ctrlKey || e.metaKey)) {
                var target = e.target;
                var tag = target && target.tagName;
                if (tag === 'INPUT' || tag === 'TEXTAREA' || (target && target.isContentEditable)) return;
                e.preventDefault();
                RaSearch.open();
            }
            if (e.key === 'Escape' && bd.classList.contains('active')) {
                RaSearch.close();
            }
        });
    });
})();
