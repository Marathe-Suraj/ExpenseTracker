// Global loading utilities and site-wide JavaScript

// Global loading overlay utility
window.LoadingOverlay = {
    show: function(message = 'Loading...', target = 'body') {
        const $target = $(target);
        const overlayId = 'global-loading-overlay';
        
        // Remove existing overlay
        $('#' + overlayId).remove();
        
        // Create overlay
        const overlay = $(`
            <div id="${overlayId}" class="loading-overlay">
                <div class="loading-content">
                    <div class="spinner-border text-primary mb-3" role="status"></div>
                    <div class="loading-text">${message}</div>
                </div>
            </div>
        `);
        
        $target.append(overlay);
        overlay.fadeIn(200);
    },
    
    hide: function() {
        $('#global-loading-overlay').fadeOut(200, function() {
            $(this).remove();
        });
    }
};

// Global button loading utility
window.ButtonLoader = {
    show: function($button, text = 'Loading...') {
        if (!$button.data('original-html')) {
            $button.data('original-html', $button.html());
        }
        $button.prop('disabled', true).html(`<span class="spinner-border spinner-border-sm me-1"></span>${text}`);
    },
    
    hide: function($button) {
        const originalHtml = $button.data('original-html');
        if (originalHtml) {
            $button.prop('disabled', false).html(originalHtml);
            $button.removeData('original-html');
        }
    }
};

// Global AJAX setup for loading indicators
$(document).ajaxStart(function() {
    // Show global loading for non-modal AJAX requests
    if (!$('.modal.show').length) {
        $('body').addClass('ajax-loading');
    }
}).ajaxStop(function() {
    $('body').removeClass('ajax-loading');
});

// Add loading states to all form submissions
$(document).on('submit', 'form', function(e) {
    const $form = $(this);
    const $submitBtn = $form.find('button[type="submit"], input[type="submit"]').first();
    
    if ($submitBtn.length && !$submitBtn.prop('disabled')) {
        setTimeout(function() {
            if (!$form.hasClass('no-loading')) {
                window.ButtonLoader.show($submitBtn, 'Processing...');
            }
        }, 100);
    }
});

// Add loading states to navigation links
$(document).on('click', 'a[href]:not([href^="#"]):not([data-bs-toggle]):not(.no-loading)', function(e) {
    const $link = $(this);
    const href = $link.attr('href');
    
    // Skip external links, javascript links, and same page links
    if (href.startsWith('http') || href.startsWith('javascript:') || href === window.location.pathname) {
        return;
    }
    
    // Show loading for internal navigation
    window.ButtonLoader.show($link, 'Loading...');
    
    // Hide loading after a timeout as fallback
    setTimeout(function() {
        window.ButtonLoader.hide($link);
    }, 5000);
});

// Page load complete - hide any loading states
$(document).ready(function() {
    $('body').removeClass('ajax-loading');
    $('.btn').each(function() {
        window.ButtonLoader.hide($(this));
    });
});
