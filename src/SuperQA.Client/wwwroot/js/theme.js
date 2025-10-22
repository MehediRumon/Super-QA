// Theme management for Super-QA
window.themeManager = {
    init: function() {
        // Check for saved theme preference or default to 'dark'
        const savedTheme = localStorage.getItem('theme') || 'dark';
        this.setTheme(savedTheme);
    },
    
    setTheme: function(theme) {
        document.documentElement.setAttribute('data-theme', theme);
        localStorage.setItem('theme', theme);
        
        // Update toggle button icon if it exists
        const toggleBtn = document.getElementById('theme-toggle-btn');
        if (toggleBtn) {
            const icon = toggleBtn.querySelector('i');
            if (icon) {
                if (theme === 'dark') {
                    icon.className = 'bi bi-sun-fill';
                } else {
                    icon.className = 'bi bi-moon-fill';
                }
            }
        }
    },
    
    toggleTheme: function() {
        const currentTheme = document.documentElement.getAttribute('data-theme') || 'dark';
        const newTheme = currentTheme === 'dark' ? 'light' : 'dark';
        this.setTheme(newTheme);
        return newTheme;
    },
    
    getTheme: function() {
        return document.documentElement.getAttribute('data-theme') || 'dark';
    }
};

// Initialize theme when document is ready
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', function() {
        window.themeManager.init();
    });
} else {
    window.themeManager.init();
}
