// Global loading utilities and site-wide JavaScript

window.getAntiForgeryToken = function () {
    return $('#antiForgeryForm input[name="__RequestVerificationToken"]').val()
        || $('input[name="__RequestVerificationToken"]').first().val()
        || '';
};

window.Toast = {
    show: function (message, type) {
        type = type || 'success';
        var bg = type === 'danger' ? 'text-bg-danger'
            : type === 'warning' ? 'text-bg-warning'
            : type === 'info' ? 'text-bg-info'
            : 'text-bg-success';
        var $host = $('#toastHost');
        if ($host.length === 0) {
            $host = $('<div id="toastHost" class="toast-container position-fixed top-0 end-0 p-3" style="z-index:1100;"></div>');
            $('body').append($host);
        }
        var id = 'toast-' + Date.now();
        var html = '<div id="' + id + '" class="toast align-items-center ' + bg + ' border-0" role="alert" aria-live="polite" aria-atomic="true">'
            + '<div class="d-flex"><div class="toast-body"></div>'
            + '<button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast" aria-label="Close"></button>'
            + '</div></div>';
        var $toast = $(html);
        $toast.find('.toast-body').text(message);
        $host.append($toast);
        var toast = bootstrap.Toast.getOrCreateInstance($toast[0], { delay: 3500 });
        toast.show();
        $toast.on('hidden.bs.toast', function () { $toast.remove(); });
    }
};

// Global loading overlay utility
window.LoadingOverlay = {
    show: function (message, target) {
        message = message || 'Loading...';
        target = target || 'body';
        var $target = $(target);
        var overlayId = 'global-loading-overlay';

        $('#' + overlayId).remove();

        var overlay = $(
            '<div id="' + overlayId + '" class="loading-overlay">' +
            '<div class="loading-content">' +
            '<div class="spinner-border text-primary mb-3" role="status"><span class="visually-hidden">Loading</span></div>' +
            '<div class="loading-text"></div>' +
            '</div></div>'
        );
        overlay.find('.loading-text').text(message);

        $target.append(overlay);
        overlay.fadeIn(200);
    },

    hide: function () {
        $('#global-loading-overlay').fadeOut(200, function () {
            $(this).remove();
        });
    }
};

// Global button loading utility
window.ButtonLoader = {
    show: function ($button, text) {
        text = text || 'Loading...';
        if (!$button.data('original-html')) {
            $button.data('original-html', $button.html());
        }
        $button.prop('disabled', true).html('<span class="spinner-border spinner-border-sm me-1"></span>' + text);
    },

    hide: function ($button) {
        var originalHtml = $button.data('original-html');
        if (originalHtml) {
            $button.prop('disabled', false).html(originalHtml);
            $button.removeData('original-html');
        }
    }
};

// Attach antiforgery token to all non-GET AJAX requests
$.ajaxSetup({
    beforeSend: function (xhr, settings) {
        if (!settings.type || /^(GET|HEAD|OPTIONS|TRACE)$/i.test(settings.type)) {
            return;
        }
        var token = window.getAntiForgeryToken();
        if (token) {
            xhr.setRequestHeader('RequestVerificationToken', token);
        }
    }
});

// Global AJAX setup for loading indicators
$(document).ajaxStart(function () {
    if (!$('.modal.show').length) {
        $('body').addClass('ajax-loading');
    }
}).ajaxStop(function () {
    $('body').removeClass('ajax-loading');
});

// Add loading states to all form submissions
$(document).on('submit', 'form', function () {
    var $form = $(this);
    var $submitBtn = $form.find('button[type="submit"], input[type="submit"]').first();

    if ($submitBtn.length && !$submitBtn.prop('disabled')) {
        setTimeout(function () {
            if (!$form.hasClass('no-loading')) {
                window.ButtonLoader.show($submitBtn, 'Processing...');
            }
        }, 100);
    }
});

// Add loading states to navigation links
$(document).on('click', 'a[href]:not([href^="#"]):not([data-bs-toggle]):not(.no-loading)', function () {
    var $link = $(this);
    var href = $link.attr('href');

    if (!href || href.startsWith('http') || href.startsWith('javascript:') || href === window.location.pathname) {
        return;
    }

    window.ButtonLoader.show($link, 'Loading...');

    setTimeout(function () {
        window.ButtonLoader.hide($link);
    }, 5000);
});

// Page load complete - hide any loading states
$(document).ready(function () {
    $('body').removeClass('ajax-loading');
    $('.btn').each(function () {
        window.ButtonLoader.hide($(this));
    });
});
