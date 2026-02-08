# Dark Mode Fix - Requirements

## Overview
Fix the dark mode implementation so that theme changes are properly reflected throughout the application.

## Problem Analysis
The theme toggle button works and sets the `data-theme` attribute correctly, but many components use hardcoded colors in their CSS files instead of CSS variables. This means the colors don't change when switching themes.

## User Stories

### 1. Dark Mode Visual Changes
**As a** user  
**I want** the entire application to change appearance when I toggle dark mode  
**So that** I can use the app comfortably in different lighting conditions

**Acceptance Criteria:**
- 1.1 All backgrounds should change from light to dark colors
- 1.2 All text should change to appropriate contrast colors
- 1.3 All borders and shadows should adapt to the theme
- 1.4 The header gradient should work in both themes
- 1.5 Buttons and interactive elements should have proper contrast

### 2. Consistent Theme Application
**As a** user  
**I want** all components to respect the selected theme  
**So that** the experience is consistent throughout the app

**Acceptance Criteria:**
- 2.1 Dashboard sidebar should use theme variables
- 2.2 File list table should use theme variables
- 2.3 Folder tree should use theme variables
- 2.4 All modals and popups should use theme variables
- 2.5 No hardcoded colors should remain in component CSS

## Technical Approach
Replace all hardcoded color values in CSS files with CSS variable references:
- `#ffffff` → `var(--surface-primary)` or `var(--bg-primary)`
- `#1e293b` → `var(--text-primary)`
- `#64748b` → `var(--text-secondary)`
- `#e2e8f0` → `var(--border-color)`
- etc.

## Safety Considerations
- Keep gradient colors for the header (they work in both themes)
- Preserve all layout and spacing
- Don't change component structure or functionality
- Test thoroughly in both light and dark modes
