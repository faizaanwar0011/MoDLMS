/* ============================================================
   MoDLibrary - site.js
   ============================================================ */

// ── Toast notification utility ────────────────────────────────
function showToast(message, type = 'info') {
    const container = document.getElementById('notif-container')
                   || document.getElementById('remark-container');
    if (!container) return;

    const colorMap = {
        info:    'alert-info',
        success: 'alert-success',
        warning: 'alert-warning',
        danger:  'alert-danger'
    };

    const iconMap = {
        info:    'info-circle-fill',
        success: 'check-circle-fill',
        warning: 'exclamation-triangle-fill',
        danger:  'x-circle-fill'
    };

    const div = document.createElement('div');
    div.className = `alert ${colorMap[type] || 'alert-info'} alert-dismissible fade show shadow`;
    div.style.cssText = 'border-radius:12px;font-size:13px;margin-bottom:8px;';
    div.innerHTML = `
        <i class="bi bi-${iconMap[type] || 'info-circle-fill'} me-2"></i>
        ${message}
        <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
    `;
    container.appendChild(div);

    // Auto remove after 8 seconds
    setTimeout(() => {
        if (div.parentNode) {
            div.classList.remove('show');
            setTimeout(() => { if (div.parentNode) div.remove(); }, 300);
        }
    }, 8000);
}

// ── Auto-dismiss alerts after 5 seconds ──────────────────────
document.addEventListener('DOMContentLoaded', function () {
    document.querySelectorAll('.alert-dismissible').forEach(function (alert) {
        setTimeout(function () {
            const bsAlert = bootstrap.Alert.getOrCreateInstance(alert);
            if (bsAlert) bsAlert.close();
        }, 5000);
    });
});

// ── Confirm delete helper ─────────────────────────────────────
function confirmAction(message) {
    return confirm(message || 'Are you sure?');
}

// ── Table search filter ───────────────────────────────────────
function filterTable(inputId, tableId) {
    const input = document.getElementById(inputId);
    if (!input) return;
    input.addEventListener('input', function () {
        const q = this.value.toLowerCase();
        const rows = document.querySelectorAll('#' + tableId + ' tbody tr');
        rows.forEach(row => {
            row.style.display = row.textContent.toLowerCase().includes(q) ? '' : 'none';
        });
    });
}

// ── CNIC formatter (auto-insert dashes) ──────────────────────
document.addEventListener('DOMContentLoaded', function () {
    const cnicInput = document.querySelector('input[name="CNIC"]');
    if (cnicInput) {
        cnicInput.addEventListener('input', function () {
            let val = this.value.replace(/[^0-9]/g, '');
            if (val.length > 5 && val.length <= 12) {
                val = val.substring(0, 5) + '-' + val.substring(5);
            } else if (val.length > 12) {
                val = val.substring(0, 5) + '-' + val.substring(5, 12) + '-' + val.substring(12, 13);
            }
            this.value = val;
        });
    }
});

// ── Active nav highlight ──────────────────────────────────────
document.addEventListener('DOMContentLoaded', function () {
    const path = window.location.pathname.toLowerCase();
    document.querySelectorAll('.sidebar-link').forEach(link => {
        const href = link.getAttribute('href');
        if (href && path === href.toLowerCase()) {
            link.classList.add('active');
        }
    });
});
