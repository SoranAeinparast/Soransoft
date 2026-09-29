// ===== Soransoft admin scripts =====
(function () {
    'use strict';
    document.addEventListener('DOMContentLoaded', function () {
        var burger = document.getElementById('adminBurger');
        var sidebar = document.querySelector('.sn-admin-sidebar');
        var closeButton = document.getElementById('adminSidebarClose');
        var backdrop = document.getElementById('adminSidebarBackdrop');
        if (burger && sidebar) {
            function setSidebarOpen(isOpen) {
                sidebar.classList.toggle('show', isOpen);
                document.body.classList.toggle('sn-sidebar-open', isOpen);
            }

            burger.addEventListener('click', function () {
                setSidebarOpen(!sidebar.classList.contains('show'));
            });
            if (closeButton) closeButton.addEventListener('click', function () { setSidebarOpen(false); });
            if (backdrop) backdrop.addEventListener('click', function () { setSidebarOpen(false); });
            sidebar.querySelectorAll('a').forEach(function (link) {
                link.addEventListener('click', function () { setSidebarOpen(false); });
            });
            document.addEventListener('keydown', function (event) {
                if (event.key === 'Escape') setSidebarOpen(false);
            });
        }

        initRichTextEditors();
    });

    // ---------- ویرایشگر متن پیشرفته (TinyMCE) ----------
    function initRichTextEditors() {
        var editors = document.querySelectorAll('textarea.sn-tinymce');
        if (!editors.length) return;
        if (typeof tinymce === 'undefined') {
            console.warn('TinyMCE بارگذاری نشده است.');
            return;
        }

        var csrf = (document.querySelector('meta[name="csrf-token"]') || {}).content || '';

        editors.forEach(function (el) {
            tinymce.init({
                target: el,
                language: 'fa',
                directionality: 'rtl',
                height: parseInt(el.dataset.editorHeight || '420', 10),
                menubar: 'file edit view insert format table',
                plugins: 'advlist autolink lists link image charmap preview anchor ' +
                    'searchreplace visualblocks code fullscreen ' +
                    'insertdatetime media table help wordcount',
                toolbar: 'undo redo | blocks | bold italic underline strikethrough | ' +
                    'forecolor backcolor | alignleft aligncenter alignright alignjustify | ' +
                    'bullist numlist outdent indent | link image media table | ' +
                    'removeformat searchreplace | code fullscreen',
                skin: 'oxide',
                content_css: 'default',
                content_style: 'body { font-family: Vazirmatn, Tahoma, sans-serif; font-size: 14px; }',
                // ---- آپلود تصویر داخل متن ----
                images_upload_url: '/Admin/Upload/Image',
                images_upload_handler: function (blobInfo) {
                    return new Promise(function (resolve, reject) {
                        var formData = new FormData();
                        formData.append('file', blobInfo.blob(), blobInfo.filename());
                        var xhr = new XMLHttpRequest();
                        xhr.open('POST', '/Admin/Upload/Image');
                        xhr.setRequestHeader('RequestVerificationToken', csrf);
                        xhr.withCredentials = true;
                        xhr.onload = function () {
                            if (xhr.status >= 200 && xhr.status < 300) {
                                try {
                                    var json = JSON.parse(xhr.responseText);
                                    if (json && json.location) { resolve(json.location); return; }
                                    reject(json && json.message ? json.message : 'پاسخ نامعتبر سرور');
                                } catch (e) {
                                    reject('پاسخ سرور قابل خواندن نیست');
                                }
                            } else if (xhr.status === 400) {
                                try { reject(JSON.parse(xhr.responseText).message || 'فایل مجاز نیست.'); }
                                catch (e) { reject('فایل مجاز نیست.'); }
                            } else {
                                reject('خطای سرور: ' + xhr.status);
                            }
                        };
                        xhr.onerror = function () { reject('خطای شبکه هنگام آپلود تصویر'); };
                        xhr.send(formData);
                    });
                },
                images_reuse_filename: false,
                automatic_uploads: true,
                file_picker_types: 'image',
                // ---- انتخابگر کتابخانه رسانه ----
                file_picker_callback: function (cb, value, meta) {
                    if (meta.filetype !== 'image') return;
                    openMediaLibraryPicker(function (url, alt) {
                        cb(url, { alt: alt || '' });
                    });
                },
                // ---- چسباندن متن از Word / وب بدون استایل‌های آشفته ----
                paste_as_text: false,
                paste_data_images: true,
                // ---- خروجی تمیز HTML ----
                convert_urls: false,
                relative_urls: false,
                remove_script_host: false,
                entity_encoding: 'raw',
                branding: false,
                promotion: false,
                license_key: 'gpl',
                setup: function (editor) {
                    // همگام‌سازی مطمئن محتوا با textarea پیش از Submit
                    var form = editor.getElement().form;
                    if (form) {
                        form.addEventListener('submit', function () {
                            if (tinymce.get(editor.id)) editor.save();
                        });
                    }
                }
            });
        });
    }

    // ---------- انتخابگر کتابخانه رسانه (مودال Bootstrap) ----------
    function openMediaLibraryPicker(onPick) {
        var modalEl = document.getElementById('snMediaPickerModal');
        if (!modalEl || typeof bootstrap === 'undefined') {
            console.warn('مودال کتابخانه رسانه در دسترس نیست.');
            return;
        }

        var modal = bootstrap.Modal.getOrCreateInstance(modalEl);
        var grid = modalEl.querySelector('#snMediaPickerGrid');
        var uploadInput = modalEl.querySelector('#snMediaPickerUpload');
        var status = modalEl.querySelector('#snMediaPickerStatus');
        var currentUrl = null; // آدرس انتخاب‌شده که بعد از بسته‌شدن مودال به TinyMCE برمی‌گردد

        function renderFiles(files) {
            grid.innerHTML = '';
            if (!files.length) {
                grid.innerHTML = '<p class="text-muted small my-3 w-100 text-center">فایلی موجود نیست؛ از دکمه بالا آپلود کنید.</p>';
                return;
            }
            files.forEach(function (f) {
                if (!f.isImage) return;
                var btn = document.createElement('button');
                btn.type = 'button';
                btn.className = 'sn-media-pick';
                btn.title = f.fileName;
                btn.innerHTML = '<img src="' + f.url + '" alt="' + (f.fileName || '') + '" loading="lazy" />';
                btn.addEventListener('click', function () {
                    currentUrl = f.url;
                    modal.hide();
                });
                grid.appendChild(btn);
            });
        }

        function load() {
            status.textContent = 'در حال دریافت…';
            fetch('/Admin/MediaLibrary/List', { credentials: 'same-origin' })
                .then(function (r) { return r.ok ? r.json() : Promise.reject(r.status); })
                .then(function (data) {
                    status.textContent = '';
                    renderFiles(data.files || []);
                })
                .catch(function () { status.textContent = 'خطا در دریافت فهرست فایل‌ها'; });
        }

        uploadInput.onchange = function () {
            var file = uploadInput.files && uploadInput.files[0];
            if (!file) return;
            var fd = new FormData();
            fd.append('file', file);
            status.textContent = 'در حال آپلود…';
            fetch('/Admin/Upload/Image', {
                method: 'POST',
                headers: { 'RequestVerificationToken': (document.querySelector('meta[name="csrf-token"]') || {}).content || '' },
                credentials: 'same-origin',
                body: fd
            })
                .then(function (r) { return r.json().then(function (j) { return { ok: r.ok, j: j }; }); })
                .then(function (res) {
                    if (res.ok && res.j.location) {
                        status.textContent = '';
                        uploadInput.value = '';
                        load();
                    } else {
                        status.textContent = res.j.message || 'آپلود ناموفق بود';
                    }
                })
                .catch(function () { status.textContent = 'خطای شبکه در آپلود'; });
        };

        // برگرداندن انتخاب به file_picker_callback پس از بستن مودال
        modalEl.addEventListener('hidden.bs.modal', function handler() {
            modalEl.removeEventListener('hidden.bs.modal', handler);
            if (currentUrl && onPick) {
                onPick(currentUrl, currentUrl.split('/').pop());
            }
        });

        currentUrl = null;
        load();
        modal.show();
    }
})();