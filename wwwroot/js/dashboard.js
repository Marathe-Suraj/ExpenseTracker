(function(){
	function getCurrencySymbol(){
		try {
			return JSON.parse(document.getElementById('currencySymbol')?.textContent || '"₹"');
		} catch {
			return '₹';
		}
	}

	function formatMoney(n){
		const symbol = getCurrencySymbol();
		return symbol + Number(n || 0).toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 });
	}

	function animateCounter(el){
		const target = Number((el.getAttribute('data-target')||'').replace(/,/g,'')) || 0;
		const duration = 1200;
		const start = performance.now();
		const finalText = (el.textContent||'').trim();
		const leadMatch = finalText.match(/^[^\d\-\+]+/);
		const trailMatch = finalText.match(/[^\d\.,\s]+$/);
		const prefix = leadMatch ? leadMatch[0].trim() : '';
		const suffix = !prefix && trailMatch ? trailMatch[0].trim() : '';
		const formatNumber = (n)=> n.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 });
		
		function frame(now){
			const p = Math.min(1, (now - start) / duration);
			const eased = p < 0.5 ? 4 * p * p * p : 1 - Math.pow(-2 * p + 2, 3) / 2;
			const val = target * eased;
			el.textContent = prefix ? (prefix + formatNumber(val)) : (formatNumber(val) + (suffix ? (' ' + suffix) : ''));
			if(p < 1) requestAnimationFrame(frame);
		}
		
		const delay = parseInt(el.closest('[data-delay]')?.getAttribute('data-delay')) || 0;
		setTimeout(() => requestAnimationFrame(frame), delay);
	}

	function makePieChart(canvasId, labels, values){
		const el = document.getElementById(canvasId);
		const emptyEl = document.getElementById(canvasId + 'Empty');
		if(!el || typeof Chart === 'undefined') return;
		
		const hasData = values && values.length > 0 && values.some(v => v > 0);
		
		if (!hasData) {
			el.style.display = 'none';
			if (emptyEl) emptyEl.classList.remove('d-none');
			return;
		}
		
		el.style.display = 'block';
		if (emptyEl) emptyEl.classList.add('d-none');
		
		const gradientColors = [
			'#667eea', '#764ba2', '#f093fb', '#f5576c',
			'#4facfe', '#00f2fe', '#43e97b', '#38f9d7',
			'#ffecd2', '#fcb69f', '#a8edea', '#fed6e3'
		];
		const colors = values.map((_,i) => gradientColors[i % gradientColors.length]);
		
		new Chart(el, {
			type: 'doughnut',
			data: { 
				labels, 
				datasets: [{ 
					data: values, 
					backgroundColor: colors,
					borderWidth: 3,
					borderColor: getComputedStyle(document.documentElement).getPropertyValue('--bs-body-bg') || '#ffffff',
					hoverBorderWidth: 4,
					hoverOffset: 8
				}]
			},
			options: {
				responsive: true,
				maintainAspectRatio: false,
				plugins: { 
					legend: { 
						position: 'bottom',
						labels: {
							padding: 20,
							usePointStyle: true,
							pointStyle: 'circle',
							font: { size: 12, weight: '500' }
						}
					},
					tooltip: {
						backgroundColor: 'rgba(0, 0, 0, 0.8)',
						titleColor: '#ffffff',
						bodyColor: '#ffffff',
						borderColor: '#667eea',
						borderWidth: 1,
						cornerRadius: 8,
						displayColors: true,
						callbacks: {
							label: function(context) {
								const total = context.dataset.data.reduce((a, b) => a + b, 0);
								const percentage = total ? ((context.parsed / total) * 100).toFixed(1) : '0.0';
								return `${context.label}: ${formatMoney(context.parsed)} (${percentage}%)`;
							}
						}
					}
				},
				cutout: '65%',
				animation: { 
					duration: 1000,
					easing: 'easeInOutCubic',
					animateRotate: true,
					animateScale: true
				},
				interaction: {
					intersect: false,
					mode: 'index'
				}
			}
		});
	}

	function makeTrendChart(labels, values){
		const el = document.getElementById('dailyTrendChart');
		const emptyEl = document.getElementById('dailyTrendChartEmpty');
		if(!el || typeof Chart === 'undefined') return;

		const hasData = values && values.length > 0 && values.some(v => v > 0);
		if (!hasData) {
			el.style.display = 'none';
			if (emptyEl) emptyEl.classList.remove('d-none');
			return;
		}

		el.style.display = 'block';
		if (emptyEl) emptyEl.classList.add('d-none');

		new Chart(el, {
			type: 'line',
			data: {
				labels,
				datasets: [{
					label: 'Daily total',
					data: values,
					borderColor: '#667eea',
					backgroundColor: 'rgba(102, 126, 234, 0.15)',
					fill: true,
					tension: 0.35,
					pointRadius: 3,
					pointHoverRadius: 5
				}]
			},
			options: {
				responsive: true,
				maintainAspectRatio: false,
				plugins: {
					legend: { display: false },
					tooltip: {
						callbacks: {
							label: (ctx) => formatMoney(ctx.parsed.y)
						}
					}
				},
				scales: {
					y: {
						beginAtZero: true,
						ticks: {
							callback: (v) => formatMoney(v)
						}
					}
				}
			}
		});
	}

	function wireModal(){
		const detailsModal = document.getElementById('detailsModal');
		if(!detailsModal) return;
		const modal = new bootstrap.Modal(detailsModal);
		document.querySelectorAll('[data-show-details]')?.forEach(btn=>{
			btn.addEventListener('click',()=>{ modal.show(); });
		});
	}

	document.addEventListener('DOMContentLoaded',function(){
		document.querySelectorAll('[data-counter]')?.forEach(animateCounter);
		const daily = JSON.parse(document.getElementById('dailyLabels')?.textContent||'[]');
		const dailyVals = JSON.parse(document.getElementById('dailyValues')?.textContent||'[]');
		const monthly = JSON.parse(document.getElementById('monthlyLabels')?.textContent||'[]');
		const monthlyVals = JSON.parse(document.getElementById('monthlyValues')?.textContent||'[]');
		const yearly = JSON.parse(document.getElementById('yearlyLabels')?.textContent||'[]');
		const yearlyVals = JSON.parse(document.getElementById('yearlyValues')?.textContent||'[]');
		const trendLabels = JSON.parse(document.getElementById('trendLabels')?.textContent||'[]');
		const trendValues = JSON.parse(document.getElementById('trendValues')?.textContent||'[]');
		makePieChart('pieDaily', daily, dailyVals);
		makePieChart('pieMonthly', monthly, monthlyVals);
		makePieChart('pieYearly', yearly, yearlyVals);
		makeTrendChart(trendLabels, trendValues);
		wireModal();
	});
})();
