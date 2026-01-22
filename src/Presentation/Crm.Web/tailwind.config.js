/** @type {import('tailwindcss').Config} */
module.exports = {
  darkMode: 'class',
  content: [
    // Blazor (Web)
    './Components/**/*.{razor,cshtml,html}',
    './Components/**/*.razor.cs',
    // Blazor (shared UI project)
    '../Crm.UI/Components/**/*.{razor,cshtml,html}',
    '../Crm.UI/Components/**/*.razor.cs',
    // Static scripts in this project
    './wwwroot/**/*.js',
    // Exclusions (avoid scanning heavy dirs)
    '!./**/node_modules/**',
    '!./**/bin/**',
    '!./**/obj/**'
  ],
  theme: {
    extend: {
      fontFamily: {
        sans: ['Inter','ui-sans-serif','system-ui','Segoe UI','Roboto','Helvetica Neue','Arial']
      },
      colors: {
        brand: { 500: '#6366f1', 600: '#4f46e5' },
        accent: { 500: '#ec4899', 600: '#db2777' }
      },
      boxShadow: {
        glass: '0 10px 30px rgba(2,6,23,.18)'
      }
    }
  },
  plugins: [],
  corePlugins: {
    preflight: false
  }
};