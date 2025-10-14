// Simplified view.js: Shows merged Gherkin steps with locators

function filterDuplicateStrings(arr) {
    return Array.from(new Set(arr));
}

function getStoredData(callback) {
    if (typeof chrome !== 'undefined' && chrome.storage && chrome.storage.sync) {
        chrome.storage.sync.get([
            'collectedGherkinSteps',
            'testName'
        ], callback);
    } else {
        // Fallback to localStorage for testing
        const result = {
            collectedGherkinSteps: JSON.parse(localStorage.getItem('collectedGherkinSteps') || '[]'),
            testName: localStorage.getItem('testName') || ''
        };
        callback(result);
    }
}

// Function to update the display with current data
function updateDisplay() {
    getStoredData((result) => {
        console.log('Loaded data:', result);
        
        const gherkinSteps = filterDuplicateStrings(result.collectedGherkinSteps || []);
        const testName = result.testName || '';
        
        const gherkinElement = document.getElementById('gherkinCode');
        const testNameInput = document.getElementById('testNameForSend');
        
        if (gherkinElement) {
            gherkinElement.innerText = gherkinSteps.length
                ? gherkinSteps.join('\n')
                : 'No Gherkin steps collected.';
        }
        
        if (testNameInput && testName) {
            testNameInput.value = testName;
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

// Send to SuperQA functionality
const sendToSuperQABtn = document.getElementById('sendToSuperQABtn');
const testNameForSendInput = document.getElementById('testNameForSend');
const sendStatus = document.getElementById('sendStatus');

if (sendToSuperQABtn) {
    sendToSuperQABtn.addEventListener('click', async () => {
        const testName = testNameForSendInput?.value?.trim() || '';
        
        if (!testName) {
            sendStatus.textContent = '❌ Please enter a test name';
            sendStatus.style.color = '#ffcccb';
            return;
        }
        
        getStoredData(async (result) => {
            const gherkinSteps = filterDuplicateStrings(result.collectedGherkinSteps || []);
            
            if (gherkinSteps.length === 0) {
                sendStatus.textContent = '❌ No Gherkin steps to send';
                sendStatus.style.color = '#ffcccb';
                return;
            }
            
            // Parse Gherkin steps to extract action, locator, and description
            const steps = gherkinSteps.map(stepText => {
                // Parse step format: "When Click on Login Button [xpath=//button[@id='login']]"
                const match = stepText.match(/^(Given|When|Then|And)\s+(.+?)\s*(?:\[(.+?)\])?$/);
                if (match) {
                    const [, keyword, description, locator] = match;
                    
                    // Determine action type
                    let action = 'click';
                    let value = '';
                    
                    if (description.toLowerCase().includes('enter') || description.toLowerCase().includes('type')) {
                        action = 'fill';
                        // Try to extract value from quotes
                        const valueMatch = description.match(/"([^"]+)"/);
                        if (valueMatch) {
                            value = valueMatch[1];
                        }
                    } else if (description.toLowerCase().includes('select')) {
                        action = 'select';
                        const valueMatch = description.match(/"([^"]+)"/);
                        if (valueMatch) {
                            value = valueMatch[1];
                        }
                    }
                    
                    return {
                        action: action,
                        locator: locator || '',
                        value: value,
                        description: stepText
                    };
                }
                
                // Fallback if parsing fails
                return {
                    action: 'click',
                    locator: '',
                    value: '',
                    description: stepText
                };
            });
            
            // Get current tab URL
            let applicationUrl = '';
            try {
                if (typeof chrome !== 'undefined' && chrome.tabs) {
                    const tabs = await chrome.tabs.query({ active: true, currentWindow: true });
                    if (tabs && tabs.length > 0) {
                        applicationUrl = tabs[0].url || '';
                    }
                }
            } catch (e) {
                console.log('Could not get current tab URL:', e);
                applicationUrl = 'http://localhost';
            }
            
            // Prepare request payload
            const payload = {
                applicationUrl: applicationUrl,
                testName: testName,
                steps: steps
            };
            
            console.log('Sending to SuperQA:', payload);
            
            // Update UI
            sendToSuperQABtn.disabled = true;
            sendStatus.textContent = '⏳ Sending to SuperQA...';
            sendStatus.style.color = 'white';
            
            try {
                // Send to SuperQA API
                const response = await fetch('https://localhost:7001/api/playwright/generate-from-extension', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                    },
                    body: JSON.stringify(payload)
                });
                
                if (response.ok) {
                    const data = await response.json();
                    sendStatus.textContent = '✅ Successfully sent to SuperQA!';
                    sendStatus.style.color = '#90EE90';
                    
                    // Open SuperQA in a new tab
                    setTimeout(() => {
                        window.open('https://localhost:7001', '_blank');
                    }, 500);
                } else {
                    const errorData = await response.json();
                    sendStatus.textContent = `❌ Error: ${errorData.errorMessage || 'Failed to send'}`;
                    sendStatus.style.color = '#ffcccb';
                }
            } catch (error) {
                console.error('Error sending to SuperQA:', error);
                sendStatus.textContent = `❌ Error: ${error.message}`;
                sendStatus.style.color = '#ffcccb';
            } finally {
                sendToSuperQABtn.disabled = false;
            }
        });
    });
    
    // Save test name to storage when changed
    testNameForSendInput?.addEventListener('input', () => {
        const testName = testNameForSendInput.value.trim();
        if (typeof chrome !== 'undefined' && chrome.storage && chrome.storage.sync) {
            chrome.storage.sync.set({ testName });
        } else {
            localStorage.setItem('testName', testName);
        }
    });
}

