// Popup script for SuperQA Test Generator Extension

let isRecording = false;
let recordedSteps = [];

// DOM elements
const testNameInput = document.getElementById('testName');
const startRecordingBtn = document.getElementById('startRecording');
const stopRecordingBtn = document.getElementById('stopRecording');
const clearStepsBtn = document.getElementById('clearSteps');
const sendToSuperQABtn = document.getElementById('sendToSuperQA');
const gherkinStepsContainer = document.getElementById('gherkinSteps');
const stepCountSpan = document.querySelector('.step-count');
const statusMessageDiv = document.getElementById('statusMessage');

// Load saved test name and steps from storage
chrome.storage.local.get(['testName', 'recordedSteps', 'isRecording'], (data) => {
    if (data.testName) {
        testNameInput.value = data.testName;
    }
    if (data.recordedSteps) {
        recordedSteps = data.recordedSteps;
        renderGherkinSteps();
    }
    if (data.isRecording) {
        isRecording = data.isRecording;
        updateRecordingState();
    }
});

// Save test name on input
testNameInput.addEventListener('input', () => {
    chrome.storage.local.set({ testName: testNameInput.value });
});

// Start recording button
startRecordingBtn.addEventListener('click', async () => {
    const testName = testNameInput.value.trim();
    
    if (!testName) {
        showStatus('Please enter a test name before recording', 'error');
        testNameInput.focus();
        return;
    }

    isRecording = true;
    await chrome.storage.local.set({ isRecording: true, testName: testName });
    
    // Send message to content script to start recording
    const [tab] = await chrome.tabs.query({ active: true, currentWindow: true });
    chrome.tabs.sendMessage(tab.id, { action: 'startRecording' });
    
    updateRecordingState();
    showStatus('Recording started. Interact with the page.', 'info');
});

// Stop recording button
stopRecordingBtn.addEventListener('click', async () => {
    isRecording = false;
    await chrome.storage.local.set({ isRecording: false });
    
    // Send message to content script to stop recording
    const [tab] = await chrome.tabs.query({ active: true, currentWindow: true });
    chrome.tabs.sendMessage(tab.id, { action: 'stopRecording' });
    
    updateRecordingState();
    showStatus('Recording stopped.', 'info');
});

// Clear steps button
clearStepsBtn.addEventListener('click', async () => {
    if (confirm('Are you sure you want to clear all recorded steps?')) {
        recordedSteps = [];
        await chrome.storage.local.set({ recordedSteps: [] });
        renderGherkinSteps();
        showStatus('All steps cleared.', 'info');
    }
});

// Send to SuperQA button
sendToSuperQABtn.addEventListener('click', async () => {
    const testName = testNameInput.value.trim();
    
    if (!testName) {
        showStatus('Please enter a test name', 'error');
        return;
    }
    
    if (recordedSteps.length === 0) {
        showStatus('No steps to send. Please record some interactions first.', 'error');
        return;
    }

    try {
        showStatus('Sending to SuperQA...', 'info');
        sendToSuperQABtn.disabled = true;

        // Get current tab URL
        const [tab] = await chrome.tabs.query({ active: true, currentWindow: true });
        const applicationUrl = new URL(tab.url).origin;

        // Convert Gherkin steps to BrowserExtensionStep format
        const steps = recordedSteps.map(step => ({
            action: step.action,
            locator: step.locator,
            value: step.value || '',
            description: `${step.keyword} ${step.description}`
        }));

        // Send to SuperQA API
        const response = await fetch('https://localhost:7001/api/playwright/generate-from-extension', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                applicationUrl: applicationUrl,
                steps: steps,
                testName: testName
            })
        });

        if (response.ok) {
            const result = await response.json();
            showStatus('Test successfully sent to SuperQA!', 'success');
            
            // Open SuperQA in new tab
            setTimeout(() => {
                chrome.tabs.create({
                    url: 'https://localhost:5001/playwright-generator?fromExtension=true'
                });
            }, 1000);
        } else {
            const errorData = await response.json();
            showStatus(`Error: ${errorData.errorMessage || 'Failed to send to SuperQA'}`, 'error');
        }
    } catch (error) {
        console.error('Error sending to SuperQA:', error);
        showStatus(`Error: ${error.message}`, 'error');
    } finally {
        sendToSuperQABtn.disabled = false;
    }
});

// Listen for messages from content script
chrome.runtime.onMessage.addListener((message, sender, sendResponse) => {
    if (message.action === 'stepRecorded') {
        recordedSteps.push(message.step);
        chrome.storage.local.set({ recordedSteps: recordedSteps });
        renderGherkinSteps();
    }
});

// Update UI based on recording state
function updateRecordingState() {
    if (isRecording) {
        startRecordingBtn.disabled = true;
        stopRecordingBtn.disabled = false;
        testNameInput.disabled = true;
    } else {
        startRecordingBtn.disabled = false;
        stopRecordingBtn.disabled = true;
        testNameInput.disabled = false;
    }
    
    // Enable send button if we have steps and a test name
    sendToSuperQABtn.disabled = recordedSteps.length === 0 || !testNameInput.value.trim();
}

// Render Gherkin steps in the viewer
function renderGherkinSteps() {
    stepCountSpan.textContent = `${recordedSteps.length} step${recordedSteps.length !== 1 ? 's' : ''} recorded`;
    
    if (recordedSteps.length === 0) {
        gherkinStepsContainer.innerHTML = `
            <div class="empty-state">
                <p>No steps recorded yet</p>
                <p class="hint">Click "Start Recording" to begin capturing interactions</p>
            </div>
        `;
        sendToSuperQABtn.disabled = true;
        return;
    }

    gherkinStepsContainer.innerHTML = recordedSteps.map((step, index) => `
        <div class="gherkin-step">
            <span class="keyword">${step.keyword}</span>
            <span class="description">${step.description}</span>
            ${step.locator ? `<span class="locator">Locator: ${step.locator}</span>` : ''}
        </div>
    `).join('');
    
    // Enable send button if test name is present
    sendToSuperQABtn.disabled = !testNameInput.value.trim();
}

// Show status message
function showStatus(message, type) {
    statusMessageDiv.textContent = message;
    statusMessageDiv.className = `status-message ${type}`;
    
    if (type === 'success' || type === 'info') {
        setTimeout(() => {
            statusMessageDiv.style.display = 'none';
        }, 5000);
    }
}

// Initialize
updateRecordingState();
