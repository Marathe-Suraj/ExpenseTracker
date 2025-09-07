// Authentication Forms JavaScript Module
// Handles password visibility toggle and confirm password validation

(function() {
    'use strict';

    // Password visibility toggle functionality
    function initPasswordToggle() {
        const passwordToggles = document.querySelectorAll('[data-password-toggle]');
        
        passwordToggles.forEach(toggle => {
            const targetId = toggle.getAttribute('data-password-toggle');
            const passwordInput = document.getElementById(targetId);
            
            if (!passwordInput) return;
            
            // Set initial state
            toggle.setAttribute('aria-pressed', 'false');
            toggle.setAttribute('aria-label', 'Show password');
            
            toggle.addEventListener('click', function() {
                const isPasswordVisible = passwordInput.type === 'text';
                
                // Toggle input type
                passwordInput.type = isPasswordVisible ? 'password' : 'text';
                
                // Update button state
                toggle.setAttribute('aria-pressed', !isPasswordVisible);
                toggle.setAttribute('aria-label', isPasswordVisible ? 'Show password' : 'Hide password');
                
                // Update icon
                const icon = toggle.querySelector('.toggle-icon');
                if (icon) {
                    icon.innerHTML = isPasswordVisible ? 
                        '<path d="M16 8s-3-5.5-8-5.5S0 8 0 8s3 5.5 8 5.5S16 8 16 8zM1.173 8a13.133 13.133 0 0 1 1.66-2.043C4.12 4.668 5.88 3.5 8 3.5c2.12 0 3.879 1.168 5.168 2.457A13.133 13.133 0 0 1 14.828 8c-.058.087-.122.183-.195.288-.335.48-.83 1.12-1.465 1.755C11.879 11.332 10.119 12.5 8 12.5c-2.12 0-3.879-1.168-5.168-2.457A13.134 13.134 0 0 1 1.172 8z"/><path d="M8 5.5a2.5 2.5 0 1 0 0 5 2.5 2.5 0 0 0 0-5zM4.5 8a3.5 3.5 0 1 1 7 0 3.5 3.5 0 0 1-7 0z"/>' :
                        '<path d="m10.79 12.912-1.614-1.615a3.5 3.5 0 0 1-4.474-4.474l-2.06-2.06C.938 6.278 0 8 0 8s3 5.5 8 5.5a7.029 7.029 0 0 0 2.79-.588zM5.21 3.088A7.028 7.028 0 0 1 8 2.5c5 0 8 5.5 8 5.5s-.939 1.721-2.641 3.238l-2.062-2.062a3.5 3.5 0 0 0-4.474-4.474L5.21 3.089z"/><path d="M5.525 7.646a2.5 2.5 0 0 0 2.829 2.829l-2.83-2.829zm4.95.708-2.829-2.83a2.5 2.5 0 0 1 2.829 2.829zm3.171 6-12-12 .708-.708 12 12-.708.708z"/>';
                }
            });
            
            // Make toggle keyboard accessible
            toggle.addEventListener('keydown', function(e) {
                if (e.key === 'Enter' || e.key === ' ') {
                    e.preventDefault();
                    toggle.click();
                }
            });
        });
    }

    // Password confirmation validation
    function initPasswordConfirmation() {
        const passwordInput = document.getElementById('Password');
        const confirmPasswordInput = document.getElementById('ConfirmPassword');
        const form = document.querySelector('[data-auth-form="register"]');
        
        if (!passwordInput || !confirmPasswordInput || !form) return;
        
        // Find the validation container for the confirm password field
        const confirmPasswordGroup = confirmPasswordInput.closest('.form-group');
        const validationContainer = confirmPasswordGroup ? confirmPasswordGroup.querySelector('.validation-container') : null;
        
        if (!validationContainer) return;
        
        let errorElement = null;
        
        function createErrorElement() {
            if (!errorElement) {
                errorElement = document.createElement('div');
                errorElement.className = 'invalid-feedback';
                errorElement.setAttribute('aria-live', 'polite');
                validationContainer.appendChild(errorElement);
            }
            return errorElement;
        }
        
        function removeErrorElement() {
            if (errorElement) {
                errorElement.remove();
                errorElement = null;
            }
            confirmPasswordInput.classList.remove('is-invalid');
            confirmPasswordInput.setCustomValidity('');
        }
        
        function validatePasswordMatch() {
            const password = passwordInput.value;
            const confirmPassword = confirmPasswordInput.value;
            
            if (confirmPassword && password !== confirmPassword) {
                const error = createErrorElement();
                error.textContent = 'Passwords do not match.';
                confirmPasswordInput.classList.add('is-invalid');
                confirmPasswordInput.setCustomValidity('Passwords do not match');
                return false;
            } else {
                removeErrorElement();
                return true;
            }
        }
        
        // Real-time validation on input
        confirmPasswordInput.addEventListener('input', validatePasswordMatch);
        passwordInput.addEventListener('input', function() {
            if (confirmPasswordInput.value) {
                validatePasswordMatch();
            }
        });
        
        // Prevent form submission if passwords don't match
        form.addEventListener('submit', function(e) {
            if (!validatePasswordMatch()) {
                e.preventDefault();
                confirmPasswordInput.focus();
                return false;
            }
        });
    }

    // Initialize all auth form functionality
    function wireAuthForms() {
        initPasswordToggle();
        initPasswordConfirmation();
    }

    // Auto-initialize on pages with auth forms
    document.addEventListener('DOMContentLoaded', function() {
        if (document.querySelector('[data-auth-form]')) {
            wireAuthForms();
        }
    });

    // Export for manual initialization if needed
    window.authForms = {
        wire: wireAuthForms,
        initPasswordToggle: initPasswordToggle,
        initPasswordConfirmation: initPasswordConfirmation
    };
})();
