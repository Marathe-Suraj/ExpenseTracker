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
        btn.addEventListener('click', function() {
            const target = btn.getAttribute('data-clear-target');
            const input = document.querySelector(target);
            if (input) {
                input.value = '';
                updateDateValueClass(input);
                const form = btn.closest('form');
                if (form) {
                    form.submit();
                }
            }
        });
    });

    // Hide server pagination if DataTable is active
    if (window.DataTable && document.querySelector('#expensesTable')) {
        var pag = document.getElementById('serverPagination');
        if (pag) pag.style.display = 'none';
    }
});
