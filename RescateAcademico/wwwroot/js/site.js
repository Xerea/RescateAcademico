// ============================================
// Rescate Academico — Theme, Notifications, Toasts, Semaforo, CountUp
// ============================================

(function () {
    'use strict';

    window.RaDataTablesEs = {
        decimal: ',',
        thousands: '.',
        emptyTable: 'No hay datos disponibles',
        info: 'Mostrando _START_ a _END_ de _TOTAL_ registros',
        infoEmpty: 'Mostrando 0 registros',
        infoFiltered: '(filtrado de _MAX_ registros totales)',
        lengthMenu: 'Mostrar _MENU_ registros',
        loadingRecords: 'Cargando...',
        processing: 'Procesando...',
        search: 'Buscar:',
        zeroRecords: 'No se encontraron resultados',
        paginate: {
            first: 'Primero',
            last: 'Último',
            next: 'Siguiente',
            previous: 'Anterior'
        },
        aria: {
            sortAscending: ': activar para ordenar ascendente',
            sortDescending: ': activar para ordenar descendente'
        }
    };

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

    // --- Bootstrap Modal Coordinator ---
    // Keeps custom RA modals interactive even when they are rendered inside
    // animated/cards/layout containers. Bootstrap modals must live under body
    // and must keep the modal-content class for pointer/focus handling.
    window.RaModalSystem = {
        normalize: function (modalEl) {
            if (!modalEl || !modalEl.classList || !modalEl.classList.contains('modal')) return null;

            if (modalEl.parentElement !== document.body) {
                document.body.appendChild(modalEl);
            }

            var customContent = modalEl.querySelector('.modal-dialog > .ra-modal-content:not(.modal-content)');
            if (customContent) {
                customContent.classList.add('modal-content');
            }

            return modalEl;
        },

        closeBlockingOverlays: function () {
            var search = document.getElementById('raSearchBackdrop');
            if (search) search.classList.remove('active');

            var notifPanel = document.getElementById('raNotifPanel');
            if (notifPanel) {
                notifPanel.classList.remove('active');
                notifPanel.setAttribute('aria-hidden', 'true');
            }

            var notifScrim = document.getElementById('raNotifScrim');
            if (notifScrim) {
                notifScrim.classList.remove('active');
                notifScrim.hidden = true;
            }

            var notifTrigger = document.getElementById('raNotifTrigger');
            if (notifTrigger) notifTrigger.setAttribute('aria-expanded', 'false');
        },

        cleanupChrome: function () {
            if (document.querySelector('.modal.show')) return;
            document.querySelectorAll('.modal-backdrop').forEach(function (backdrop) {
                backdrop.remove();
            });
            document.body.classList.remove('modal-open');
            document.body.style.removeProperty('overflow');
            document.body.style.removeProperty('padding-right');
        },

        prepareOpen: function (modalEl) {
            this.normalize(modalEl);
            this.closeBlockingOverlays();
            this.cleanupChrome();
        }
    };

    document.addEventListener('click', function (e) {
        var trigger = e.target.closest('[data-bs-toggle="modal"]');
        if (!trigger) return;

        var selector = trigger.getAttribute('data-bs-target') || trigger.getAttribute('href');
        if (!selector || selector.charAt(0) !== '#') return;

        var modalEl = document.querySelector(selector);
        if (modalEl) window.RaModalSystem.prepareOpen(modalEl);
    }, true);

    document.addEventListener('show.bs.modal', function (e) {
        window.RaModalSystem.prepareOpen(e.target);
    });

    document.addEventListener('hidden.bs.modal', function () {
        window.setTimeout(function () {
            window.RaModalSystem.cleanupChrome();
        }, 20);
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

    // --- Lightweight Notification Center ---
    (function initNotificationCenter() {
        var badgeEl = document.getElementById('raNotifBadge');
        var trigger = document.getElementById('raNotifTrigger');
        var panel = document.getElementById('raNotifPanel');
        var scrim = document.getElementById('raNotifScrim');
        var closeBtn = document.getElementById('raNotifClose');
        var list = document.getElementById('raNotifList');
        var empty = document.getElementById('raNotifEmpty');
        var markAllBtn = document.getElementById('raNotifMarkAll');
        var enableBrowserBtn = document.getElementById('raNotifEnableBrowser');
        if (!badgeEl || !trigger || !panel || !list) return;

        var pollTimer = null;
        var inFlight = null;
        var touchStartY = 0;
        var lastCount = parseInt(window.localStorage.getItem('ra-last-notif-count') || '0', 10) || 0;
        var lastNotifiedId = parseInt(window.localStorage.getItem('ra-last-browser-notif-id') || '0', 10) || 0;

        function getAntiForgeryToken() {
            var meta = document.querySelector('meta[name="request-verification-token"]');
            if (meta && meta.content) return meta.content;

            var input = document.querySelector('input[name="__RequestVerificationToken"]');
            return input ? input.value : '';
        }

        function postJson(url, body) {
            var token = getAntiForgeryToken();
            if (!token) return Promise.resolve(null);
            if (inFlight) inFlight.abort();
            inFlight = new AbortController();
            return fetch(url, {
                method: 'POST',
                credentials: 'same-origin',
                headers: {
                    'X-CSRF-TOKEN': token,
                    'Accept': 'application/json',
                    'Content-Type': 'application/x-www-form-urlencoded; charset=UTF-8'
                },
                body: body || '',
                signal: inFlight.signal
            }).then(function (r) {
                return r.ok ? r.json() : null;
            }).catch(function (err) {
                if (err && err.name === 'AbortError') return null;
                return null;
            }).finally(function () {
                inFlight = null;
            });
        }

        function updateBadge(count) {
            var n = parseInt(count, 10) || 0;
            badgeEl.textContent = n > 99 ? '99+' : n;
            badgeEl.style.display = n > 0 ? 'inline-flex' : 'none';
            trigger.setAttribute('aria-label', n > 0 ? 'Abrir notificaciones, ' + n + ' sin leer' : 'Abrir notificaciones');
            window.localStorage.setItem('ra-last-notif-count', String(n));
        }

        function iconForType(type) {
            var t = (type || '').toLowerCase();
            if (t.indexOf('error') >= 0) return 'var(--ra-danger)';
            if (t.indexOf('advert') >= 0) return 'var(--ra-warning)';
            if (t.indexOf('exito') >= 0 || t.indexOf('éxito') >= 0) return 'var(--ra-success)';
            return 'var(--ra-primary)';
        }

        function renderNotifications(items) {
            list.replaceChildren();
            var notifications = Array.isArray(items) ? items : [];
            if (empty) empty.hidden = notifications.length !== 0;
            if (!notifications.length) return;

            notifications.forEach(function (item) {
                var btn = document.createElement('button');
                btn.type = 'button';
                btn.className = 'ra-notif-item ' + (item.leida ? 'read' : 'unread');
                btn.dataset.id = item.id;
                btn.dataset.href = item.enlace || '/Notificaciones';

                var dot = document.createElement('span');
                dot.className = 'ra-notif-dot';
                dot.style.background = item.leida ? 'var(--ra-border)' : iconForType(item.tipo);
                btn.appendChild(dot);

                var content = document.createElement('span');
                content.style.flex = '1';
                var title = document.createElement('span');
                title.className = 'ra-notif-title d-block';
                title.textContent = item.titulo || 'Notificación';
                var message = document.createElement('p');
                message.className = 'ra-notif-message';
                message.textContent = item.mensaje || '';
                var time = document.createElement('span');
                time.className = 'ra-notif-time d-block';
                time.textContent = item.relativa || item.fecha || '';
                content.appendChild(title);
                content.appendChild(message);
                content.appendChild(time);
                btn.appendChild(content);

                btn.addEventListener('click', function () {
                    var id = encodeURIComponent(btn.dataset.id || '');
                    postJson('/Notificaciones/MarcarLeidaJson', 'id=' + id).then(function (data) {
                        window.location.href = data && data.enlace ? data.enlace : btn.dataset.href;
                    });
                });
                list.appendChild(btn);
            });
        }

        function maybeNotifyBrowser(data) {
            if (!('Notification' in window) || Notification.permission !== 'granted') return;
            var items = data && Array.isArray(data.notifications) ? data.notifications : [];
            var firstUnread = items.find(function (n) { return !n.leida; });
            if (!firstUnread || firstUnread.id <= lastNotifiedId) return;
            lastNotifiedId = firstUnread.id;
            window.localStorage.setItem('ra-last-browser-notif-id', String(lastNotifiedId));
            new Notification(firstUnread.titulo || 'Nueva notificación', {
                body: firstUnread.mensaje || 'Tienes un aviso nuevo en Rescate Académico.',
                tag: 'rescate-academico-' + firstUnread.id
            });
        }

        function refreshCount() {
            return postJson('/Notificaciones/GetConteoNoLeidas').then(function (data) {
                if (!data) return;
                var count = parseInt(data.count, 10) || 0;
                if (count > lastCount && !document.hidden) {
                    refreshList(false).then(maybeNotifyBrowser);
                }
                lastCount = count;
                updateBadge(count);
            });
        }

        function refreshList(showLoading) {
            if (showLoading) list.innerHTML = '<div class="ra-notif-loading">Cargando notificaciones...</div>';
            return postJson('/Notificaciones/Recientes', 'take=8').then(function (data) {
                if (!data) return data;
                lastCount = parseInt(data.count, 10) || 0;
                updateBadge(lastCount);
                renderNotifications(data.notifications);
                return data;
            });
        }

        function scheduleNextPoll() {
            clearTimeout(pollTimer);
            var interval = document.hidden ? 120000 : (lastCount > 0 ? 30000 : 60000);
            pollTimer = setTimeout(function () {
                refreshCount().finally(scheduleNextPoll);
            }, interval);
        }

        function openPanel() {
            panel.classList.add('active');
            panel.setAttribute('aria-hidden', 'false');
            trigger.setAttribute('aria-expanded', 'true');
            if (scrim) { scrim.hidden = false; requestAnimationFrame(function () { scrim.classList.add('active'); }); }
            refreshList(true);
        }

        function closePanel() {
            panel.classList.remove('active');
            panel.setAttribute('aria-hidden', 'true');
            trigger.setAttribute('aria-expanded', 'false');
            if (scrim) {
                scrim.classList.remove('active');
                setTimeout(function () { scrim.hidden = true; }, 180);
            }
        }

        trigger.addEventListener('click', function () {
            panel.classList.contains('active') ? closePanel() : openPanel();
        });
        if (closeBtn) closeBtn.addEventListener('click', closePanel);
        if (scrim) scrim.addEventListener('click', closePanel);
        panel.addEventListener('touchstart', function (e) {
            touchStartY = e.touches && e.touches.length ? e.touches[0].clientY : 0;
        }, { passive: true });
        panel.addEventListener('touchend', function (e) {
            var endY = e.changedTouches && e.changedTouches.length ? e.changedTouches[0].clientY : touchStartY;
            if (touchStartY && endY - touchStartY > 70 && window.matchMedia('(max-width: 767.98px)').matches) {
                closePanel();
            }
            touchStartY = 0;
        }, { passive: true });
        document.addEventListener('keydown', function (e) {
            if (e.key === 'Escape' && panel.classList.contains('active')) closePanel();
        });
        document.addEventListener('visibilitychange', function () {
            if (!document.hidden) refreshCount();
            scheduleNextPoll();
        });
        window.addEventListener('pagehide', function () {
            clearTimeout(pollTimer);
            if (inFlight) inFlight.abort();
        });
        if (markAllBtn) {
            markAllBtn.addEventListener('click', function () {
                postJson('/Notificaciones/MarcarTodasLeidasJson').then(function () {
                    updateBadge(0);
                    refreshList(false);
                });
            });
        }
        if (enableBrowserBtn) {
            if (!('Notification' in window)) {
                enableBrowserBtn.hidden = true;
            } else if (Notification.permission === 'granted') {
                enableBrowserBtn.innerHTML = '<i class="bi bi-check2"></i> Avisos activos';
            } else {
                enableBrowserBtn.addEventListener('click', function () {
                    Notification.requestPermission().then(function (permission) {
                        if (permission === 'granted') {
                            enableBrowserBtn.innerHTML = '<i class="bi bi-check2"></i> Avisos activos';
                        }
                    });
                });
            }
        }

        refreshCount().finally(scheduleNextPoll);
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

    // --- Page content entrance handled purely in CSS (no JS dependency) ---

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
            window.RaModalSystem.prepareOpen(modalEl);

            // Show modal first for perceived speed
            var existing = bootstrap.Modal.getInstance(modalEl);
            if (existing) existing.dispose();

            var modal = new bootstrap.Modal(modalEl, { backdrop: true, keyboard: true, focus: true });
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

                var promedio = typeof data.promedioGlobal === 'number' ? data.promedioGlobal : parseFloat(data.promedioGlobal) || 0;
                var reprobadas = parseInt(data.materiasReprobadas, 10) || 0;
                var ausencias = parseInt(data.ausencias, 10) || 0;
                var ets = parseInt(data.etsPresentados, 10) || 0;
                var recursadas = parseInt(data.recursamientos, 10) || 0;
                var riesgo = data.riesgoAcademico || 'Verde';
                var identityLine = modalEl.querySelector('[data-qv="identityLine"]');
                if (identityLine) {
                    identityLine.textContent = fmt(data.carrera) + ' · ' + fmt(data.grupo) + ' · Boleta ' + fmt(data.matricula);
                }

                var fields = {
                    matricula: data.matricula,
                    carrera: fmt(data.carrera),
                    semestre: fmt(data.semestreActual) + '°',
                    grupo: fmt(data.grupo),
                    carga: (data.cargaAcademicaActual || 0) + ' materias',
                    actualizacion: fmt(data.fechaUltimaActualizacion),
                    reprobadas: reprobadas,
                    ausencias: ausencias,
                    ets: ets,
                    recursadas: recursadas
                };
                Object.keys(fields).forEach(function (key) {
                    var el = modalEl.querySelector('[data-qv="' + key + '"]');
                    if (el) el.textContent = fields[key];
                });

                var cosecoviCount = parseInt(data.cosecoviReportes, 10) || 0;
                var cosecoviOpen = parseInt(data.cosecoviAbiertos, 10) || 0;
                var cosecoviResumen = modalEl.querySelector('[data-qv="cosecoviResumen"]');
                var cosecoviDetalle = modalEl.querySelector('[data-qv="cosecoviDetalle"]');
                var cosecoviBadge = modalEl.querySelector('[data-qv="cosecoviBadge"]');
                if (cosecoviResumen) {
                    cosecoviResumen.textContent = cosecoviCount > 0
                        ? cosecoviCount + ' reporte' + (cosecoviCount === 1 ? '' : 's') + ' COSECOVI'
                        : 'Sin reportes disciplinarios';
                }
                if (cosecoviDetalle) {
                    cosecoviDetalle.textContent = cosecoviCount > 0
                        ? 'Ultimo: ' + fmt(data.cosecoviUltimoTipo) + ' · ' + fmt(data.cosecoviUltimaGravedad) + ' · ' + fmt(data.cosecoviUltimaFecha)
                        : '';
                }
                if (cosecoviBadge) {
                    cosecoviBadge.textContent = cosecoviOpen > 0 ? cosecoviOpen + ' abierto' + (cosecoviOpen === 1 ? '' : 's') : String(cosecoviCount);
                    cosecoviBadge.className = 'ra-badge ' + (cosecoviOpen > 0 ? 'ra-badge-warning' : 'ra-badge-neutral');
                }

                var promedioEl = modalEl.querySelector('[data-qv="promedio"]');
                if (promedioEl) {
                    promedioEl.textContent = promedio.toFixed(2);
                    promedioEl.style.color = colorByPromedio(promedio);
                }

                var diagnosticoEl = modalEl.querySelector('[data-qv="diagnostico"]');
                var nextStepEl = modalEl.querySelector('[data-qv="nextStep"]');
                var hasDisciplinary = cosecoviCount > 0;
                var flags = reprobadas + ets + recursadas;
                if (diagnosticoEl) {
                    if (riesgo === 'Rojo' || promedio < 6 || flags >= 2) {
                        diagnosticoEl.textContent = 'Atención prioritaria: desempeño académico comprometido';
                    } else if (riesgo === 'Amarillo' || promedio < 7.5 || ausencias >= 4 || flags > 0) {
                        diagnosticoEl.textContent = 'Seguimiento preventivo: señales que conviene atender';
                    } else if (hasDisciplinary) {
                        diagnosticoEl.textContent = 'Académicamente estable, con antecedente disciplinario';
                    } else {
                        diagnosticoEl.textContent = 'Trayectoria estable: sin alertas críticas';
                    }
                }
                if (nextStepEl) {
                    if (riesgo === 'Rojo' || promedio < 6 || flags >= 2) {
                        nextStepEl.textContent = 'Prioriza intervención, contacto con tutor legal y plan de mejora.';
                    } else if (riesgo === 'Amarillo' || promedio < 7.5 || ausencias >= 4 || flags > 0) {
                        nextStepEl.textContent = 'Revisa causas, agenda seguimiento y confirma materias sensibles.';
                    } else if (hasDisciplinary) {
                        nextStepEl.textContent = 'Consulta COSECOVI antes de decidir el siguiente acompañamiento.';
                    } else {
                        nextStepEl.textContent = 'Mantén monitoreo regular y revisa oportunidades disponibles.';
                    }
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
                ['profile', 'detalle', 'timeline', 'intervencion', 'plan', 'cosecovi'].forEach(function (key) {
                    var link = modalEl.querySelector('[data-qv-link="' + key + '"]');
                    if (!link) return;
                    if (cfg[key] && (key !== 'cosecovi' || cosecoviCount > 0)) {
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
