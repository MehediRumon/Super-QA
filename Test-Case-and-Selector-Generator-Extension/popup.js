// --- Simplified popup.js - Only Gherkin steps and locators ---

document.addEventListener('DOMContentLoaded', () => {
    // UI element selectors
    const toggleSwitch = document.getElementById('toggleSwitch');
    const viewBtn = document.getElementById('viewBtn');
    const resetBtn = document.getElementById('resetBtn');
    const testNameInput = document.getElementById('testNameInput');
    const fabBtn = document.getElementById('fab');
    const themeToggle = document.getElementById('themeToggle');

    // --- Theme toggle (dark/light mode) ---
    if (localStorage.getItem("darkMode") === "enabled") document.body.classList.add("dark-mode");
    themeToggle.onclick = () => {
        document.body.classList.toggle('dark-mode');
        localStorage.setItem("darkMode", document.body.classList.contains("dark-mode") ? "enabled" : "disabled");
    };

    // --- Modal dialog for confirmations ---
    function showModal(message, onConfirm) {
        const modal = document.getElementById('modal');
        document.getElementById('modal-message').innerText = message;
        modal.style.display = 'flex';
        document.getElementById('modal-confirm').onclick = function () {
            modal.style.display = 'none';
            onConfirm();
        };
        document.getElementById('modal-cancel').onclick = function () {
            modal.style.display = 'none';
        };
    }

    // --- Toast notifications ---
    window.showToast = function (message, type = 'info') {
        const toast = document.createElement('div');
        toast.className = 'toast-message toast-' + type;
        toast.innerHTML = (type === 'success' ? '✅ ' : type === 'error' ? '❌ ' : 'ℹ️ ') + message;
        document.body.appendChild(toast);
        setTimeout(() => { toast.remove(); }, 2200);
    };

    // --- Load extension settings from storage ---
    chrome.storage.sync.get(['extensionEnabled', 'testName'], (result) => {
        toggleSwitch.checked = result.extensionEnabled ?? false;
        testNameInput.value = result.testName ?? '';
    });

    // --- Enable/disable extension ---
    toggleSwitch?.addEventListener('change', () => {
        chrome.storage.sync.set({ extensionEnabled: toggleSwitch.checked });
        chrome.tabs.query({ active: true, currentWindow: true }, (tabs) => {
            if (tabs[0]?.id) {
                chrome.tabs.sendMessage(tabs[0].id, {
                    action: 'toggle',
                    value: toggleSwitch.checked
                }).catch(() => { });
            }
        });
    });

    // --- Set test name from input ---
    testNameInput?.addEventListener('input', () => {
        const testName = testNameInput.value.trim();
        chrome.storage.sync.set({ testName }, () => {
            console.log('Test name updated:', testName);
        });
        chrome.tabs.query({ active: true, currentWindow: true }, (tabs) => {
            if (tabs[0]?.id) {
                chrome.tabs.sendMessage(tabs[0].id, { action: 'setTestName', value: testName }).catch(() => { });
            }
        });
    });

    // --- Reset/clear all data & logs ---
    resetBtn?.addEventListener('click', () => {
        showModal("Are you sure you want to clear all data?", () => {
            const currentTestName = testNameInput?.value?.trim() || '';
            
            chrome.storage.sync.set({
                collectedLocators: [],
                collectedGherkinSteps: [],
                testName: currentTestName
            }, () => {
                if (chrome.runtime.lastError) {
                    showToast('Error resetting: ' + chrome.runtime.lastError.message, 'error');
                    return;
                }
                
                chrome.tabs.query({ active: true, currentWindow: true }, (tabs) => {
                    if (tabs[0]?.id) {
                        chrome.tabs.sendMessage(tabs[0].id, { action: 'setTestName', value: currentTestName }).catch(() => { });
                    }
                });
                
                showToast('All data cleared!', 'success');
            });
        });
    });

    // --- Open view.html page in new tab ---
    viewBtn?.addEventListener('click', () => {
        chrome.tabs.create({ url: chrome.runtime.getURL('view.html') });
    });

    // --- Floating Action Button: open view.html ---
    fabBtn.onclick = () => {
        showToast("Quick action: Open view!", "info");
        window.open('view.html', '_blank', 'width=900,height=800,left=200,top=100');
    };
});
