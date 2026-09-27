// ===== Soransoft site scripts =====
(function () {
    'use strict';

    // ---- شمارش معکوس (نمایشی، مشابه مرجع) ----
    function startCountdown() {
        var daysEl = document.getElementById('cd-days');
        if (!daysEl) return;
        var hoursEl = document.getElementById('cd-hours');
        var minsEl = document.getElementById('cd-mins');
        // پایان: ۱۸ روز و ۱۴ ساعت و ۳۰ دقیقه بعد از هر بار باز شدن (نمایشی)
        var end = Date.now() + ((18 * 24 + 14) * 60 + 30) * 60 * 1000;
        end = Date.now() - (Date.now() % 60000) + ((18 * 24 + 14) * 60 + 30) * 60 * 1000;

        function pad(n) { return n < 10 ? '0' + n : '' + n; }

        function tick() {
            var diff = Math.max(0, end - Date.now());
            var mins = Math.floor(diff / 60000);
            daysEl.textContent = pad(Math.floor(mins / 1440));
            hoursEl.textContent = pad(Math.floor((mins % 1440) / 60));
            minsEl.textContent = pad(mins % 60);
        }
        tick();
        setInterval(tick, 30000);
    }

    // ---- توست موفقیت ----
    function hideToast() {
        var toast = document.getElementById('snToast');
        if (!toast) return;
        setTimeout(function () {
            toast.style.transition = 'opacity .5s';
            toast.style.opacity = '0';
            setTimeout(function () { toast.remove(); }, 500);
        }, 4000);
    }

    // ---- ارسال AJAX فرم مشاوره ----
    function bindConsultationForm() {
        var form = document.getElementById('consultationForm');
        if (!form) return;
        form.addEventListener('submit', function (e) {
            e.preventDefault();
            var result = document.getElementById('consultationResult');
            var btn = form.querySelector('button[type="submit"]');
            if (btn) { btn.disabled = true; }

            fetch(form.action, {
                method: 'POST',
                body: new FormData(form),
                headers: { 'X-Requested-With': 'XMLHttpRequest' }
            })
                .then(function (r) { return r.json(); })
                .then(function (data) {
                    if (result) {
                        result.innerHTML = '<div class="alert ' + (data.success ? 'alert-success' : 'alert-danger') + '">' + data.message + '</div>';
                    }
                    if (data.success) form.reset();
                })
                .catch(function () {
                    if (result) result.innerHTML = '<div class="alert alert-danger">خطا در ارتباط با سرور</div>';
                })
                .finally(function () { if (btn) btn.disabled = false; });
        });
    }

    // ---- تم روز/شب (مانند DayLogo/NightLogo مرجع) ----
    function bindThemeToggle() {
        var saved = null;
        try { saved = localStorage.getItem('sn-theme'); } catch (e) { }
        // پیش‌فرض: حالت روز (مانند مرجع)؛ فقط انتخاب صریح کاربر حالت شب را فعال می‌کند
        if (saved === 'dark') {
            document.body.classList.add('dark_mode');
        }
        function flip() {
            document.body.classList.toggle('dark_mode');
            try { localStorage.setItem('sn-theme', document.body.classList.contains('dark_mode') ? 'dark' : 'light'); } catch (e) { }
        }
        ['snThemeToggle', 'snThemeToggleM'].forEach(function (id) {
            var btn = document.getElementById(id);
            if (btn) btn.addEventListener('click', flip);
        });
    }

    // ---- نوار سبز چسبان مشاوره (مانند stick_message مرجع) ----
    function bindStickMessage() {
        var bar = document.getElementById('snStickMessage');
        if (!bar) return;
        var shown = false;
        window.addEventListener('scroll', function () {
            if (!shown && window.scrollY > 700) {
                shown = true;
                bar.classList.add('sn-show');
                setTimeout(function () { bar.classList.remove('sn-show'); }, 9000);
            }
        }, { passive: true });
    }

    // ---- پنهان‌کردن تصاویر خراب تا آیکون جایگزین نمایش داده شود ----
    function hideBrokenImages() {
        document.querySelectorAll('.sn-article-img img, .sn-portfolio-img img, .sn-team-avatar img').forEach(function (img) {
            function drop() {
                img.insertAdjacentHTML('afterend', '<i class="bi bi-journal-text"></i>');
                img.remove();
            }
            if (img.complete && img.naturalWidth === 0) { drop(); return; }
            img.addEventListener('error', drop);
        });
    }

    document.addEventListener('DOMContentLoaded', function () {
        hideBrokenImages();
        startCountdown();
        hideToast();
        bindConsultationForm();
        bindThemeToggle();
        bindStickMessage();
    });
})();
