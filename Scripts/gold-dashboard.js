$(function () {
    updateDateTime();
    setInterval(updateDateTime, 1000);
    $('.chart-filter').click(function () {
        $('.chart-filter').removeClass('active');
        $(this).addClass('active');
        loadChartData(String($(this).data('filter')));
    });
    $('#currencySelect').on('change', loadCurrencyConversions);
    loadChartData('7');
    loadCurrencyConversions();
});

function updateDateTime() {
    var now = new Date();
    var formatted = now.toLocaleDateString('en-GB', { day: '2-digit', month: 'short', year: 'numeric' }) + ' ' + now.toLocaleTimeString('en-GB');
    $('#currentDateTime').text(formatted);
}

function refreshDashboard() {
    $('#loadingOverlay').show();
    $.ajax({
        url: window.dashboardData && window.dashboardData.refreshUrl ? window.dashboardData.refreshUrl : "/GoldRate/Refresh",
        type: 'POST',
        success: function (response) {
            if (response.success) location.reload();
            else alert('Refresh failed: ' + response.message);
        },
        error: function () { alert('Error during refresh.'); },
        complete: function () { $('#loadingOverlay').hide(); }
    });
}

var chart916Instance = null;
var chart999Instance = null;
var chartSources = ['Malabar', 'Mustafa', 'GRT Jewellers', 'Joyalukkas'];
var chartColors = { 'Malabar': '#a64b42', 'Mustafa': '#b58b35', 'GRT Jewellers': '#176b66', 'Joyalukkas': '#496c8c' };

function parseExcelTimestamp(row) {
    var value = row.Date;
    var date;
    if (typeof value === 'string' && value.indexOf('/Date(') === 0) date = new Date(parseInt(value.slice(6), 10));
    else if (value) date = new Date(value);
    var dateText = String(value || '').split('T')[0];
    var parts = dateText.split('-');
    if (parts.length === 3) date = new Date(Number(parts[0]), Number(parts[1]) - 1, Number(parts[2]));
    if (!date || !Number.isFinite(date.getTime())) return new Date(NaN);

    if (row.Time && typeof row.Time === 'object') {
        return new Date(date.getFullYear(), date.getMonth(), date.getDate(), Number(row.Time.Hours || 0), Number(row.Time.Minutes || 0), Number(row.Time.Seconds || 0), Number(row.Time.Milliseconds || 0));
    }

    var time = String(row.Time || '00:00:00');
    var timeParts = time.match(/^(\d{1,2}):(\d{2}):(\d{2})(?:\.(\d+))?/);
    if (!timeParts) timeParts = time.match(/^PT(?:(\d+(?:\.\d+)?)H)?(?:(\d+(?:\.\d+)?)M)?(?:(\d+(?:\.\d+)?)S)?/);
    if (timeParts && time.indexOf('PT') === 0) {
        return new Date(date.getFullYear(), date.getMonth(), date.getDate(), Number(timeParts[1] || 0), Number(timeParts[2] || 0), Number(timeParts[3] || 0), Math.floor(Number(timeParts[3] || 0) % 1 * 1000));
    }
    if (timeParts) return new Date(date.getFullYear(), date.getMonth(), date.getDate(), Number(timeParts[1]), Number(timeParts[2]), Number(timeParts[3]), Number(('0.' + (timeParts[4] || '0')) * 1000));
    return date;
}

function buildPurityChart(canvasId, purity, rows) {
    var datasets = chartSources.map(function (source) {
        var points = rows.filter(function (row) { return row.Source === source && Number(row.Purity) === purity; })
            .map(function (row) { return { x: parseExcelTimestamp(row).getTime(), y: Number(row.RatePerGram) }; })
            .filter(function (point) { return Number.isFinite(point.x) && Number.isFinite(point.y); })
            .sort(function (left, right) { return left.x - right.x; });
        return { label: source, data: points, showLine: true, borderColor: chartColors[source], backgroundColor: chartColors[source], pointRadius: 2, pointHoverRadius: 5, borderWidth: 2, tension: 0.15, spanGaps: false };
    });
    var values = datasets.reduce(function (all, dataset) { return all.concat(dataset.data.map(function (point) { return point.y; })); }, []);
    var min = values.length ? Math.min.apply(null, values) : undefined;
    var max = values.length ? Math.max.apply(null, values) : undefined;
    var padding = values.length ? Math.max((max - min) * 0.06, max * 0.002) : undefined;
    var canvas = document.getElementById(canvasId);
    var previous = canvasId === 'chart916' ? chart916Instance : chart999Instance;
    if (previous) previous.destroy();
    var instance = new Chart(canvas, {
        type: 'scatter',
        data: { datasets: datasets },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            interaction: { mode: 'nearest', intersect: false },
            plugins: { legend: { position: 'top' }, tooltip: { callbacks: { title: function (items) { return items.length ? new Date(items[0].parsed.x).toLocaleString() : ''; }, label: function (item) { return item.dataset.label + ': S$' + item.parsed.y.toFixed(2) + ' / g'; } } } },
            scales: {
                x: { type: 'linear', ticks: { maxTicksLimit: 8, callback: function (value) { return new Date(Number(value)).toLocaleDateString('en-SG', { day: '2-digit', month: 'short' }); } }, title: { display: true, text: 'Recorded date and time' } },
                y: { min: values.length ? Math.max(0, min - padding) : undefined, max: values.length ? max + padding : undefined, title: { display: true, text: 'SGD per gram' } }
            }
        }
    });
    if (canvasId === 'chart916') chart916Instance = instance;
    else chart999Instance = instance;
}

function loadChartData(filter) {
    var url = window.dashboardData ? window.dashboardData.historyUrl : '/GoldRate/GetHistory';
    $.get(url, { filter: filter }).done(function (rows) {
        var actualRows = rows.filter(function (row) { return row.Status === 'Success'; });
        $('#historyEmpty').prop('hidden', actualRows.length > 0);
        buildPurityChart('chart916', 916, actualRows);
        buildPurityChart('chart999', 999, actualRows);
    }).fail(function () {
        $('#historyEmpty').prop('hidden', false).text('History is temporarily unavailable.');
    });
}

var fxRates = { SGD: 1 };
var fxDates = {};
var currencySymbols = { SGD: 'S$', INR: 'INR ', USD: 'USD ', MYR: 'MYR ', AED: 'AED ', GBP: 'GBP ', EUR: 'EUR ' };

function loadCurrencyConversions() {
    var currency = $('#currencySelect').val() || 'SGD';
    function render(rate, date) {
        var symbol = currencySymbols[currency] || currency + ' ';
        $('[data-fx-shop]').each(function () {
            var shop = $(this).data('fx-shop');
            var purity = String($(this).data('fx-purity'));
            var value = window.dashboardData[shop + purity];
            $(this).text(value == null || !rate ? '-' : symbol + (Number(value) * rate).toFixed(2) + ' / g');
        });
        $('#conversionStatus').text(currency === 'SGD' ? 'Primary display currency.' : (date ? 'Reference exchange rate dated ' + date + '.' : 'Reference exchange rate unavailable.'));
    }
    if (fxRates[currency]) { render(fxRates[currency], fxDates[currency]); return; }
    $('#conversionStatus').text('Fetching reference exchange rate...');
    fetch('https://api.frankfurter.dev/v1/latest?base=SGD&symbols=' + encodeURIComponent(currency))
        .then(function (response) { if (!response.ok) throw new Error('FX unavailable'); return response.json(); })
        .then(function (data) { fxRates[currency] = data.rates[currency]; fxDates[currency] = data.date; render(fxRates[currency], data.date); })
        .catch(function () { render(null); });
}
