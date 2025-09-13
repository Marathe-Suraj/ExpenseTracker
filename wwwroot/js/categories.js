// Categories AJAX and modal logic
document.addEventListener('DOMContentLoaded', function(){
});

window.initializeCategoriesPage = function(){
    if (window.initDataTable) {
        window.initDataTable('#categoriesTable', {
            columnDefs: [
                { targets: 2, orderable: false, searchable: false, className: 'text-end' }
            ]
        });
    }

    // Clear modal content when modal is hidden to prevent content flash
    $('#categoryModal').on('hidden.bs.modal', function () {
        $('#categoryModalBody').html('');
        $('#categoryModalLoader').addClass('d-none');
    });

    // open modal links
    $(document).off('click', '.open-category-modal').on('click', '.open-category-modal', function(e){
        e.preventDefault();
        const $button = $(this);
        const url = $button.data('url');
        const $body = $('#categoryModalBody');
        const $loader = $('#categoryModalLoader');
        
        // Show button loading state immediately
        window.ButtonLoader.show($button, 'Loading...');
        
        // Clear previous content immediately to prevent flash
        $body.html('');
        $loader.removeClass('d-none');
        
        $.ajax({ url, headers: { 'X-Requested-With': 'XMLHttpRequest' } })
         .done(function(html){ $body.html(html); $loader.addClass('d-none'); })
         .fail(function(){ $body.html('<div class="p-4 text-danger">Failed to load. Please try again.</div>'); $loader.addClass('d-none'); })
         .always(function(){
            // Restore button state
            window.ButtonLoader.hide($button);
         });
    });

    // new button with data-url
    $(document).off('click', '[data-bs-target="#categoryModal"][data-url]').on('click', '[data-bs-target="#categoryModal"][data-url]', function(){
        const $button = $(this);
        const url = $button.data('url');
        const $body = $('#categoryModalBody');
        const $loader = $('#categoryModalLoader');
        
        // Show button loading state immediately
        window.ButtonLoader.show($button, 'Loading...');
        
        // Clear previous content immediately to prevent flash
        $body.html('');
        $loader.removeClass('d-none');
        
        $.ajax({ url, headers: { 'X-Requested-With': 'XMLHttpRequest' } })
         .done(function(html){ $body.html(html); $loader.addClass('d-none'); })
         .fail(function(){ $body.html('<div class="p-4 text-danger">Failed to load. Please try again.</div>'); $loader.addClass('d-none'); })
         .always(function(){
            // Restore button state
            window.ButtonLoader.hide($button);
         });
    });

    // submit create/edit/delete
    $(document).off('submit', '#categoryForm, #categoryDeleteForm').on('submit', '#categoryForm, #categoryDeleteForm', function(e){
        e.preventDefault();
        const $form = $(this);
        const url = $form.attr('action') || window.location.href;
        const method = ($form.attr('method') || 'post').toUpperCase();
        const $btn = $form.find('button[type="submit"]');
        const initial = $btn.html();
        $btn.prop('disabled', true).html('<span class="spinner-border spinner-border-sm me-2"></span>Saving');
        $.ajax({
            url, type: method, data: $form.serialize(), headers: { 'X-Requested-With': 'XMLHttpRequest' }
        }).done(function(resp){
            if (typeof resp === 'object' && resp && resp.success) {
                refreshCategoriesList();
                const modalEl = document.getElementById('categoryModal');
                bootstrap.Modal.getOrCreateInstance(modalEl).hide();
                return;
            }
            $('#categoryModalBody').html(resp);
        }).fail(function(xhr){
            const text = xhr.responseText || 'An error occurred';
            $('#categoryModalBody').html('<div class="p-4 text-danger">'+text+'</div>');
        }).always(function(){
            $btn.prop('disabled', false).html(initial);
        });
    });

    // Handle category status toggle
    $(document).off('click', '.toggle-category-status').on('click', '.toggle-category-status', function(e){
        e.preventDefault();
        const $btn = $(this);
        const categoryId = $btn.data('category-id');
        const action = $btn.data('action');
        const originalHtml = $btn.html();
        
        // Show loading state
        $btn.prop('disabled', true).html('<span class="spinner-border spinner-border-sm"></span>');
        
        $.ajax({
            url: '/Categories/ToggleStatus',
            type: 'POST',
            data: { id: categoryId },
            headers: { 'X-Requested-With': 'XMLHttpRequest' }
        }).done(function(resp){
            if (resp && resp.success) {
                // Show success message if available
                if (resp.message) {
                    // You can add a toast notification here if you have one
                    console.log(resp.message);
                }
                // Refresh the categories list to show updated status
                refreshCategoriesList();
            } else {
                alert('Failed to update category status. Please try again.');
                $btn.prop('disabled', false).html(originalHtml);
            }
        }).fail(function(xhr){
            const errorMsg = xhr.responseJSON?.message || 'An error occurred while updating category status.';
            alert(errorMsg);
            $btn.prop('disabled', false).html(originalHtml);
        });
    });
}

window.refreshCategoriesList = function(){
    const $host = $('#categoriesList');
    if ($host.length === 0) return;
    const baseUrl = $host.data('list-url') || (window.location.pathname.replace(/\/$/, '') + '/List');
    const url = baseUrl + window.location.search;
    const snapshot = $host.html();
    $host.html('<div class="text-center p-5"><div class="spinner-border text-primary"></div></div>');
    $.ajax({ url, headers: { 'X-Requested-With': 'XMLHttpRequest' } })
     .done(function(html){
        $host.html(html);
        if (window.initDataTable) {
            window.initDataTable('#categoriesTable', {
                columnDefs: [ { targets: 2, orderable: false, searchable: false, className: 'text-end' } ]
            });
        }
     })
     .fail(function(){ $host.html(snapshot); });
}


