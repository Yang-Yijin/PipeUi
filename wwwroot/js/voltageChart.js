// ===== 公共绘图工具 =====
const COLORS = ['#00acc1', '#e53935', '#43a047', '#fb8c00', '#8e24aa', '#1e88e5', '#d81b60', '#00897b'];

function getCanvas() {
    const canvas = document.getElementById('chart');
    if (!canvas) { console.error('Canvas not found!'); return null; }
    canvas.width = canvas.width; // 强制重置
    return canvas;
}

function calcRange(seriesArray) {
    let minT = Infinity, maxT = -Infinity, minV = Infinity, maxV = -Infinity;
    for (const s of seriesArray) {
        const n = Math.min(s.times.length, s.values.length);
        for (let i = 0; i < n; i++) {
            const t = s.times[i], v = s.values[i];
            if (!Number.isFinite(t) || !Number.isFinite(v)) continue;
            // if (t < minT) minT = t; if (t > maxT) maxT = t;
            // if (v < minV) minV = v; if (v > maxV) maxV = v;
            // 简化写法：每个判断单独一行，更清晰
            if (t < minT) {
                minT = t;
            }
            if (t > maxT) {
                maxT = t;
            }
            if (v < minV) {
                minV = v;
            }
            if (v > maxV) {
                maxV = v;
            }
        }
    }
    if (!Number.isFinite(minT) || !Number.isFinite(minV)) return null;
    if (minT === maxT) maxT = minT + 1;
    if (minV === maxV) maxV = minV + 1;
    return { minT, maxT, minV, maxV };
}

function drawFrame(ctx, w, h, m, range, title, yLabel) {
    const { minT, maxT, minV, maxV } = range;

    // 背景
    ctx.fillStyle = '#fff';
    ctx.fillRect(0, 0, w, h);

    // grid
    ctx.strokeStyle = '#e0e0e0';
    ctx.lineWidth = 1;
    // for (let i = 0; i <= 5; i++) {
    //     const y = m.t + (h - m.t - m.b) * i / 5;
    //     ctx.beginPath(); ctx.moveTo(m.l, y); ctx.lineTo(w - m.r, y); ctx.stroke();
    //     const x = m.l + (w - m.l - m.r) * i / 5;
    //     ctx.beginPath(); ctx.moveTo(x, m.t); ctx.lineTo(x, h - m.b); ctx.stroke();
    // }
    // 简化写法：画布操作每步一行，更容易理解
    for (let i = 0; i <= 5; i++) {
        // 画横线
        var y = m.t + (h - m.t - m.b) * i / 5;
        ctx.beginPath();
        ctx.moveTo(m.l, y);
        ctx.lineTo(w - m.r, y);
        ctx.stroke();

        // 画竖线
        var x = m.l + (w - m.l - m.r) * i / 5;
        ctx.beginPath();
        ctx.moveTo(x, m.t);
        ctx.lineTo(x, h - m.b);
        ctx.stroke();
    }

    // axes
    ctx.strokeStyle = '#333';
    ctx.lineWidth = 2;
    ctx.beginPath();
    ctx.moveTo(m.l, m.t); ctx.lineTo(m.l, h - m.b); ctx.lineTo(w - m.r, h - m.b);
    ctx.stroke();

    const cx = (m.l + w - m.r) / 2;

    // title
    ctx.fillStyle = '#333';
    ctx.font = 'bold 18px Arial';
    ctx.textAlign = 'center';
    ctx.fillText(title, cx, 30);

    // Y label
    ctx.save();
    ctx.translate(25, h / 2);
    ctx.rotate(-Math.PI / 2);
    ctx.font = '14px Arial';
    ctx.fillText(yLabel, 0, 0);
    ctx.restore();

    // X label
    ctx.font = '14px Arial';
    ctx.textAlign = 'center';
    ctx.fillText('Time (s)', cx, h - 20);

    // Y ticks
    ctx.font = '12px Arial';
    ctx.textAlign = 'right';
    for (let i = 0; i <= 5; i++) {
        const v = minV + (maxV - minV) * (5 - i) / 5;
        const y = m.t + (h - m.t - m.b) * i / 5;
        ctx.fillText(v.toFixed(2), m.l - 10, y + 4);
    }

    // X ticks
    ctx.textAlign = 'center';
    for (let i = 0; i <= 5; i++) {
        const t = minT + (maxT - minT) * i / 5;
        const x = m.l + (w - m.l - m.r) * i / 5;
        ctx.fillText(t.toExponential(2), x, h - m.b + 25);
    }
}

// function drawLine(ctx, times, values, scaleX, scaleY, color) {
//     const n = Math.min(times.length, values.length);
//     ctx.strokeStyle = color;
//     ctx.lineWidth = 2;
//     ctx.beginPath();
//     let started = false;
//     for (let i = 0; i < n; i++) {
//         const t = times[i], v = values[i];
//         if (!Number.isFinite(t) || !Number.isFinite(v)) continue;
//         const x = scaleX(t), y = scaleY(v);
//         if (!started) { ctx.moveTo(x, y); started = true; }
//         else ctx.lineTo(x, y);
//     }
//     if (started) ctx.stroke();
// }
// 简化写法：变量拆开声明，if/else 带花括号，逻辑更清晰
function drawLine(ctx, times, values, scaleX, scaleY, color) {
    var n = Math.min(times.length, values.length);
    ctx.strokeStyle = color;
    ctx.lineWidth = 2;
    ctx.beginPath();
    var started = false;

    for (var i = 0; i < n; i++) {
        var t = times[i];
        var v = values[i];

        // 跳过无效数据
        if (!Number.isFinite(t) || !Number.isFinite(v)) {
            continue;
        }

        var x = scaleX(t);
        var y = scaleY(v);

        if (!started) {
            ctx.moveTo(x, y);
            started = true;
        } else {
            ctx.lineTo(x, y);
        }
    }

    if (started) {
        ctx.stroke();
    }
}

// ===== 单系列 =====
window.drawChart = function (times, values, title, yLabel) {
    const canvas = getCanvas();
    if (!canvas || !times || !values) return;
    if (Math.min(times.length, values.length) < 2) return;

    const ctx = canvas.getContext('2d');
    const w = canvas.width, h = canvas.height;
    const m = { t: 50, r: 50, b: 70, l: 80 };
    const range = calcRange([{ times, values }]);
    if (!range) return;

    var minT = range.minT;
    var maxT = range.maxT;
    var minV = range.minV;
    var maxV = range.maxV;

    // const scaleX = t => m.l + ((t - minT) / (maxT - minT)) * (w - m.l - m.r);
    // const scaleY = v => h - m.b - ((v - minV) / (maxV - minV)) * (h - m.t - m.b);
    // 简化写法：用普通函数代替箭头函数，更易读
    var plotWidth = w - m.l - m.r;
    var plotHeight = h - m.t - m.b;

    function scaleX(t) {
        return m.l + ((t - minT) / (maxT - minT)) * plotWidth;
    }
    function scaleY(v) {
        return h - m.b - ((v - minV) / (maxV - minV)) * plotHeight;
    }

    drawFrame(ctx, w, h, m, range, title || 'Voltage vs Time', yLabel || 'Voltage (V)');
    drawLine(ctx, times, values, scaleX, scaleY, COLORS[0]);
};

// ===== 多系列 =====
window.drawMultiChart = function (seriesArray, title, yLabel) {
    const canvas = getCanvas();
    if (!canvas || !seriesArray || seriesArray.length === 0) return;

    const ctx = canvas.getContext('2d');
    const w = canvas.width, h = canvas.height;
    const m = { t: 50, r: 150, b: 70, l: 80 };
    const range = calcRange(seriesArray);
    if (!range) return;

    // const { minT, maxT, minV, maxV } = range;
    // const scaleX = t => m.l + ((t - minT) / (maxT - minT)) * (w - m.l - m.r);
    // const scaleY = v => h - m.b - ((v - minV) / (maxV - minV)) * (h - m.t - m.b);
    // 简化写法：同 drawChart，用普通函数
    var minT = range.minT;
    var maxT = range.maxT;
    var minV = range.minV;
    var maxV = range.maxV;
    var plotWidth = w - m.l - m.r;
    var plotHeight = h - m.t - m.b;

    function scaleX(t) {
        return m.l + ((t - minT) / (maxT - minT)) * plotWidth;
    }
    function scaleY(v) {
        return h - m.b - ((v - minV) / (maxV - minV)) * plotHeight;
    }

    drawFrame(ctx, w, h, m, range, title || 'Multi-Series vs Time', yLabel || 'Value');

    for (let si = 0; si < seriesArray.length; si++) {
        const s = seriesArray[si];
        if (!s.times || !s.values) continue;
        drawLine(ctx, s.times, s.values, scaleX, scaleY, COLORS[si % COLORS.length]);
    }

    // 图例
    const legendX = w - m.r + 10;
    let legendY = m.t + 10;
    ctx.font = 'bold 12px Arial';
    ctx.textAlign = 'left';
    for (let si = 0; si < seriesArray.length; si++) {
        const color = COLORS[si % COLORS.length];
        ctx.fillStyle = color;
        ctx.fillRect(legendX, legendY - 8, 12, 12);
        ctx.fillStyle = '#333';
        ctx.fillText(seriesArray[si].name || `Series ${si + 1}`, legendX + 16, legendY + 2);
        legendY += 18;
    }
};

window.openFileDialogById = function (id) {
    const el = document.getElementById(id);
    if (el) el.click();
};