# 🎯 Custom Multiselect Support - Implementation Guide

## **Overview**
The Test Case and Selector Generator Extension now supports **two types of multiselect patterns**:

1. **OSL Custom Multiselect (Search & Select)** - Custom search-based multiselect
2. **Bootstrap Multiselect** - Bootstrap-based multiselect dropdowns

## **Supported Multiselect Types**

### 1. OSL Custom Multiselect (Search & Select Pattern)

#### **HTML Structure**
```html
<div class="col-md-4">
    <div class="osl-custom-multiselect">
        <div class="input-div">
            <div class="selected-value"></div>
            <input id="TeacherIds" class="form-control input-search" autocomplete="off" placeholder="[TPIN] Name (Batch) ...">
            <input type="hidden" autocomplete="off" id="uuu-selected-list-data">
        </div>
        <ul id="multiselectOptionList" class="list-unstyled">
            <li>
                <input class="select" type="checkbox" value="4726"> <span>[4726] - Rumon (2017)</span>
            </li>
        </ul>
    </div>
</div>
```

#### **Detection Logic**
The extension detects this pattern by:
1. Checking if the clicked element is inside a `div` with class `osl-custom-multiselect`
2. Finding the input field with class `input-search` within that container
3. Generating XPath based on the search input's attributes (id, name, or placeholder)

#### **Generated XPath Examples**
- **With ID**: `//input[@id='TeacherIds']`
- **With Name**: `//input[@name='StudentIds']`
- **With Placeholder**: `//input[@placeholder='[TPIN] Name (Batch) ...']`

#### **Usage**
Simply click on the search input field within the `osl-custom-multiselect` container, and the extension will automatically generate the appropriate XPath.

---

### 2. Bootstrap Multiselect

#### **HTML Structure**
```html
<span class="hide-native-select">
    <select class="form-control" id="OrganizationId" multiple="multiple" name="OrganizationId">
        <option value="0" selected="">All</option>
        <option value="1">UDVASH</option>
        <option value="2">UNMESH</option>
    </select>
    <div class="btn-group">
        <button type="button" class="multiselect dropdown-toggle btn btn-default" data-toggle="dropdown" title="Select All">
            <span class="multiselect-selected-text">Select All</span>
            <b class="caret"></b>
        </button>
        <ul class="multiselect-container dropdown-menu">
            <li><a tabindex="0"><label class="checkbox"><input type="checkbox" value="1"> UDVASH</label></a></li>
            <li><a tabindex="0"><label class="checkbox"><input type="checkbox" value="2"> UNMESH</label></a></li>
        </ul>
    </div>
</span>
```

#### **Detection Logic**
The extension detects this pattern by:
1. Checking if the clicked element is inside a `<span>` parent
2. Finding a `<select>` element within that span
3. Verifying there's a button element following the select
4. Generating XPath that targets the interactive button

#### **Generated XPath Examples**
- **With ID**: `//select[@id='OrganizationId']/following-sibling::div//button`
- **With Name**: `//select[@name='Categories']/following-sibling::div//button`
- **With Class**: `//select[@class='form-control']/following-sibling::div//button`

#### **Usage**
Click on the button element of the bootstrap multiselect, and the extension will automatically detect the pattern and generate the appropriate XPath.

---

## **How It Works**

### **Detection Priority in content.js**

When a user clicks on any element, the extension checks in this order:

1. **Data-testid attribute** (highest priority)
2. **OSL Custom Multiselect** (Search & Select pattern)
3. **Bootstrap Multiselect** (hidden select with button)
4. **Element ID**
5. **Fallback XPath** (name, placeholder, or tag)

### **Code Implementation**

The implementation includes three key functions:

1. **`checkForOslCustomMultiselect(input)`**
   - Traverses up the DOM tree to find `.osl-custom-multiselect` container
   - Locates the `input.input-search` element
   - Generates XPath based on id, name, placeholder, or class

2. **`checkForMultiselectDropdown(input)`**
   - Traverses up the DOM tree to find `<span>` parent
   - Locates the `<select>` element and validates button structure
   - Generates XPath using `following-sibling::div//button` pattern

3. **`generateFallbackXPath(input)`**
   - Checks for both multiselect patterns as fallback
   - Returns appropriate XPath or falls back to standard attributes

---

## **Testing**

### **Test File**: `test_custom_multiselect.html`

The included test file demonstrates:
1. OSL Custom Multiselect with ID
2. OSL Custom Multiselect with name only
3. Bootstrap Multiselect with ID
4. Bootstrap Multiselect with name only
5. Regular input field (control test)

### **How to Test**
1. Load the extension in Chrome (Developer mode)
2. Open `test_custom_multiselect.html` in the browser
3. Enable the extension
4. Click on each test element
5. Verify the generated XPath in the extension output

### **Expected Results**

| Test Case | Element Clicked | Expected XPath |
|-----------|----------------|----------------|
| OSL Custom (ID) | Search input | `//input[@id='TeacherIds']` |
| OSL Custom (Name) | Search input | `//input[@name='StudentIds']` |
| Bootstrap (ID) | Multiselect button | `//select[@id='OrganizationId']/following-sibling::div//button` |
| Bootstrap (Name) | Multiselect button | `//select[@name='Categories']/following-sibling::div//button` |
| Regular Input | Text input | `//input[@id='regularInput']` |

---

## **Generated Code Examples**

### **For OSL Custom Multiselect (Search & Select)**

#### Locator Code
```csharp
public static By TeacherIds => By.XPath("//input[@id='TeacherIds']");
```

#### Step File Method
```csharp
[When("Enter Teacher Search {string}")]
public void WhenEnterTeacherSearch(string teacherSearch)
{
    _webElement = page.GetTeacherIds();
    _webElement.Clear();
    _webElement.SendKeys(teacherSearch);
}
```

### **For Bootstrap Multiselect**

#### Locator Code
```csharp
public static By OrganizationId => By.XPath("//select[@id='OrganizationId']/following-sibling::div//button");
```

#### Step File Method
```csharp
[When("Select Organization {string}")]
public void WhenSelectOrganization(string organization)
{
    _webElement = page.GetOrganizationId();
    var organizations = TestHelper.GetStringsBySplit(organization);
    foreach (var item in organizations)
        SelectElement(_webElement).SelectByText(item);
}
```

---

## **Benefits**

### ✅ **Automatic Detection**
- No manual configuration needed
- Works with both custom and bootstrap multiselects
- Intelligent pattern recognition

### ✅ **Robust XPath Generation**
- Prioritizes ID → Name → Placeholder/Class
- Generates reliable, maintainable selectors
- Handles edge cases gracefully

### ✅ **Backward Compatible**
- Existing functionality remains unchanged
- Falls back to standard XPath for regular elements
- No breaking changes

### ✅ **Developer Friendly**
- Clear, readable XPath expressions
- Follows best practices for element identification
- Easy to debug and maintain

---

## **Troubleshooting**

### Issue: XPath not generated correctly
- **Solution**: Verify the HTML structure matches one of the supported patterns
- Check browser console for any error messages
- Ensure the element has id, name, or placeholder attribute

### Issue: Extension doesn't detect multiselect
- **Solution**: Make sure the container has the correct class:
  - `osl-custom-multiselect` for OSL pattern
  - `<span>` parent with `<select>` child for Bootstrap pattern

### Issue: Wrong element highlighted
- **Solution**: Click directly on the target element (search input or button)
- Avoid clicking on the container or other child elements

---

## **Summary**

The extension now provides comprehensive support for multiple multiselect patterns, automatically detecting and generating appropriate XPath expressions for:

1. **OSL Custom Multiselect** - Search-based multiselect with custom UI
2. **Bootstrap Multiselect** - Bootstrap library-based multiselect dropdowns

This enhancement makes test automation more reliable and reduces manual XPath writing! 🚀
