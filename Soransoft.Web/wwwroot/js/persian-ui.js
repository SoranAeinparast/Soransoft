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
        var raw = normalizeDigits(value).trim().replace(/[.-]/g, '/');
        var parts = raw.split('/').map(Number);
        if (parts.length !== 3 || parts.some(function (part) { return !Number.isFinite(part); })) return null;
        if (parts[0] >= 1700) return [parts[0], parts[1], parts[2]];
        if (parts[0] < 1200 || parts[0] > 1600 || parts[1] < 1 || parts[1] > 12 || parts[2] < 1 || parts[2] > 31) return null;
        return jalaliToGregorian(parts[0], parts[1], parts[2]);
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

    document.querySelectorAll('input[data-persian-date]').forEach(function (input) {
        input.inputMode = 'numeric';
        input.placeholder = input.placeholder || '۱۴۰۴/۰۱/۰۱';
        input.dir = 'ltr';
        showJalali(input);
        input.addEventListener('blur', function () {
            showJalali(input);
        });
    });

    document.querySelectorAll('input[data-persian-datetime]').forEach(function (input) {
        input.inputMode = 'numeric';
        input.placeholder = input.placeholder || '۱۴۰۴/۰۱/۰۱ ۱۲:۰۰';
        input.dir = 'ltr';
        showJalaliDateTime(input);
        input.addEventListener('blur', function () {
            showJalaliDateTime(input);
        });
    });

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
