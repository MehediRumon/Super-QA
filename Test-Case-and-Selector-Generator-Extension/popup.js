// --- Simplified popup.js - Only Gherkin steps and locators ---

document.addEventListener('DOMContentLoaded', () => {
    // UI element selectors
    const toggleSwitch = document.getElementById('toggleSwitch');
    const viewBtn = document.getElementById('viewBtn');
    const resetBtn = document.getElementById('resetBtn');
    const menuNameInput = document.getElementById('menuNameInput');
    const actionNameInput = document.getElementById('actionNameInput');
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
    chrome.storage.sync.get(['extensionEnabled', 'actionName', 'menuName'], (result) => {
        toggleSwitch.checked = result.extensionEnabled ?? false;
        actionNameInput.value = result.actionName ?? '';
        menuNameInput.value = result.menuName ?? '';
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

    // --- Set action name from input ---
    actionNameInput?.addEventListener('input', () => {
        const actionName = actionNameInput.value.trim();
        chrome.storage.sync.set({ actionName }, () => {
            console.log('Action name updated:', { actionName });
        });
    });

    // --- Set menu name (for Gherkin steps) ---
    menuNameInput?.addEventListener('input', () => {
        const menuName = menuNameInput.value.trim();
        chrome.storage.sync.set({ menuName }, () => {
            console.log('Menu name updated:', menuName);
        });
        chrome.tabs.query({ active: true, currentWindow: true }, (tabs) => {
            if (tabs[0]?.id) {
                chrome.tabs.sendMessage(tabs[0].id, { action: 'setMenuName', value: menuName }).catch(() => { });
            }
        });
    });

    // --- Reset/clear all data & logs ---
    resetBtn?.addEventListener('click', () => {
        showModal("Are you sure you want to clear all data?", () => {
            const currentActionName = actionNameInput?.value?.trim() || '';
            const currentMenuName = menuNameInput?.value?.trim() || '';
            
            chrome.storage.sync.set({
                collectedLocators: [],
                collectedGherkinSteps: [],
                actionName: currentActionName,
                menuName: currentMenuName
            }, () => {
                if (chrome.runtime.lastError) {
                    showToast('Error resetting: ' + chrome.runtime.lastError.message, 'error');
                    return;
                }
                
                chrome.tabs.query({ active: true, currentWindow: true }, (tabs) => {
                    if (tabs[0]?.id) {
                        chrome.tabs.sendMessage(tabs[0].id, { action: 'setMenuName', value: currentMenuName }).catch(() => { });
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
