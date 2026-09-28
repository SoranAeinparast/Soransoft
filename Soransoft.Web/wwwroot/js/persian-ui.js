(function () {
    'use strict';

    var persianDigits = '۰۱۲۳۴۵۶۷۸۹';
    var latinDigits = '0123456789';

    function normalizeDigits(value) {
        return String(value || '').replace(/[۰-۹]/g, function (digit) {
            return String(persianDigits.indexOf(digit));
        }).replace(/[٠-٩]/g, function (digit) {
            return String('٠١٢٣٤٥٦٧٨٩'.indexOf(digit));
        });
    }

    function toPersianDigits(value) {
        return String(value || '').replace(/[0-9]/g, function (digit) {
            return persianDigits[latinDigits.indexOf(digit)];
        });
    }

    function gregorianToJalali(gy, gm, gd) {
        var gDays = [0, 31, 59, 90, 120, 151, 181, 212, 243, 273, 304, 334];
        var jy = gy <= 1600 ? 0 : 979;
        gy -= gy <= 1600 ? 621 : 1600;
        var gy2 = gm > 2 ? gy + 1 : gy;
        var days = 365 * gy + Math.floor((gy2 + 3) / 4) - Math.floor((gy2 + 99) / 100) + Math.floor((gy2 + 399) / 400) - 80 + gd + gDays[gm - 1];
        jy += 33 * Math.floor(days / 12053);
        days %= 12053;
        jy += 4 * Math.floor(days / 1461);
        days %= 1461;
        if (days > 365) {
            jy += Math.floor((days - 1) / 365);
            days = (days - 1) % 365;
        }
        var jm = days < 186 ? 1 + Math.floor(days / 31) : 7 + Math.floor((days - 186) / 30);
        var jd = 1 + (days < 186 ? days % 31 : (days - 186) % 30);
        return [jy, jm, jd];
    }

    function jalaliToGregorian(jy, jm, jd) {
        var gy = jy <= 979 ? 621 : 1600;
        jy -= jy <= 979 ? 0 : 979;
        var days = 365 * jy + Math.floor(jy / 33) * 8 + Math.floor((jy % 33 + 3) / 4) + 78 + jd + (jm < 7 ? (jm - 1) * 31 : (jm - 7) * 30 + 186);
        gy += 400 * Math.floor(days / 146097);
        days %= 146097;
        if (days > 36524) {
            gy += 100 * Math.floor(--days / 36524);
            days %= 36524;
            if (days >= 365) days++;
        }
        gy += 4 * Math.floor(days / 1461);
        days %= 1461;
        if (days > 365) {
            gy += Math.floor((days - 1) / 365);
            days = (days - 1) % 365;
        }
        var gd = days + 1;
        var leap = (gy % 4 === 0 && gy % 100 !== 0) || gy % 400 === 0;
        var monthDays = [31, leap ? 29 : 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31];
        var gm = 1;
        while (gm <= 12 && gd > monthDays[gm - 1]) gd -= monthDays[gm++ - 1];
        return [gy, gm, gd];
    }

    function parseDate(value) {
        var raw = normalizeDigits(value).trim();
        if (!raw) return null;
        var datePart = raw.split(/[T ]/)[0].replace(/[.-]/g, '/');
        var parts = datePart.split('/').map(Number);
        if (parts.length !== 3 || parts.some(function (part) { return !Number.isFinite(part); })) return null;

        var year = parts[0];
        var month = parts[1];
        var day = parts[2];
        if (year >= 1700) {
            var gregorianDate = new Date(Date.UTC(year, month - 1, day));
            return gregorianDate.getUTCFullYear() === year && gregorianDate.getUTCMonth() === month - 1 && gregorianDate.getUTCDate() === day
                ? [year, month, day]
                : null;
        }

        if (year < 1200 || year > 1600 || month < 1 || month > 12 || day < 1 || day > jalaliDaysInMonth(year, month)) return null;
        return jalaliToGregorian(year, month, day);
    }

    function toIso(value) {
        var parsed = parseDate(value);
        return parsed ? parsed.map(function (part) { return String(part).padStart(2, '0'); }).join('-') : '';
    }

    function splitDateTime(value) {
        var parts = normalizeDigits(value).trim().split(/[T ]/);
        var date = parseDate(parts[0]);
        if (!date) return null;
        var time = (parts[1] || '').match(/^(\d{1,2}):(\d{2})(?::(\d{2}))?$/);
        return { date: date, time: time ? time[1].padStart(2, '0') + ':' + time[2] + (time[3] ? ':' + time[3] : '') : '' };
    }

    function showJalaliDateTime(input) {
        var parsed = splitDateTime(input.value);
        if (!parsed || parsed.date[0] < 1700) return;
        var jalali = gregorianToJalali(parsed.date[0], parsed.date[1], parsed.date[2]);
        input.value = toPersianDigits(jalali.map(function (part) { return String(part).padStart(2, '0'); }).join('/') + (parsed.time ? ' ' + parsed.time : ''));
    }

    function toIsoDateTime(value) {
        var parsed = splitDateTime(value);
        if (!parsed) return '';
        var date = parsed.date.map(function (part) { return String(part).padStart(2, '0'); }).join('-');
        return date + (parsed.time ? 'T' + parsed.time : '');
    }

    function showJalali(input) {
        var parsed = parseDate(input.value);
        if (parsed && parsed[0] >= 1700) {
            var jalali = gregorianToJalali(parsed[0], parsed[1], parsed[2]);
            input.value = toPersianDigits(jalali.map(function (part) { return String(part).padStart(2, '0'); }).join('/'));
        }
    }

    var jalaliMonths = [
        'فروردین', 'اردیبهشت', 'خرداد', 'تیر', 'مرداد', 'شهریور',
        'مهر', 'آبان', 'آذر', 'دی', 'بهمن', 'اسفند'
    ];
    var jalaliWeekdays = ['شنبه', 'یکشنبه', 'دوشنبه', 'سه‌شنبه', 'چهارشنبه', 'پنجشنبه', 'جمعه'];
    var activeDatePicker = null;

    function jalaliDaysInMonth(year, month) {
        if (month <= 6) return 31;
        if (month <= 11) return 30;
        var start = jalaliToGregorian(year, month, 1);
        var next = jalaliToGregorian(year + 1, 1, 1);
        return Math.round((Date.UTC(next[0], next[1] - 1, next[2]) - Date.UTC(start[0], start[1] - 1, start[2])) / 86400000);
    }

    function closeDatePicker() {
        if (!activeDatePicker) return;
        activeDatePicker.node.remove();
        activeDatePicker = null;
    }

    function setupPersianDatePicker(input) {
        if (input.dataset.persianDateReady === 'true') return;
        input.dataset.persianDateReady = 'true';
        var isDateTime = input.hasAttribute('data-persian-datetime');
        input.inputMode = 'numeric';
        input.autocomplete = 'off';
        input.placeholder = input.placeholder || (isDateTime ? '۱۴۰۴/۰۱/۰۱ ۱۲:۰۰' : '۱۴۰۴/۰۱/۰۱');
        input.dir = 'ltr';
        if (isDateTime) showJalaliDateTime(input); else showJalali(input);

        var wrapper = document.createElement('div');
        wrapper.className = 'sn-persian-date-control';
        input.parentNode.insertBefore(wrapper, input);
        wrapper.appendChild(input);

        var button = document.createElement('button');
        button.type = 'button';
        button.className = 'sn-persian-date-button';
        button.setAttribute('aria-label', 'انتخاب تاریخ');
        button.innerHTML = '<i class="bi bi-calendar3"></i>';
        wrapper.appendChild(button);

        var picker = document.createElement('div');
        picker.className = 'sn-persian-datepicker';
        picker.setAttribute('dir', 'rtl');
        var current = new Date();
        var currentJalali = gregorianToJalali(current.getFullYear(), current.getMonth() + 1, current.getDate());
        var selected = parseDate(input.value);
        var selectedJalali = selected ? gregorianToJalali(selected[0], selected[1], selected[2]) : null;
        var state = {
            year: selectedJalali ? selectedJalali[0] : currentJalali[0],
            month: selectedJalali ? selectedJalali[1] : currentJalali[1]
        };

        function positionPicker() {
            if (!activeDatePicker) return;
            var rect = input.getBoundingClientRect();
            picker.style.top = Math.round(rect.bottom + 6) + 'px';
            var right = Math.max(8, Math.round(window.innerWidth - rect.right));
            if (right + 292 > window.innerWidth - 8) right = 8;
            picker.style.right = right + 'px';
        }

        function renderPicker() {
            picker.innerHTML = '';
            var header = document.createElement('div');
            header.className = 'sn-persian-datepicker-header';

            var previous = document.createElement('button');
            previous.type = 'button';
            previous.className = 'sn-persian-datepicker-nav';
            previous.setAttribute('aria-label', 'ماه قبل');
            previous.innerHTML = '<i class="bi bi-chevron-right"></i>';
            previous.addEventListener('click', function () {
                state.month--;
                if (state.month < 1) { state.month = 12; state.year--; }
                renderPicker();
            });

            var title = document.createElement('strong');
            title.textContent = jalaliMonths[state.month - 1] + ' ' + toPersianDigits(String(state.year));

            var next = document.createElement('button');
            next.type = 'button';
            next.className = 'sn-persian-datepicker-nav';
            next.setAttribute('aria-label', 'ماه بعد');
            next.innerHTML = '<i class="bi bi-chevron-left"></i>';
            next.addEventListener('click', function () {
                state.month++;
                if (state.month > 12) { state.month = 1; state.year++; }
                renderPicker();
            });

            header.appendChild(previous);
            header.appendChild(title);
            header.appendChild(next);
            picker.appendChild(header);

            var weekdays = document.createElement('div');
            weekdays.className = 'sn-persian-datepicker-weekdays';
            jalaliWeekdays.forEach(function (weekday) {
                var cell = document.createElement('span');
                cell.textContent = weekday;
                weekdays.appendChild(cell);
            });
            picker.appendChild(weekdays);

            var grid = document.createElement('div');
            grid.className = 'sn-persian-datepicker-grid';
            var firstGregorian = jalaliToGregorian(state.year, state.month, 1);
            var offset = (new Date(Date.UTC(firstGregorian[0], firstGregorian[1] - 1, firstGregorian[2])).getUTCDay() + 1) % 7;
            for (var blank = 0; blank < offset; blank++) grid.appendChild(document.createElement('span'));

            var days = jalaliDaysInMonth(state.year, state.month);
            for (var day = 1; day <= days; day++) {
                var dayButton = document.createElement('button');
                dayButton.type = 'button';
                dayButton.className = 'sn-persian-datepicker-day';
                dayButton.textContent = toPersianDigits(String(day));
                if (selectedJalali && selectedJalali[0] === state.year && selectedJalali[1] === state.month && selectedJalali[2] === day) {
                    dayButton.classList.add('is-selected');
                }
                (function (selectedDay) {
                    dayButton.addEventListener('click', function () {
                        var dateValue = String(state.year).padStart(4, '0') + '/' + String(state.month).padStart(2, '0') + '/' + String(selectedDay).padStart(2, '0');
                        var currentDateTime = isDateTime ? splitDateTime(input.value) : null;
                        input.value = toPersianDigits(dateValue) + (currentDateTime && currentDateTime.time ? ' ' + currentDateTime.time : '');
                        input.dispatchEvent(new Event('change', { bubbles: true }));
                        closeDatePicker();
                    });
                })(day);
                grid.appendChild(dayButton);
            }
            picker.appendChild(grid);

            var footer = document.createElement('div');
            footer.className = 'sn-persian-datepicker-footer';
            var clear = document.createElement('button');
            clear.type = 'button';
            clear.className = 'btn btn-sm btn-link';
            clear.textContent = 'پاک کردن';
            clear.addEventListener('click', function () {
                input.value = '';
                input.dispatchEvent(new Event('change', { bubbles: true }));
                closeDatePicker();
            });
            footer.appendChild(clear);
            picker.appendChild(footer);
        }

        function openPicker(event) {
            event.preventDefault();
            event.stopPropagation();
            if (activeDatePicker && activeDatePicker.node === picker) {
                closeDatePicker();
                return;
            }
            closeDatePicker();
            var selected = parseDate(input.value);
            selectedJalali = selected ? gregorianToJalali(selected[0], selected[1], selected[2]) : null;
            state.year = selectedJalali ? selectedJalali[0] : currentJalali[0];
            state.month = selectedJalali ? selectedJalali[1] : currentJalali[1];
            activeDatePicker = { node: picker, wrapper: wrapper };
            document.body.appendChild(picker);
            renderPicker();
            positionPicker();
        }

        button.addEventListener('click', openPicker);
        input.addEventListener('click', openPicker);
        input.addEventListener('keydown', function (event) {
            if (event.key === 'Enter' || event.key === 'ArrowDown') openPicker(event);
        });
        input.addEventListener('blur', function () {
            if (isDateTime) showJalaliDateTime(input); else showJalali(input);
        });
        window.addEventListener('resize', positionPicker);
        window.addEventListener('scroll', positionPicker, true);
    }

    document.querySelectorAll('input[data-persian-date], input[data-persian-datetime]').forEach(setupPersianDatePicker);
    if (window.MutationObserver) {
        new MutationObserver(function (mutations) {
            mutations.forEach(function (mutation) {
                mutation.addedNodes.forEach(function (node) {
                    if (node.nodeType !== 1) return;
                    if (node.matches && node.matches('input[data-persian-date], input[data-persian-datetime]')) setupPersianDatePicker(node);
                    node.querySelectorAll && node.querySelectorAll('input[data-persian-date], input[data-persian-datetime]').forEach(setupPersianDatePicker);
                });
            });
        }).observe(document.body, { childList: true, subtree: true });
    }

    document.addEventListener('click', function (event) {
        if (activeDatePicker && !activeDatePicker.wrapper.contains(event.target) && !activeDatePicker.node.contains(event.target)) {
            closeDatePicker();
        }
    });
    document.addEventListener('keydown', function (event) {
        if (event.key === 'Escape') closeDatePicker();
    });

    function setupWebsiteToggle() {
        var websiteUrl = document.querySelector('[data-website-url]');
        var options = document.querySelectorAll('input[data-website-option]');
        if (!websiteUrl || !options.length) return;

        function updateWebsiteState() {
            var selected = document.querySelector('input[data-website-option]:checked');
            var enabled = selected && selected.value === 'true';
            websiteUrl.disabled = !enabled;
            if (!enabled) websiteUrl.value = '';
        }

        options.forEach(function (option) { option.addEventListener('change', updateWebsiteState); });
        updateWebsiteState();
    }

    function normalizeLocation(value) {
        return String(value || '').trim().replace(/[يى]/g, 'ی').replace(/ك/g, 'ک').replace(/\u200c/g, '').replace(/\s+/g, ' ');
    }

    function setupIranLocations() {
        var provinceSelect = document.querySelector('select[data-iran-province]');
        var citySelect = document.querySelector('select[data-iran-city]');
        if (!provinceSelect || !citySelect) return;

        var locationsUrl = provinceSelect.dataset.locationsUrl || '/data/iran-locations.json';
        var selectedProvince = provinceSelect.dataset.selected || provinceSelect.value || '';
        var selectedCity = citySelect.dataset.selected || citySelect.value || '';

        function addOption(select, value, text, selected) {
            var option = document.createElement('option');
            option.value = value;
            option.textContent = text;
            option.selected = selected;
            select.appendChild(option);
        }

        function fillCities(province, cityValue) {
            citySelect.innerHTML = '';
            if (!province) {
                addOption(citySelect, '', 'ابتدا استان را انتخاب کنید', true);
                citySelect.disabled = true;
                return;
            }

            addOption(citySelect, '', 'انتخاب شهرستان / شهر', !cityValue);
            province.cities.forEach(function (city) {
                addOption(citySelect, city, city, normalizeLocation(city) === normalizeLocation(cityValue));
            });
            if (cityValue && !province.cities.some(function (city) { return normalizeLocation(city) === normalizeLocation(cityValue); })) {
                addOption(citySelect, cityValue, cityValue, true);
            }
            citySelect.disabled = false;
        }

        fetch(locationsUrl, { credentials: 'same-origin' })
            .then(function (response) {
                if (!response.ok) throw new Error('Unable to load locations');
                return response.json();
            })
            .then(function (locations) {
                provinceSelect.innerHTML = '';
                addOption(provinceSelect, '', 'انتخاب استان', !selectedProvince);
                locations.forEach(function (location) {
                    addOption(provinceSelect, location.province, location.province, normalizeLocation(location.province) === normalizeLocation(selectedProvince));
                });
                var selected = locations.find(function (location) { return normalizeLocation(location.province) === normalizeLocation(provinceSelect.value); });
                fillCities(selected, selectedCity);
                provinceSelect.addEventListener('change', function () {
                    var next = locations.find(function (location) { return location.province === provinceSelect.value; });
                    fillCities(next, '');
                });
            })
            .catch(function () {
                provinceSelect.disabled = false;
                citySelect.disabled = false;
            });
    }

    setupWebsiteToggle();
    setupIranLocations();

    document.querySelectorAll('form').forEach(function (form) {
        form.addEventListener('submit', function () {
            form.querySelectorAll('input[data-persian-date]').forEach(function (input) {
                var iso = toIso(input.value);
                if (iso) input.value = iso;
            });
            form.querySelectorAll('input[data-persian-datetime]').forEach(function (input) {
                var iso = toIsoDateTime(input.value);
                if (iso) input.value = iso;
            });
        });
    });

    var walker = document.createTreeWalker(document.body, NodeFilter.SHOW_TEXT);
    var node;
    while ((node = walker.nextNode())) {
        if (!node.parentElement || /^(SCRIPT|STYLE|TEXTAREA|INPUT|CODE)$/i.test(node.parentElement.tagName)) continue;
        node.nodeValue = toPersianDigits(node.nodeValue);
    }

    window.SoransoftPersian = { toPersianDigits: toPersianDigits, toIso: toIso, toIsoDateTime: toIsoDateTime };
})();
