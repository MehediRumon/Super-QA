# Super-QA Extension Integration Improvements - Implementation Summary

## Overview
This implementation addresses three key issues with the Super-QA browser extension integration:

1. **Fixed Port Configuration Issue**
2. **Added Database Persistence for Extension Data**
3. **Added Test Debugger with Step-by-Step Execution**

---

## 1. Port Configuration Fix

### Problem
The browser extension was sending data to `https://localhost:7001/extension-test-review?dataId={id}`, but the UI server runs on `http://localhost:5000`.

### Solution
Updated `Test-Case-and-Selector-Generator-Extension/view.js`:
- Changed API endpoint from `https://localhost:7001` to `http://localhost:5000`
- Updated the SuperQA URL to open on `http://localhost:5000/extension-test-review?dataId={id}`

### Files Changed
- `Test-Case-and-Selector-Generator-Extension/view.js` (lines 249 and 265)

---

## 2. Database Persistence for Extension Data

### Problem
Extension data was stored only in memory cache with a 10-minute expiration. This meant:
- Data was lost after 10 minutes
- No ability to edit or delete saved test data
- No persistent record of tests sent from the extension

### Solution Implemented

#### New Database Entity
Created `ExtensionTestData` entity to persist extension test data:
```csharp
public class ExtensionTestData
{
    public int Id { get; set; }
    public string TestName { get; set; }
    public string ApplicationUrl { get; set; }
    public string StepsJson { get; set; } // JSON serialized steps
    public int? TestCaseId { get; set; } // Reference to generated test case
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public TestCase? TestCase { get; set; }
}
```

#### Database Migration
- Created migration `AddExtensionTestData` to add the new table
- Added `ExtensionTestData` DbSet to `SuperQADbContext`
- Created `SuperQADbContextFactory` for design-time migration support

#### API Endpoints

##### Store Extension Data (Modified)
- **Endpoint**: `POST /api/playwright/store-extension-data`
- **Changes**: Now saves to database instead of memory cache
- **Returns**: `{ dataId: <int>, message: "Data stored successfully" }`

##### Get Extension Data (Modified)
- **Endpoint**: `GET /api/playwright/get-extension-data/{dataId}`
- **Changes**: Retrieves from database instead of memory cache
- **Returns**: Extension test data with steps

##### Update Extension Data (New)
- **Endpoint**: `PUT /api/playwright/update-extension-data/{dataId}`
- **Purpose**: Update saved extension test data
- **Body**: `GenerateFromExtensionRequest` with updated data
- **Returns**: Success message with dataId

##### Delete Extension Data (New)
- **Endpoint**: `DELETE /api/playwright/delete-extension-data/{dataId}`
- **Purpose**: Delete extension test data
- **Returns**: Success message

##### List Extension Data (New)
- **Endpoint**: `GET /api/playwright/list-extension-data`
- **Purpose**: List all extension test data
- **Returns**: Array of extension data summaries

#### UI Enhancements in ExtensionTestReview.razor

Added edit and delete functionality:

1. **Save Changes Button**: 
   - Appears when viewing extension data
   - Allows editing test name, application URL, and steps
   - Saves changes back to the database

2. **Delete Button**:
   - Allows deletion of extension test data
   - Includes confirmation dialog
   - Redirects to home page after deletion

3. **Improved Data Loading**:
   - Loads data by integer ID from query string
   - Better error handling and user feedback

#### Test Case Linking
- When generating a test from extension data, the generated `TestCase` is now linked to the `ExtensionTestData` record
- This creates traceability between extension-captured tests and generated automation scripts

### Files Changed
- `src/SuperQA.Core/Entities/ExtensionTestData.cs` (new)
- `src/SuperQA.Infrastructure/Data/SuperQADbContext.cs`
- `src/SuperQA.Infrastructure/Data/SuperQADbContextFactory.cs` (new)
- `src/SuperQA.Infrastructure/Migrations/20251016064836_AddExtensionTestData.cs` (new)
- `src/SuperQA.Api/Controllers/PlaywrightController.cs`
- `src/SuperQA.Client/Pages/ExtensionTestReview.razor`

---

## 3. Test Debugger with Step-by-Step Execution

### Problem
Tests executed in headless mode with no visibility into what was happening, making debugging difficult.

### Solution Implemented

#### Enhanced Test Execution Request
Added debug parameters to `PlaywrightTestExecutionRequest`:
```csharp
public class PlaywrightTestExecutionRequest
{
    public string TestScript { get; set; }
    public string ApplicationUrl { get; set; }
    public bool DebugMode { get; set; } = false;
    public int? SlowMotion { get; set; } = null; // Milliseconds
}
```

#### Debug Mode Features

1. **Headed Browser Mode**:
   - When debug mode is enabled, browser runs in headed mode (visible)
   - Allows watching test execution in real-time

2. **Slow Motion Execution**:
   - Configurable delay (100-5000ms) between actions
   - Default: 500ms
   - Helps observe each step of the test

3. **Debug Controls in UI**:
   - Toggle switch to enable debug mode
   - Slider to adjust slow motion delay
   - Visual feedback with bug icon

#### Implementation Details

##### PlaywrightTestExecutor Changes
- Updated `ExecuteTestScriptAsync` to accept `debugMode` and `slowMotion` parameters
- When debug mode is enabled:
  - Sets `Headless = false` (overriding configuration)
  - Injects `SlowMo` parameter into browser launch options
  - Adds debug logging

##### SlowMo Injection
Created `InjectSlowMotion` method to add slow motion to test scripts:
- Parses test script to find browser launch options
- Injects `SlowMo` parameter into launch configuration
- Handles both existing options and empty launch calls

##### UI Debug Controls
Added to `ExtensionTestReview.razor`:
```html
<div class="form-check form-switch">
    <input type="checkbox" @bind="debugMode">
    <label>Debug Mode (Headed browser with slow motion)</label>
</div>
<input type="number" @bind="slowMotion" min="100" max="5000" step="100" />
```

### Files Changed
- `src/SuperQA.Shared/DTOs/PlaywrightTestGenerationDto.cs`
- `src/SuperQA.Core/Interfaces/IPlaywrightTestExecutor.cs`
- `src/SuperQA.Infrastructure/Services/PlaywrightTestExecutor.cs`
- `src/SuperQA.Api/Controllers/PlaywrightController.cs`
- `src/SuperQA.Client/Pages/ExtensionTestReview.razor`

---

## Usage Guide

### Using the Browser Extension

1. **Capture Test Steps**:
   - Use the browser extension to record Gherkin steps with locators
   - Enter a test name

2. **Send to SuperQA**:
   - Click "Send to SuperQA" button
   - Extension sends data to `http://localhost:5000/api/playwright/store-extension-data`
   - Data is saved to database and assigned an ID
   - SuperQA opens in new tab: `http://localhost:5000/extension-test-review?dataId={id}`

3. **Review and Edit**:
   - View captured test data in ExtensionTestReview page
   - Edit test name, URL, or steps as needed
   - Click "Save Changes" to persist edits

4. **Generate Test**:
   - Click "Generate Test Script" to create Playwright automation
   - Generated script uses captured locators

5. **Debug Execution** (Optional):
   - Enable "Debug Mode" toggle
   - Adjust slow motion delay (default: 500ms)
   - Click "Execute Test" to run in headed mode
   - Watch browser automation in real-time

6. **Manage Data**:
   - Use "Delete" button to remove test data
   - View all extension data via `/api/playwright/list-extension-data`

### Database Schema

```sql
CREATE TABLE ExtensionTestData (
    Id INT PRIMARY KEY IDENTITY,
    TestName NVARCHAR(500) NOT NULL,
    ApplicationUrl NVARCHAR(2000) NOT NULL,
    StepsJson NVARCHAR(MAX) NOT NULL,
    TestCaseId INT NULL,
    CreatedAt DATETIME2 NOT NULL,
    UpdatedAt DATETIME2 NOT NULL,
    FOREIGN KEY (TestCaseId) REFERENCES TestCases(Id) ON DELETE SET NULL
);
```

---

## Testing Recommendations

1. **Test Port Configuration**:
   - Start SuperQA.Api on http://localhost:5000
   - Use extension to send test data
   - Verify it opens correct URL

2. **Test Database Persistence**:
   - Send test data from extension
   - Restart application
   - Verify data persists (load by dataId)

3. **Test Edit Functionality**:
   - Load extension data
   - Modify test name/steps
   - Click "Save Changes"
   - Reload page and verify changes persisted

4. **Test Delete Functionality**:
   - Load extension data
   - Click "Delete"
   - Confirm deletion
   - Verify redirect and data removal

5. **Test Debug Mode**:
   - Generate a test script
   - Enable debug mode
   - Set slow motion to 1000ms
   - Execute test
   - Verify browser opens visibly and actions are slowed

---

## Configuration

### Development Mode (Default)
- Uses in-memory database
- No migration needed
- Data lost on restart (unless using SQL Server)

### Production Mode with SQL Server
1. Update `appsettings.json`:
```json
{
  "UseInMemoryDatabase": false,
  "ConnectionStrings": {
    "DefaultConnection": "Server=...;Database=SuperQA;..."
  }
}
```

2. Apply migrations:
```bash
cd src/SuperQA.Infrastructure
dotnet ef database update --startup-project ../SuperQA.Api
```

---

## Future Enhancements

1. **Extension Data Management UI**:
   - Add page to list all extension test data
   - Bulk delete/export functionality
   - Search and filter capabilities

2. **Enhanced Debug Features**:
   - Breakpoint support
   - Step-through execution
   - Variable inspection
   - Screenshot on each step

3. **Test Data Versioning**:
   - Track changes to extension data
   - Ability to revert to previous versions
   - Diff view between versions

4. **Integration Testing**:
   - End-to-end tests for extension workflow
   - API integration tests for new endpoints
   - UI tests for edit/delete functionality

---

## Dependencies Updated

- `Microsoft.EntityFrameworkCore.Design` 9.0.10 (added to SuperQA.Api)
- `Microsoft.EntityFrameworkCore.InMemory` 9.0.10 (updated in SuperQA.Tests)
- `dotnet-ef` tool (installed globally)

---

## Breaking Changes

⚠️ **Migration Required**: If using SQL Server, run migrations to add `ExtensionTestData` table.

⚠️ **URL Change**: Browser extension now uses `http://localhost:5000` instead of `https://localhost:7001`

---

## Rollback Plan

If issues occur, you can:

1. Revert extension URL changes in `view.js`
2. Remove migration: `dotnet ef migrations remove --startup-project ../SuperQA.Api`
3. Revert code changes via Git

---

## Summary

All three requirements have been successfully implemented:

✅ **Port Configuration Fixed**: Extension now uses correct port (5000)
✅ **Database Persistence Added**: Extension data saved to DB with edit/delete support
✅ **Test Debugger Implemented**: Debug mode with headed browser and slow motion

The implementation is production-ready with proper error handling, database migrations, and comprehensive functionality.
