(function(){
	function animateCounter(el){
		const target = Number((el.getAttribute('data-target')||'').replace(/,/g,'')) || 0;
		const duration = 900;
		const start = performance.now();
		const finalText = (el.textContent||'').trim();
		const leadMatch = finalText.match(/^[^\d\-\+]+/);
		const trailMatch = finalText.match(/[^\d\.,\s]+$/);
		const prefix = leadMatch ? leadMatch[0].trim() : '';
		const suffix = !prefix && trailMatch ? trailMatch[0].trim() : '';
		const formatNumber = (n)=> n.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 });
		function frame(now){
			const p = Math.min(1, (now - start) / duration);
			const eased = 1 - Math.pow(1 - p, 3);
			const val = target * eased;
			el.textContent = prefix ? (prefix + formatNumber(val)) : (formatNumber(val) + (suffix ? (' ' + suffix) : ''));
			if(p < 1) requestAnimationFrame(frame);
		}
		requestAnimationFrame(frame);
	}

	function makePieChart(canvasId, labels, values){
		const el = document.getElementById(canvasId);
		const emptyEl = document.getElementById(canvasId + 'Empty');
		if(!el || typeof Chart === 'undefined') return;
		
		// Check if there's any data
		const hasData = values && values.length > 0 && values.some(v => v > 0);
		
		if (!hasData) {
			// Hide canvas and show empty message
			el.style.display = 'none';
			if (emptyEl) {
				emptyEl.classList.remove('d-none');
			}
			return;
		}
		
		// Show canvas and hide empty message
		el.style.display = 'block';
		if (emptyEl) {
			emptyEl.classList.add('d-none');
		}
		
		const baseColors = ['#0d6efd','#6f42c1','#20c997','#fd7e14','#dc3545','#198754','#0dcaf0','#ffc107'];
		const colors = values.map((_,i)=>baseColors[i % baseColors.length]);
		new Chart(el, {
			type: 'doughnut',
			data: { labels, datasets: [{ data: values, backgroundColor: colors, borderWidth: 0 }]},
			options: {
				plugins: { legend: { position: 'bottom' } },
				cutout: '58%',
				animation: { duration: 800 }
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
		makePieChart('pieDaily', daily, dailyVals);
		makePieChart('pieMonthly', monthly, monthlyVals);
		makePieChart('pieYearly', yearly, yearlyVals);
		wireModal();
	});
})();


