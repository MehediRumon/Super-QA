# Extension Data Loading Error Handling Improvement

## Problem Statement
Users encountered a generic error message "Failed to load extension data. The data may no longer be available." when trying to access extension test data through the ExtensionTestReview page. This error provided no useful troubleshooting information and left users uncertain about how to resolve the issue.

## Root Cause Analysis
The original error handling implementation:
1. Caught all exceptions with a single generic catch block
2. Displayed the same error message regardless of the actual failure reason
3. Provided no guidance on how to resolve common issues
4. Did not differentiate between:
   - Data not found in database (404)
   - Server/database connectivity issues (500)
   - Invalid data ID format (400)
   - Network connection failures

## Solution Implemented

### 1. Enhanced Client-Side Error Handling (ExtensionTestReview.razor)

#### Specific Error Detection
The `LoadExtensionData()` method now catches and handles different error scenarios:

```csharp
// HTTP 404 - Data not found
if (httpEx.Message.Contains("404"))
{
    errorMessage = "Extension data not found. The data may have been deleted or has expired. 
                   Please record your test steps again using the browser extension.";
}
// HTTP 500 - Server error
else if (httpEx.Message.Contains("500"))
{
    errorMessage = "Server error loading extension data. Please ensure the API is running 
                   and the database is accessible. Try again in a moment.";
}
// HTTP 400 - Invalid data ID
else if (httpEx.Message.Contains("400"))
{
    errorMessage = "Invalid data ID format. Please ensure you're using a valid link 
                   from the browser extension.";
}
// Network/Connection errors
else
{
    errorMessage = $"Failed to connect to the API server. Please ensure the SuperQA API 
                   is running on the expected port. Error: {httpEx.Message}";
}
```

#### Contextual Troubleshooting Tips
The error display now includes helpful troubleshooting tips based on the error type:

**For "Data Not Found" errors:**
- Extension data is stored temporarily. Record your test steps again if they're no longer available.
- Make sure you're using the latest link from the browser extension's "Send to SuperQA" button.
- If you've already generated a test from this data, check the "Generated Tests" project in the main dashboard.

**For "Server/API" errors:**
- Ensure the SuperQA API server is running. Start it with: `cd src/SuperQA.Api && dotnet run`
- Verify the API is accessible at the expected URL (usually https://localhost:7001 or http://localhost:7000)
- Check your network connection and firewall settings.
- Review the browser console (F12) for additional error details.

### 2. Improved Server-Side Error Responses (PlaywrightController.cs)

#### GetExtensionData Endpoint
Enhanced with:
- Validation of empty/null dataId parameter
- More descriptive error messages for invalid ID format
- Detailed 404 response with actionable message
- JSON deserialization error handling
- Structured error responses with error, message, and details fields

```csharp
[HttpGet("get-extension-data/{dataId}")]
public async Task<ActionResult<GenerateFromExtensionRequest>> GetExtensionData(string dataId)
{
    // Validate dataId parameter
    if (string.IsNullOrWhiteSpace(dataId))
        return BadRequest(new { error = "Data ID is required" });

    if (!int.TryParse(dataId, out int id))
        return BadRequest(new { 
            error = $"Invalid data ID format: '{dataId}'. The ID must be a valid number." 
        });
    
    // Check if data exists
    var extensionData = await _context.ExtensionTestData.FirstOrDefaultAsync(e => e.Id == id);
    if (extensionData == null)
        return NotFound(new { 
            error = "Extension data not found", 
            message = "The requested test data does not exist or may have been deleted. 
                      Please record your test steps again using the browser extension.",
            dataId = dataId
        });
    
    // Handle JSON deserialization errors
    try {
        steps = JsonSerializer.Deserialize<List<BrowserExtensionStep>>(extensionData.StepsJson);
    }
    catch (JsonException jsonEx) {
        return StatusCode(500, new { 
            error = "Failed to deserialize test steps", 
            message = "The stored test data is corrupted. Please record your test steps again.",
            details = jsonEx.Message
        });
    }
    
    // ... return successful response
}
```

#### UpdateExtensionData Endpoint
Added:
- Input validation for dataId and request body
- JSON serialization error handling
- Database update exception handling
- Consistent error response structure

#### DeleteExtensionData Endpoint
Added:
- Input validation for dataId
- Specific error messages for not found scenarios
- Database deletion exception handling
- Consistent error response structure

#### StoreExtensionData Endpoint
Added:
- Request body validation
- Steps validation (must have at least one step)
- JSON serialization error handling
- Database save exception handling
- Enhanced success response with additional metadata

### 3. Error Response Structure
All API endpoints now return consistent error responses:

```json
{
  "error": "Short error identifier",
  "message": "Detailed, user-friendly error message with resolution guidance",
  "details": "Technical details (when applicable)",
  "dataId": "The ID that was attempted (when relevant)"
}
```

## Benefits

1. **Better User Experience**: Users now understand exactly what went wrong and how to fix it
2. **Reduced Support Burden**: Clear error messages and troubleshooting tips reduce the need for support intervention
3. **Faster Problem Resolution**: Users can self-diagnose and resolve common issues
4. **Improved Debugging**: Developers get better error information in logs and browser console
5. **Consistent Error Handling**: All extension data endpoints follow the same error handling pattern

## Testing

### Build Verification
- ✅ Solution builds successfully without warnings or errors
- ✅ All 38 existing unit tests pass

### Manual Testing Scenarios
To test the improvements, try these scenarios:

1. **Invalid Data ID Format**
   - Navigate to: `/extension-test-review?dataId=abc`
   - Expected: See error message about invalid ID format

2. **Non-Existent Data ID**
   - Navigate to: `/extension-test-review?dataId=99999`
   - Expected: See error message about data not found with troubleshooting tips

3. **Missing Data ID**
   - Navigate to: `/extension-test-review`
   - Expected: See error message about missing dataId parameter

4. **Server Not Running**
   - Stop the API server and navigate to: `/extension-test-review?dataId=1`
   - Expected: See error message about server connectivity with troubleshooting tips

## Files Modified

1. **src/SuperQA.Client/Pages/ExtensionTestReview.razor**
   - Enhanced `LoadExtensionData()` method with specific error handling
   - Added contextual troubleshooting tips in the UI
   - Improved error message display logic

2. **src/SuperQA.Api/Controllers/PlaywrightController.cs**
   - Enhanced `GetExtensionData()` endpoint with better validation and error messages
   - Enhanced `UpdateExtensionData()` endpoint with comprehensive error handling
   - Enhanced `DeleteExtensionData()` endpoint with clear error responses
   - Enhanced `StoreExtensionData()` endpoint with input validation

## Future Enhancements

Consider implementing:
1. **Automatic Data Expiration**: Add a background job to clean up old extension data after a configurable period
2. **Data Retention Policy**: Allow users to configure how long extension data should be retained
3. **Retry Mechanism**: Implement automatic retry for transient network errors
4. **Error Logging**: Add structured logging for all error scenarios to aid in troubleshooting
5. **Toast Notifications**: Replace or supplement error alerts with toast notifications for better UX
6. **Health Check Endpoint**: Add an endpoint to verify API and database connectivity before loading data

## Backward Compatibility

All changes are backward compatible:
- Existing API endpoints continue to work as before
- API response structure for successful requests remains unchanged
- Only error responses have been enhanced with additional information
- No database schema changes required
