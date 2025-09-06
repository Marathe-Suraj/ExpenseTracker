(function(){
	function getTheme(){ return document.documentElement.getAttribute('data-bs-theme') || 'light'; }
	function applyTheme(dt){
		const theme = getTheme();
		const table = dt.table().container();
		if (theme === 'dark') table.classList.add('dt-dark'); else table.classList.remove('dt-dark');
	}
	window.initDataTable = function(selector, opts){
		if (!window.DataTable) return null;
		const defaults = {
			paging: true,
			pageLength: 10,
			lengthMenu: [ [10,25,50,100,-1], [10,25,50,100,'All'] ],
			searching: true,
			lengthChange: true,
			ordering: true,
			info: true,
			order: [],
			pagingType: 'full_numbers',
			layout: { topStart: 'pageLength', topEnd: 'search', bottomStart: 'info', bottomEnd: 'paging' },
			language: {
                lengthMenu: 'Show _MENU_ Expenses',
                info: 'Showing _START_ to _END_ of _TOTAL_ Expenses',
                search: '', searchPlaceholder: 'Search...'
            },
		};
		const dt = new DataTable(selector, Object.assign({}, defaults, opts||{}));
		applyTheme(dt);
		document.getElementById('themeToggle')?.addEventListener('change',()=>setTimeout(()=>applyTheme(dt),0));
		return dt;
	}
})();


