// Settings Page JavaScript

document.addEventListener('DOMContentLoaded', function() {
    // Sync dark mode toggle with theme switch in header
    const darkModeCheckbox = document.getElementById('darkMode');
    const themeToggle = document.getElementById('themeToggle');
    
    if (darkModeCheckbox && themeToggle) {
        // Set initial state
        darkModeCheckbox.checked = themeToggle.checked;
        
        // Sync changes
        darkModeCheckbox.addEventListener('change', function() {
            themeToggle.checked = darkModeCheckbox.checked;
            themeToggle.dispatchEvent(new Event('change'));
        });
        
        themeToggle.addEventListener('change', function() {
            darkModeCheckbox.checked = themeToggle.checked;
        });
    }
});
