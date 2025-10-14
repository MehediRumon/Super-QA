// Content script for recording user interactions and generating Gherkin steps

let isRecording = false;
let stepCounter = 0;

// Listen for messages from popup
chrome.runtime.onMessage.addListener((message, sender, sendResponse) => {
    if (message.action === 'startRecording') {
        startRecording();
        sendResponse({ success: true });
    } else if (message.action === 'stopRecording') {
        stopRecording();
        sendResponse({ success: true });
    }
    return true;
});

// Check if recording is already active
chrome.storage.local.get(['isRecording'], (data) => {
    if (data.isRecording) {
        startRecording();
    }
});

function startRecording() {
    if (isRecording) return;
    
    isRecording = true;
    console.log('[SuperQA] Recording started');
    
    // Add event listeners for various interactions
    document.addEventListener('click', handleClick, true);
    document.addEventListener('input', handleInput, true);
    document.addEventListener('change', handleChange, true);
    document.addEventListener('submit', handleSubmit, true);
}

function stopRecording() {
    if (!isRecording) return;
    
    isRecording = false;
    console.log('[SuperQA] Recording stopped');
    
    // Remove event listeners
    document.removeEventListener('click', handleClick, true);
    document.removeEventListener('input', handleInput, true);
    document.removeEventListener('change', handleChange, true);
    document.removeEventListener('submit', handleSubmit, true);
}

function handleClick(event) {
    if (!isRecording) return;
    
    const element = event.target;
    const tag = element.tagName.toLowerCase();
    
    // Ignore clicks on the extension UI itself
    if (element.closest('[data-superqa-extension]')) return;
    
    let step = null;
    
    if (tag === 'a') {
        step = createStep('When', `I click the link "${getElementText(element)}"`, 'click', getSelector(element));
    } else if (tag === 'button' || element.type === 'submit') {
        step = createStep('When', `I click the "${getElementText(element)}" button`, 'click', getSelector(element));
    } else if (tag === 'input' && element.type === 'checkbox') {
        const action = element.checked ? 'check' : 'uncheck';
        step = createStep('When', `I ${action} the "${getLabel(element)}" checkbox`, action, getSelector(element));
    } else if (tag === 'input' && element.type === 'radio') {
        step = createStep('When', `I select the "${getLabel(element)}" radio button`, 'click', getSelector(element));
    } else {
        // Generic click
        const text = getElementText(element);
        if (text) {
            step = createStep('When', `I click on "${text}"`, 'click', getSelector(element));
        } else {
            step = createStep('When', `I click on ${tag}`, 'click', getSelector(element));
        }
    }
    
    if (step) {
        sendStepToPopup(step);
    }
}

function handleInput(event) {
    if (!isRecording) return;
    
    const element = event.target;
    const tag = element.tagName.toLowerCase();
    
    if (tag === 'input' || tag === 'textarea') {
        const label = getLabel(element) || element.placeholder || element.name || 'input field';
        const value = element.value;
        
        // Debounce input events
        clearTimeout(element._inputTimeout);
        element._inputTimeout = setTimeout(() => {
            const step = createStep('When', `I enter "${value}" into the "${label}" field`, 'fill', getSelector(element), value);
            sendStepToPopup(step);
        }, 500);
    }
}

function handleChange(event) {
    if (!isRecording) return;
    
    const element = event.target;
    const tag = element.tagName.toLowerCase();
    
    if (tag === 'select') {
        const label = getLabel(element) || element.name || 'dropdown';
        const selectedOption = element.options[element.selectedIndex];
        const value = selectedOption ? selectedOption.text : element.value;
        
        const step = createStep('When', `I select "${value}" from the "${label}" dropdown`, 'select', getSelector(element), value);
        sendStepToPopup(step);
    }
}

function handleSubmit(event) {
    if (!isRecording) return;
    
    const form = event.target;
    const step = createStep('When', 'I submit the form', 'click', getSelector(form) + ' button[type="submit"]');
    sendStepToPopup(step);
}

function createStep(keyword, description, action, locator, value = '') {
    stepCounter++;
    return {
        id: stepCounter,
        keyword: keyword,
        description: description,
        action: action,
        locator: locator,
        value: value,
        timestamp: Date.now()
    };
}

function sendStepToPopup(step) {
    chrome.runtime.sendMessage({
        action: 'stepRecorded',
        step: step
    });
}

function getSelector(element) {
    // Priority: ID > data-testid > name > class > CSS path
    
    // Try ID
    if (element.id) {
        return `#${element.id}`;
    }
    
    // Try data-testid
    if (element.dataset.testid) {
        return `[data-testid="${element.dataset.testid}"]`;
    }
    
    // Try name attribute
    if (element.name) {
        return `[name="${element.name}"]`;
    }
    
    // Try aria-label
    if (element.getAttribute('aria-label')) {
        return `[aria-label="${element.getAttribute('aria-label')}"]`;
    }
    
    // Try class (if not too generic)
    if (element.className && typeof element.className === 'string') {
        const classes = element.className.split(' ').filter(c => c && !c.match(/^(active|hover|focus|selected)$/i));
        if (classes.length > 0 && classes.length <= 3) {
            return `.${classes[0]}`;
        }
    }
    
    // Try text content for links and buttons
    const tag = element.tagName.toLowerCase();
    if ((tag === 'a' || tag === 'button') && element.textContent.trim()) {
        const text = element.textContent.trim().substring(0, 30);
        return `${tag}:has-text("${text}")`;
    }
    
    // Fallback to CSS path
    return getCssPath(element);
}

function getCssPath(element) {
    const path = [];
    let current = element;
    
    while (current && current.nodeType === Node.ELEMENT_NODE) {
        let selector = current.tagName.toLowerCase();
        
        if (current.id) {
            selector += `#${current.id}`;
            path.unshift(selector);
            break;
        } else {
            let sibling = current;
            let nth = 1;
            
            while (sibling.previousElementSibling) {
                sibling = sibling.previousElementSibling;
                if (sibling.tagName === current.tagName) {
                    nth++;
                }
            }
            
            if (nth > 1) {
                selector += `:nth-of-type(${nth})`;
            }
        }
        
        path.unshift(selector);
        current = current.parentElement;
        
        // Limit depth
        if (path.length > 5) break;
    }
    
    return path.join(' > ');
}

function getElementText(element) {
    // Get text content, trimmed and cleaned
    let text = element.textContent || element.value || element.innerText || '';
    text = text.trim().replace(/\s+/g, ' ');
    
    // Limit length
    if (text.length > 50) {
        text = text.substring(0, 50) + '...';
    }
    
    return text;
}

function getLabel(element) {
    // Try to find associated label
    if (element.id) {
        const label = document.querySelector(`label[for="${element.id}"]`);
        if (label) {
            return label.textContent.trim();
        }
    }
    
    // Try parent label
    const parentLabel = element.closest('label');
    if (parentLabel) {
        return parentLabel.textContent.trim();
    }
    
    // Try aria-label
    if (element.getAttribute('aria-label')) {
        return element.getAttribute('aria-label');
    }
    
    // Try placeholder
    if (element.placeholder) {
        return element.placeholder;
    }
    
    // Try name
    if (element.name) {
        return element.name;
    }
    
    return null;
}

console.log('[SuperQA] Content script loaded');
