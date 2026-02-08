# Dark Mode Fix - Design

## Problem Statement
The theme system is correctly implemented with CSS variables in `index.css`, but component-specific CSS files use hardcoded colors that don't respond to theme changes.

## Solution Architecture

### CSS Variable Mapping Strategy

#### Background Colors
- `#ffffff` → `var(--surface-primary)` (white surfaces like cards, tables)
- `#f8fafc`, `#f8f9fa`, `#f1f5f9` → `var(--bg-secondary)` (light gray backgrounds)
- `#f0f9ff` → Keep for hover states (light blue tint)

#### Text Colors
- `#1e293b`, `#1a1a1a` → `var(--text-primary)` (main text)
- `#475569`, `#64748b`, `#4a4a4a` → `var(--text-secondary)` (secondary text)
- `#94a3b8`, `#868e96` → `var(--text-tertiary)` (tertiary/muted text)

#### Border Colors
- `#e2e8f0`, `#dee2e6` → `var(--border-color)`

#### Special Cases
- Gradient backgrounds (header, buttons) → Keep as-is (work in both themes)
- Colored buttons (red delete, blue primary) → Keep gradients but may need opacity adjustments
- Shadows → Use existing shadow variables or keep as-is

## Files to Update

### 1. Dashboard.css
**Changes needed:**
- `.dashboard-sidebar`: background-color, border-right
- `.dashboard-main`: background-color
- Keep header gradient (works in both themes)

### 2. FileList.css
**Changes needed:**
- `.file-list-title`: color
- `.file-table-container`: background-color
- `.file-table`: background-color
- `.file-table thead`: background gradient → use variables
- `.file-table th`: color
- `.file-row`: background-color (even/odd)
- `.file-row:hover`: background-color
- `.file-cell`: color
- `.file-cell.secondary`: color
- Keep button gradients (they provide visual identity)

### 3. FolderTree.css
**Changes needed:**
- `.folder-tree-title`: color
- `.folder-item`: background on hover
- `.folder-item.selected`: background
- `.folder-name`: color
- `.folder-count`: color
- `.create-input-container`: background
- `.folder-input`: background-color, border-color
- Keep button gradients

### 4. SearchBar.css (if exists)
Check and update if needed

### 5. FileUpload.css (if exists)
Check and update if needed

## Testing Strategy
1. Toggle between light and dark modes
2. Verify all sections change appropriately:
   - Header (should stay same - gradient works in both)
   - Sidebar
   - Main content area
   - File table
   - Folder tree
   - Modals (tag editor)
3. Check text contrast in both modes
4. Verify hover states work in both modes
5. Test on different screen sizes

## Rollback Plan
If issues arise:
1. Git revert the CSS changes
2. The theme toggle will still work (just won't have visual effect)
3. No functionality will be broken
