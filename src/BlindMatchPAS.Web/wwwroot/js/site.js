/**
 * Blind-Match PAS — site.js
 * Client-side UX enhancements
 */

document.addEventListener('DOMContentLoaded', function () {

    // ── Auto-dismiss alerts after 5 seconds ─────────────────────
    document.querySelectorAll('.alert.alert-success, .alert.alert-info').forEach(function (alert) {
        setTimeout(function () {
            var bsAlert = bootstrap.Alert.getOrCreateInstance(alert);
            if (bsAlert) bsAlert.close();
        }, 5000);
    });

    // ── Abstract character counter ───────────────────────────────
    var abstractEl = document.getElementById('projectAbstract');
    var charCount = document.getElementById('charCount');
    if (abstractEl && charCount) {
        var update = function () {
            var len = abstractEl.value.length;
            charCount.textContent = len;
            charCount.style.color = len < 50 ? '#ef4444' : len > 1800 ? '#f59e0b' : '#10b981';
        };
        abstractEl.addEventListener('input', update);
        update();
    }

    // ── Confirm before form submission (double-safety) ──────────
    document.querySelectorAll('[data-confirm]').forEach(function (el) {
        el.addEventListener('click', function (e) {
            if (!confirm(el.dataset.confirm)) {
                e.preventDefault();
            }
        });
    });

    // ── Active nav item highlighting ─────────────────────────────
    var currentPath = window.location.pathname.toLowerCase();
    document.querySelectorAll('.navbar-nav .nav-link').forEach(function (link) {
        var href = link.getAttribute('href');
        if (href && currentPath.startsWith(href.toLowerCase()) && href !== '/') {
            link.classList.add('active');
        }
    });

    // ── Textarea auto-resize ─────────────────────────────────────
    document.querySelectorAll('textarea.form-control').forEach(function (ta) {
        ta.addEventListener('input', function () {
            ta.style.height = 'auto';
            ta.style.height = Math.min(ta.scrollHeight, 400) + 'px';
        });
    });

    // ── Research area checkbox styling toggle ────────────────────
    document.querySelectorAll('input[type="checkbox"].form-check-input').forEach(function (cb) {
        var updateStyle = function () {
            var parent = cb.closest('.form-check[style]') || cb.closest('[data-area-card]');
            if (parent) {
                if (cb.checked) {
                    parent.style.borderColor = 'rgba(139,92,246,0.5)';
                    parent.style.background = 'rgba(139,92,246,0.08)';
                } else {
                    parent.style.borderColor = 'rgba(139,92,246,0.2)';
                    parent.style.background = '';
                }
            }
        };
        cb.addEventListener('change', updateStyle);
        updateStyle();
    });

});
