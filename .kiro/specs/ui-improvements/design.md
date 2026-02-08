# UI Improvements - Design

## Architecture

### Component Changes
- **ThemeToggle**: Add text label alongside icon
- **TagEditor**: Improve visual styling and layout
- **FileList**: Add dropdown menu for file actions

## Implementation Details

### 1. Theme Toggle Enhancement
- Add a text label next to the icon ("Light" / "Dark")
- Improve button styling with better contrast
- Keep the toggle in the header but make it more prominent

### 2. Tag Editor Improvements
- Increase modal width for better spacing
- Improve tag chip styling with better colors
- Add better visual hierarchy
- Improve button styling and spacing
- Add subtle animations for better UX

### 3. File Actions Dropdown
- Add a dropdown menu component using a three-dot icon (MoreVertical from lucide-react)
- Include "Rename" and "Delete" options in the dropdown
- Remove hover-dependent action buttons
- Position dropdown in the Actions column
- Implement click-outside-to-close functionality

### 4. API Warning Fixes
- Locate the header manipulation code in Program.cs (lines 253-257)
- Replace `context.Response.Headers.Add()` with indexer syntax
- Example: `context.Response.Headers["Header-Name"] = "value"`

## Testing Strategy
- Manual testing of all UI changes
- Verify theme toggle works correctly
- Test tag editor functionality
- Test dropdown menu interactions
- Verify API still runs without warnings
