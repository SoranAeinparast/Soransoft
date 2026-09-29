(function () {
    'use strict';

    function fireToast(icon, title) {
        if (typeof Swal === 'undefined' || !title) return;
        Swal.mixin({
            toast: true,
            position: 'top',
            showConfirmButton: false,
            timer: 2400,
            timerProgressBar: true,
            width: 'min(420px, calc(100vw - 2rem))',
            customClass: { popup: 'sn-swal-toast' }
        }).fire({ icon: icon, title: title });
    }

    function confirmAction(options, onConfirm) {
        if (typeof Swal === 'undefined') {
            onConfirm();
            return;
        }

        Swal.fire({
            icon: 'warning',
            title: options.title || 'تأیید عملیات',
            text: options.text || 'آیا از انجام این عملیات مطمئن هستید؟',
            showCancelButton: true,
            confirmButtonText: options.confirmText || 'بله، ادامه بده',
            cancelButtonText: 'انصراف',
            reverseButtons: true,
            focusCancel: true
        }).then(function (result) {
            if (result.isConfirmed) onConfirm();
        });
    }

    function submitForm(form, submitter) {
        form.dataset.swalPending = 'true';
        if (typeof form.requestSubmit === 'function' && submitter) {
            form.requestSubmit(submitter);
        } else {
            form.submit();
        }
    }

    document.addEventListener('DOMContentLoaded', function () {
        document.querySelectorAll('[data-swal-alert]').forEach(function (alert) {
            var icon = alert.dataset.swalIcon || 'info';
            var title = alert.dataset.swalTitle || alert.textContent.trim();
            if (typeof Swal === 'undefined') {
                alert.classList.remove('d-none');
                alert.classList.add('alert', icon === 'error' ? 'alert-danger' : 'alert-success');
                alert.textContent = title;
                return;
            }
            fireToast(icon, title);
            alert.remove();
        });

        document.querySelectorAll('form[data-swal-confirm]').forEach(function (form) {
            form.addEventListener('submit', function (event) {
                if (form.dataset.swalPending === 'true') return;
                event.preventDefault();
                confirmAction({
                    text: form.dataset.swalConfirm,
                    title: form.dataset.swalConfirmTitle
                }, function () { submitForm(form, null); });
            });
        });

        document.querySelectorAll('[data-swal-confirm]:not(form)').forEach(function (element) {
            element.addEventListener('click', function (event) {
                if (element.dataset.swalPending === 'true') return;
                event.preventDefault();
                var form = element.form;
                confirmAction({
                    text: element.dataset.swalConfirm,
                    title: element.dataset.swalConfirmTitle
                }, function () {
                    if (form) submitForm(form, element);
                    else if (element.href) window.location.href = element.href;
                });
            });
        });
    });
})();
