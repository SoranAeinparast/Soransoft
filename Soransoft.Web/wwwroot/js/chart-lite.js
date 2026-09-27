(function (window) {
    'use strict';

    function Chart(canvas, config) {
        this.canvas = canvas;
        this.config = config || {};
        this.draw();
        window.addEventListener('resize', this.draw.bind(this));
    }

    Chart.defaults = { font: { family: 'Tahoma' }, color: '#7a8a94' };

    Chart.prototype.draw = function () {
        var canvas = this.canvas;
        if (!canvas) return;
        var box = canvas.parentElement.getBoundingClientRect();
        var ratio = window.devicePixelRatio || 1;
        var width = Math.max(240, Math.floor(box.width));
        var height = Math.max(220, Math.floor(box.height || 320));
        canvas.width = width * ratio;
        canvas.height = height * ratio;
        canvas.style.width = width + 'px';
        canvas.style.height = height + 'px';
        var ctx = canvas.getContext('2d');
        ctx.scale(ratio, ratio);
        ctx.clearRect(0, 0, width, height);
        ctx.font = '12px ' + Chart.defaults.font.family;
        ctx.fillStyle = Chart.defaults.color;
        var type = this.config.type || 'line';
        if (type === 'doughnut') drawDoughnut(ctx, width, height, this.config.data || {});
        else drawCartesian(ctx, width, height, this.config.data || {}, this.config.options || {}, type);
    };

    function valuesOf(dataset, labels) {
        return labels.map(function (_, index) { return Number(dataset.data && dataset.data[index]) || 0; });
    }

    function drawLegend(ctx, datasets, width, y) {
        var x = width - 14;
        ctx.textAlign = 'right';
        datasets.slice().reverse().forEach(function (dataset) {
            var label = dataset.label || '';
            var textWidth = ctx.measureText(label).width;
            x -= textWidth;
            ctx.fillStyle = dataset.borderColor || dataset.backgroundColor || '#64d8cb';
            ctx.fillRect(x - 14, y - 9, 9, 9);
            ctx.fillStyle = Chart.defaults.color;
            ctx.fillText(label, x - 20, y);
            x -= textWidth + 38;
        });
    }

    function drawCartesian(ctx, width, height, data, options, chartType) {
        var labels = data.labels || [];
        var datasets = data.datasets || [];
        var left = 42, right = 16, top = 16, bottom = 42;
        var chartWidth = width - left - right;
        var chartHeight = height - top - bottom - 26;
        var all = [];
        datasets.forEach(function (dataset) { all = all.concat(valuesOf(dataset, labels)); });
        var max = Math.max(1, Math.max.apply(Math, all.length ? all : [1]));
        var horizontal = options.indexAxis === 'y';
        ctx.strokeStyle = '#d9e2ea';
        ctx.lineWidth = 1;
        for (var g = 0; g <= 4; g++) {
            var gy = top + chartHeight - (chartHeight * g / 4);
            ctx.beginPath(); ctx.moveTo(left, gy); ctx.lineTo(width - right, gy); ctx.stroke();
            ctx.fillStyle = Chart.defaults.color; ctx.textAlign = 'left';
            ctx.fillText(formatNumber(max * g / 4), 3, gy + 4);
        }
        if (horizontal) {
            var row = chartHeight / Math.max(1, labels.length);
            labels.forEach(function (label, index) {
                var x = left;
                datasets.forEach(function (dataset) {
                    var value = valuesOf(dataset, labels)[index];
                    var barWidth = chartWidth * value / max;
                    ctx.fillStyle = dataset.backgroundColor || dataset.borderColor || '#64d8cb';
                    ctx.fillRect(x, top + index * row + row * .2, barWidth, row * .6);
                    x += barWidth;
                });
                ctx.fillStyle = Chart.defaults.color; ctx.textAlign = 'right';
                ctx.fillText(label, width - 4, top + index * row + row * .58);
            });
        } else {
            var slot = chartWidth / Math.max(1, labels.length);
            datasets.forEach(function (dataset, datasetIndex) {
                var values = valuesOf(dataset, labels);
                var isLine = dataset.type === 'line' || chartType === 'line';
                ctx.strokeStyle = dataset.borderColor || '#64d8cb';
                ctx.fillStyle = dataset.backgroundColor || ctx.strokeStyle;
                ctx.lineWidth = dataset.borderWidth || 2;
                if (isLine) {
                    ctx.beginPath();
                    values.forEach(function (value, index) {
                        var px = left + slot * (index + .5);
                        var py = top + chartHeight - chartHeight * value / max;
                        index ? ctx.lineTo(px, py) : ctx.moveTo(px, py);
                    });
                    ctx.stroke();
                } else {
                    var barWidth = Math.max(3, slot * .65 / Math.max(1, datasets.length));
                    values.forEach(function (value, index) {
                        var px = left + slot * (index + .15) + datasetIndex * barWidth;
                        var py = top + chartHeight - chartHeight * value / max;
                        ctx.fillRect(px, py, barWidth - 1, top + chartHeight - py);
                    });
                }
            });
            labels.forEach(function (label, index) {
                if (index % Math.max(1, Math.ceil(labels.length / 8)) !== 0) return;
                ctx.fillStyle = Chart.defaults.color; ctx.textAlign = 'center';
                ctx.fillText(label, left + slot * (index + .5), top + chartHeight + 19);
            });
        }
        drawLegend(ctx, datasets, width, height - 7);
    }

    function drawDoughnut(ctx, width, height, data) {
        var values = (data.datasets && data.datasets[0] && data.datasets[0].data || []).map(Number);
        var colors = data.datasets && data.datasets[0] && data.datasets[0].backgroundColor || ['#64d8cb', '#86b8ff', '#f4c66a', '#ff7c87'];
        var total = values.reduce(function (sum, value) { return sum + (value || 0); }, 0) || 1;
        var radius = Math.min(width, height) * .31;
        var cx = width / 2, cy = height / 2 - 12, start = -Math.PI / 2;
        values.forEach(function (value, index) {
            var end = start + Math.PI * 2 * value / total;
            ctx.beginPath(); ctx.moveTo(cx, cy); ctx.arc(cx, cy, radius, start, end); ctx.closePath();
            ctx.fillStyle = colors[index % colors.length]; ctx.fill(); start = end;
        });
        ctx.beginPath(); ctx.fillStyle = '#fff'; ctx.arc(cx, cy, radius * .56, 0, Math.PI * 2); ctx.fill();
        drawLegend(ctx, (data.labels || []).map(function (label, index) { return { label: label, backgroundColor: colors[index % colors.length] }; }), width, height - 7);
    }

    function formatNumber(value) {
        var text = Math.round(value).toLocaleString('en-US');
        return window.SoransoftPersian ? window.SoransoftPersian.toPersianDigits(text) : text;
    }

    window.Chart = Chart;
})(window);
