// Background service worker for SuperQA Test Generator Extension

console.log('[SuperQA] Background service worker loaded');

// Listen for extension installation
chrome.runtime.onInstalled.addListener((details) => {
    if (details.reason === 'install') {
        console.log('[SuperQA] Extension installed');
        
        // Initialize storage
        chrome.storage.local.set({
            isRecording: false,
            recordedSteps: [],
            testName: ''
        });
    } else if (details.reason === 'update') {
        console.log('[SuperQA] Extension updated');
    }
});

// Handle messages from popup and content scripts
chrome.runtime.onMessage.addListener((message, sender, sendResponse) => {
    if (message.action === 'stepRecorded') {
        // Forward step to popup if it's open
        chrome.runtime.sendMessage(message).catch(() => {
            // Popup might not be open, store in storage instead
            chrome.storage.local.get(['recordedSteps'], (data) => {
                const steps = data.recordedSteps || [];
                steps.push(message.step);
                chrome.storage.local.set({ recordedSteps: steps });
            });
        });
    }
    
    return true;
});

// Listen for tab updates to inject content script if recording is active
chrome.tabs.onUpdated.addListener((tabId, changeInfo, tab) => {
    if (changeInfo.status === 'complete') {
        chrome.storage.local.get(['isRecording'], (data) => {
            if (data.isRecording) {
                chrome.tabs.sendMessage(tabId, { action: 'startRecording' }).catch(() => {
                    // Content script might not be ready yet
                    console.log('[SuperQA] Content script not ready on tab', tabId);
                });
            }
        });
    }
});
