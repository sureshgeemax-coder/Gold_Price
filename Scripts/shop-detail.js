(function () {
    var chart;
    var selectedPeriod = '7';
    var currency = 'SGD';
    var fxRates = { SGD: 1 };
    var fxDates = {};
    var currencySymbols = { SGD: 'S$', INR: 'INR ', USD: 'USD ', MYR: 'MYR ', AED: 'AED ', GBP: 'GBP ', EUR: 'EUR ' };

    function parseDate(value, time) {
        var date = typeof value === 'string' && value.indexOf('/Date(') === 0 ? new Date(parseInt(value.slice(6), 10)) : new Date(value);
        var dateText = value ? String(value).split('T')[0] : '';
        var parts = dateText.split('-');
        if (parts.length === 3) date = new Date(Number(parts[0]), Number(parts[1]) - 1, Number(parts[2]));
        if (time && typeof time === 'object') return new Date(date.getFullYear(), date.getMonth(), date.getDate(), Number(time.Hours || 0), Number(time.Minutes || 0), Number(time.Seconds || 0), Number(time.Milliseconds || 0));
        var timeParts = String(time || '00:00:00').match(/^(\d{1,2}):(\d{2}):(\d{2})(?:\.(\d+))?/);
        if (timeParts) return new Date(date.getFullYear(), date.getMonth(), date.getDate(), Number(timeParts[1]), Number(timeParts[2]), Number(timeParts[3]), Number(('0.' + (timeParts[4] || '0')) * 1000));
        return date;
    }

    function loadHistory() {
        fetch(window.shopDetail.historyUrl + '&filter=' + selectedPeriod)
            .then(function (response) { if (!response.ok) throw new Error('History could not be loaded'); return response.json(); })
            .then(function (rows) {
                var filtered = rows.filter(function (row) { return row.Status === 'Success' && (row.Purity === 916 || row.Purity === 999); });
                document.getElementById('historyEmpty').hidden = filtered.length > 0;
                var labels = filtered.map(function (row) { return parseDate(row.Date, row.Time).toLocaleString('en-SG', { day: '2-digit', month: 'short', hour: '2-digit', minute: '2-digit' }); });
                var data916 = filtered.map(function (row) { return row.Purity === 916 ? Number(row.RatePerGram) : null; });
                var data999 = filtered.map(function (row) { return row.Purity === 999 ? Number(row.RatePerGram) : null; });
                var values = filtered.map(function (row) { return Number(row.RatePerGram); }).filter(Number.isFinite);
                var min = values.length ? Math.min.apply(null, values) : 0;
                var max = values.length ? Math.max.apply(null, values) : 1;
                var padding = Math.max((max - min) * 0.08, max * 0.005);
                if (chart) chart.destroy();
                chart = new Chart(document.getElementById('shopHistoryChart'), {
                    type: 'line',
                    data: { labels: labels, datasets: [
                        { label: '22K / 916 (SGD/g)', data: data916, spanGaps: false, borderColor: '#b58b35', backgroundColor: 'rgba(181,139,53,.12)', pointRadius: 2, tension: .18 },
                        { label: '24K / 999 (SGD/g)', data: data999, spanGaps: false, borderColor: '#176b66', backgroundColor: 'rgba(23,107,102,.08)', pointRadius: 2, tension: .18 }
                    ] },
                    options: { responsive: true, maintainAspectRatio: false, interaction: { mode: 'nearest', intersect: false }, plugins: { legend: { position: 'top' } }, scales: { y: { min: values.length ? Math.max(0, min - padding) : undefined, max: values.length ? max + padding : undefined, title: { display: true, text: 'SGD per gram' } }, x: { ticks: { maxTicksLimit: 9 } } } }
                });
            })
            .catch(function () { document.getElementById('historyEmpty').hidden = false; });
    }

    function showConverted() {
        var rate = fxRates[currency];
        var symbol = currencySymbols[currency] || currency + ' ';
        document.getElementById('converted916').textContent = window.shopDetail.rate916 == null || !rate ? '-' : symbol + (window.shopDetail.rate916 * rate).toFixed(2) + ' / g';
        document.getElementById('converted999').textContent = window.shopDetail.rate999 == null || !rate ? '-' : symbol + (window.shopDetail.rate999 * rate).toFixed(2) + ' / g';
    }

    function loadCurrency() {
        currency = document.getElementById('currencySelect').value;
        if (fxRates[currency]) {
            document.getElementById('conversionStatus').textContent = currency === 'SGD' ? 'Primary display currency.' : 'Reference rate dated ' + fxDates[currency];
            showConverted();
            return;
        }
        document.getElementById('conversionStatus').textContent = 'Fetching reference exchange rate...';
        fetch('https://api.frankfurter.dev/v1/latest?base=SGD&symbols=' + currency)
            .then(function (response) { if (!response.ok) throw new Error('Exchange rate unavailable'); return response.json(); })
            .then(function (data) {
                fxRates[currency] = data.rates[currency];
                fxDates[currency] = data.date;
                document.getElementById('conversionStatus').textContent = 'Reference rate dated ' + data.date;
                document.getElementById('fxTimestamp').textContent = 'Exchange-rate date: ' + data.date + '.';
                showConverted();
            })
            .catch(function () { document.getElementById('conversionStatus').textContent = 'Reference exchange rate unavailable.'; showConverted(); });
    }

    document.querySelectorAll('.period-button').forEach(function (button) {
        button.addEventListener('click', function () {
            selectedPeriod = button.dataset.filter;
            document.querySelectorAll('.period-button').forEach(function (item) { item.classList.toggle('active', item === button); });
            loadHistory();
        });
    });
    document.getElementById('currencySelect').addEventListener('change', loadCurrency);
    loadHistory();
    loadCurrency();
}());