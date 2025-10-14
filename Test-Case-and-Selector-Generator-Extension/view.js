// Simplified view.js: Shows merged Gherkin steps with locators

function filterDuplicateStrings(arr) {
    return Array.from(new Set(arr));
}

function getStoredData(callback) {
    if (typeof chrome !== 'undefined' && chrome.storage && chrome.storage.sync) {
        chrome.storage.sync.get([
            'collectedGherkinSteps'
        ], callback);
    } else {
        // Fallback to localStorage for testing
        const result = {
            collectedGherkinSteps: JSON.parse(localStorage.getItem('collectedGherkinSteps') || '[]')
        };
        callback(result);
    }
}

// Function to update the display with current data
function updateDisplay() {
    getStoredData((result) => {
        console.log('Loaded data:', result);
        
        const gherkinSteps = filterDuplicateStrings(result.collectedGherkinSteps || []);
        
        const gherkinElement = document.getElementById('gherkinCode');
        
        if (gherkinElement) {
            gherkinElement.innerText = gherkinSteps.length
                ? gherkinSteps.join('\n')
                : 'No Gherkin steps collected.';
        }
    });
}

// Initial load and display data
updateDisplay();

// Listen for storage changes for live preview
if (typeof chrome !== 'undefined' && chrome.storage && chrome.storage.onChanged) {
    chrome.storage.onChanged.addListener((changes, namespace) => {
        if (namespace === 'sync') {
            // Check if Gherkin steps changed
            if (changes.collectedGherkinSteps) {
                console.log('Storage changed, updating display...');
                updateDisplay();
            }
        }
    });
}

// Copy to clipboard functionality
document.addEventListener('click', (e) => {
    if (e.target.classList.contains('copy-btn')) {
        const targetId = e.target.getAttribute('data-target');
        const statusId = e.target.getAttribute('data-status');
        const targetElement = document.getElementById(targetId);
        
        if (targetElement) {
            navigator.clipboard.writeText(targetElement.innerText).then(() => {
                const statusElement = document.getElementById(statusId);
                if (statusElement) {
                    statusElement.textContent = 'Copied!';
                    statusElement.classList.add('show');
                    setTimeout(() => {
                        statusElement.classList.remove('show');
                    }, 2000);
                }
            });
        }
    }
    
    // Edit functionality
    if (e.target.classList.contains('edit-btn')) {
        const targetId = e.target.getAttribute('data-target');
        const targetElement = document.getElementById(targetId);
        
        if (targetElement) {
            if (targetElement.contentEditable === 'true') {
                targetElement.contentEditable = 'false';
                e.target.textContent = '✏️';
                
                // Save edited content back to storage
                const content = targetElement.innerText;
                if (targetId === 'gherkinCode') {
                    const steps = content.split('\n').filter(s => s.trim());
                    if (typeof chrome !== 'undefined' && chrome.storage && chrome.storage.sync) {
                        chrome.storage.sync.set({ collectedGherkinSteps: steps });
                    } else {
                        localStorage.setItem('collectedGherkinSteps', JSON.stringify(steps));
                    }
                }
            } else {
                targetElement.contentEditable = 'true';
                e.target.textContent = '💾';
                targetElement.focus();
            }
        }
    }
});

// Theme toggle
const themeToggleBtn = document.getElementById('themeToggleBtn');
if (themeToggleBtn) {
    themeToggleBtn.addEventListener('click', () => {
        document.body.classList.toggle('dark-mode');
        const isDark = document.body.classList.contains('dark-mode');
        localStorage.setItem('theme', isDark ? 'dark' : 'light');
    });
    
    // Load saved theme
    const savedTheme = localStorage.getItem('theme');
    if (savedTheme === 'dark') {
        document.body.classList.add('dark-mode');
    }
}
