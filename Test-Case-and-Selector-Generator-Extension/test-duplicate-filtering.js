// Test file for filterDuplicateStrings function
// Run with: node test-duplicate-filtering.js

function filterDuplicateStrings(arr) {
    // Intelligent duplicate removal
    // Two steps are considered duplicates if they have the same action and locator
    const seen = new Map();
    const result = [];
    
    for (const step of arr) {
        // Extract the key parts: action description and locator (if present)
        // Format: "Action on Element (xpath=...)" or "Action on Element"
        const locatorMatch = step.match(/\((xpath=.+?|css=.+?|id=.+?)\)$/);
        const locator = locatorMatch ? locatorMatch[1] : '';
        
        // Get the description part (everything before the locator)
        const description = locator ? step.substring(0, step.lastIndexOf('(')).trim() : step.trim();
        
        // Create a unique key combining description and locator
        const key = locator ? `${description}|${locator}` : description;
        
        // Only add if we haven't seen this combination before
        if (!seen.has(key)) {
            seen.set(key, true);
            result.push(step);
        }
    }
    
    return result;
}

// Test framework
let passed = 0;
let failed = 0;

function test(name, fn) {
    try {
        fn();
        console.log('✓', name);
        passed++;
    } catch (e) {
        console.log('✗', name);
        console.log('  Error:', e.message);
        failed++;
    }
}

function assertEquals(actual, expected, message) {
    if (JSON.stringify(actual) !== JSON.stringify(expected)) {
        throw new Error(message || `Expected ${JSON.stringify(expected)} but got ${JSON.stringify(actual)}`);
    }
}

// Tests
test('Should remove exact duplicate strings', () => {
    const input = [
        'Click on Login Button (xpath=//button[@id="login"])',
        'Click on Login Button (xpath=//button[@id="login"])',
        'Click on Submit (css=#submit)'
    ];
    const expected = [
        'Click on Login Button (xpath=//button[@id="login"])',
        'Click on Submit (css=#submit)'
    ];
    const result = filterDuplicateStrings(input);
    assertEquals(result, expected);
});

test('Should keep steps with same description but different locators', () => {
    const input = [
        'Click on Button (xpath=//button[@id="btn1"])',
        'Click on Button (xpath=//button[@id="btn2"])',
        'Click on Button (css=#btn3)'
    ];
    const expected = [
        'Click on Button (xpath=//button[@id="btn1"])',
        'Click on Button (xpath=//button[@id="btn2"])',
        'Click on Button (css=#btn3)'
    ];
    const result = filterDuplicateStrings(input);
    assertEquals(result, expected);
});

test('Should remove duplicates with same description and locator', () => {
    const input = [
        'Fill username field (xpath=//input[@id="user"])',
        'Fill username field (xpath=//input[@id="user"])',
        'Fill password field (xpath=//input[@id="pass"])'
    ];
    const expected = [
        'Fill username field (xpath=//input[@id="user"])',
        'Fill password field (xpath=//input[@id="pass"])'
    ];
    const result = filterDuplicateStrings(input);
    assertEquals(result, expected);
});

test('Should handle steps without locators', () => {
    const input = [
        'Navigate to homepage',
        'Navigate to homepage',
        'Click on Login Button (xpath=//button[@id="login"])'
    ];
    const expected = [
        'Navigate to homepage',
        'Click on Login Button (xpath=//button[@id="login"])'
    ];
    const result = filterDuplicateStrings(input);
    assertEquals(result, expected);
});

test('Should handle empty array', () => {
    const input = [];
    const expected = [];
    const result = filterDuplicateStrings(input);
    assertEquals(result, expected);
});

test('Should preserve order of first occurrence', () => {
    const input = [
        'Step 1 (xpath=//div[@id="1"])',
        'Step 2 (xpath=//div[@id="2"])',
        'Step 1 (xpath=//div[@id="1"])',
        'Step 3 (xpath=//div[@id="3"])',
        'Step 2 (xpath=//div[@id="2"])'
    ];
    const expected = [
        'Step 1 (xpath=//div[@id="1"])',
        'Step 2 (xpath=//div[@id="2"])',
        'Step 3 (xpath=//div[@id="3"])'
    ];
    const result = filterDuplicateStrings(input);
    assertEquals(result, expected);
});

test('Should handle mixed steps with and without locators', () => {
    const input = [
        'Navigate to page',
        'Click on Button (xpath=//button[@id="btn"])',
        'Navigate to page',
        'Wait for element',
        'Click on Button (xpath=//button[@id="btn"])',
        'Wait for element'
    ];
    const expected = [
        'Navigate to page',
        'Click on Button (xpath=//button[@id="btn"])',
        'Wait for element'
    ];
    const result = filterDuplicateStrings(input);
    assertEquals(result, expected);
});

// Display summary
console.log('\n' + '='.repeat(50));
console.log(`Test Summary: ${passed} passed, ${failed} failed, ${passed + failed} total`);
if (failed === 0) {
    console.log('✓ All tests passed!');
}
console.log('='.repeat(50));

process.exit(failed > 0 ? 1 : 0);
