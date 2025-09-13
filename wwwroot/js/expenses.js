// Expenses screen specific JavaScript

document.addEventListener('DOMContentLoaded', function() {
    // Function to update date input value class
    function updateDateValueClass(input) {
        if (input.value && input.value.length > 0) {
            input.classList.add('has-value');
        } else {
            input.classList.remove('has-value');
        }
    }

    // Initialize date inputs
    const fromDateInput = document.getElementById('fromDate');
    const toDateInput = document.getElementById('toDate');
    
    if (fromDateInput) {
        updateDateValueClass(fromDateInput);
        fromDateInput.addEventListener('change', function() {
            updateDateValueClass(fromDateInput);
        });
    }
    
    if (toDateInput) {
        updateDateValueClass(toDateInput);
        toDateInput.addEventListener('change', function() {
            updateDateValueClass(toDateInput);
        });
    }

    // Handle clear button clicks
    document.querySelectorAll('[data-clear-target]').forEach(function(btn) {
        btn.addEventListener('click', function(e) {
            e.preventDefault();
            e.stopPropagation();
            const target = btn.getAttribute('data-clear-target');
            const input = document.querySelector(target);
            if (input) {
                input.value = '';
                updateDateValueClass(input);
                // Only clear the field, don't auto-refresh or update URL
                // User needs to click "Apply Filters" to refresh data
            }
        });
    });

    // Hide server pagination if DataTable is active
    if (window.DataTable && document.querySelector('#expensesTable')) {
        var pag = document.getElementById('serverPagination');
        if (pag) pag.style.display = 'none';
    }

    // AJAX filter submit to refresh list only
    $(document).off('submit', '#filterForm').on('submit', '#filterForm', function(e){
        e.preventDefault();
        const $submitBtn = $(this).find('#filterGoBtn');
        
        // Show loading on filter button
        window.ButtonLoader.show($submitBtn, 'Filtering...');
        
        // Don't update URL, just refresh the list with current form data
        window.refreshExpensesList();
        
        // Hide loading after a delay (refreshExpensesList will handle the actual completion)
        setTimeout(function() {
            window.ButtonLoader.hide($submitBtn);
        }, 1000);
    });
});

// Global initializer to setup DataTable and ajax behaviors
window.initializeExpensesPage = function(){
    // init datatable on current list
    if (window.initDataTable) {
        window.initDataTable('#expensesTable', {
            columnDefs: [
                { targets: 2, className: 'text-end' },
                { targets: 4, orderable: false, searchable: false, className: 'text-end' }
            ]
        });
    }

    // Initialize view toggle functionality
    initializeViewToggle();

    // Clear modal content when modal is hidden to prevent content flash
    $('#expenseModal').on('hidden.bs.modal', function () {
        $('#expenseModalBody').html('');
        $('#expenseModalLoader').addClass('d-none');
    });

    // intercept modal openers
    $(document).off('click', '.open-expense-modal').on('click', '.open-expense-modal', function(e){
        e.preventDefault();
        const $button = $(this);
        const url = $button.data('url');
        const $modal = $('#expenseModal');
        const $body = $('#expenseModalBody');
        const $loader = $('#expenseModalLoader');
        
        // Show button loading state immediately
        window.ButtonLoader.show($button, 'Loading...');
        
        // Clear previous content immediately to prevent flash
        $body.html('');
        $loader.removeClass('d-none');
        $body.addClass('position-relative');
        
        $.ajax({ url: url, headers: { 'X-Requested-With': 'XMLHttpRequest' } }).done(function(html){
            $body.html(html);
            $loader.addClass('d-none');
        }).fail(function(){
            $body.html('<div class="p-4 text-danger">Failed to load. Please try again.</div>');
            $loader.addClass('d-none');
        }).always(function(){
            // Restore button state
            window.ButtonLoader.hide($button);
        });
    });

    // New Expense top button
    $(document).off('click', '[data-bs-target="#expenseModal"][data-url]').on('click', '[data-bs-target="#expenseModal"][data-url]', function(){
        const $button = $(this);
        const url = $button.data('url');
        const $body = $('#expenseModalBody');
        const $loader = $('#expenseModalLoader');
        
        // Show button loading state immediately
        window.ButtonLoader.show($button, 'Loading...');
        
        // Clear previous content immediately to prevent flash
        $body.html('');
        $loader.removeClass('d-none');
        
        $.ajax({ url: url, headers: { 'X-Requested-With': 'XMLHttpRequest' } }).done(function(html){
            $body.html(html);
            $loader.addClass('d-none');
        }).fail(function(){
            $body.html('<div class="p-4 text-danger">Failed to load. Please try again.</div>');
            $loader.addClass('d-none');
        }).always(function(){
            // Restore button state
            window.ButtonLoader.hide($button);
        });
    });

    // delegate form submit (create/edit/delete)
    $(document).off('submit', '#expenseForm, #expenseDeleteForm').on('submit', '#expenseForm, #expenseDeleteForm', function(e){
        e.preventDefault();
        const $form = $(this);
        const url = $form.attr('action') || window.location.href;
        const method = ($form.attr('method') || 'post').toUpperCase();
        const $submitBtn = $form.find('button[type="submit"]');
        const originalHtml = $submitBtn.html();
        $submitBtn.prop('disabled', true).html('<span class="spinner-border spinner-border-sm me-2"></span>Saving');
        $.ajax({
            url: url,
            type: method,
            data: $form.serialize(),
            headers: { 'X-Requested-With': 'XMLHttpRequest' }
        }).done(function(resp){
            // if JSON success
            if (typeof resp === 'object' && resp && resp.success) {
                refreshExpensesList();
                const modalEl = document.getElementById('expenseModal');
                const modal = bootstrap.Modal.getOrCreateInstance(modalEl);
                modal.hide();
                return;
            }
            // else it returned html with validation summary
            $('#expenseModalBody').html(resp);
        }).fail(function(xhr){
            const text = xhr.responseText || 'An error occurred';
            $('#expenseModalBody').html('<div class="p-4 text-danger">'+text+'</div>');
        }).always(function(){
            $submitBtn.prop('disabled', false).html(originalHtml);
        });
    });
}

// Function to initialize view toggle functionality
function initializeViewToggle() {
    const tableViewBtn = document.getElementById('tableViewBtn');
    const cardViewBtn = document.getElementById('cardViewBtn');
    const tableView = document.getElementById('tableView');
    const cardView = document.getElementById('cardView');

    if (tableViewBtn && cardViewBtn && tableView) {
        // Remove existing event listeners to prevent duplicates
        tableViewBtn.replaceWith(tableViewBtn.cloneNode(true));
        cardViewBtn.replaceWith(cardViewBtn.cloneNode(true));
        
        // Get the new elements after replacement
        const newTableViewBtn = document.getElementById('tableViewBtn');
        const newCardViewBtn = document.getElementById('cardViewBtn');
        
        newTableViewBtn.addEventListener('click', function() {
            // Update button states
            newTableViewBtn.classList.add('active');
            newCardViewBtn.classList.remove('active');
            
            // Show table view, hide card view
            tableView.classList.remove('d-none');
            tableView.classList.add('d-block');
            
            if (cardView) {
                cardView.classList.add('d-none');
                cardView.classList.remove('d-block');
            }
        });

        newCardViewBtn.addEventListener('click', function() {
            // Update button states
            newCardViewBtn.classList.add('active');
            newTableViewBtn.classList.remove('active');
            
            // Hide table view, show card view
            tableView.classList.add('d-none');
            tableView.classList.remove('d-block');
            
            if (cardView) {
                cardView.classList.remove('d-none');
                cardView.classList.add('d-block');
            }
        });
    }
}

window.refreshExpensesList = function(){
    const $listHost = $('#expensesList');
    if ($listHost.length === 0) return;
    const baseUrl = $listHost.data('list-url') || (window.location.pathname.replace(/\/$/, '') + '/List');
    
    // Get form data for filtering
    const form = document.getElementById('filterForm');
    let url = baseUrl;
    if (form) {
        const formData = new FormData(form);
        const params = new URLSearchParams();
        
        // Only add non-empty parameters
        for (const [key, value] of formData.entries()) {
            if (value && value.trim() !== '') {
                params.append(key, value);
            }
        }
        
        const queryString = params.toString();
        if (queryString) {
            url = baseUrl + '?' + queryString;
        }
    }
    
    // show a lightweight loader inline
    const original = $listHost.html();
    $listHost.html('<div class="text-center p-5"><div class="spinner-border text-primary"></div></div>');
    $.ajax({ url: url, headers: { 'X-Requested-With': 'XMLHttpRequest' } })
     .done(function(html){
        $listHost.html(html);
        // re-init table
        if (window.initDataTable) {
            window.initDataTable('#expensesTable', {
                columnDefs: [
                    { targets: 2, className: 'text-end' },
                    { targets: 4, orderable: false, searchable: false, className: 'text-end' }
                ]
            });
        }
        // Re-initialize view toggle functionality after refresh
        initializeViewToggle();
     })
     .fail(function(){
        $listHost.html(original);
     });
}