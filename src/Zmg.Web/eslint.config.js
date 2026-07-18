import js from '@eslint/js';
import globals from 'globals';
import reactHooks from 'eslint-plugin-react-hooks';
import reactRefresh from 'eslint-plugin-react-refresh';
import tseslint from 'typescript-eslint';

// Flat config for the SPA (replaces oxlint). Mirrors the previous ruleset — rules-of-hooks as an
// error, Fast-Refresh component-export hygiene as a warning — on top of the JS + TS recommended sets.
export default tseslint.config(
  { ignores: ['dist', 'node_modules'] },
  {
    files: ['**/*.{ts,tsx}'],
    extends: [js.configs.recommended, ...tseslint.configs.recommended],
    languageOptions: {
      ecmaVersion: 2022,
      globals: globals.browser,
      // Type-aware linting so no-floating-promises can see Promise-returning calls.
      parserOptions: {
        projectService: true,
        tsconfigRootDir: import.meta.dirname,
      },
    },
    plugins: {
      'react-hooks': reactHooks,
      'react-refresh': reactRefresh,
    },
    rules: {
      'react-hooks/rules-of-hooks': 'error',
      'react-hooks/exhaustive-deps': 'warn',
      'react-refresh/only-export-components': ['warn', { allowConstantExport: true }],
      // Catch fire-and-forget promises (an un-voided fetch/mutation). Intentional fire-and-forget
      // (cache invalidation) is marked `void`. no-misused-promises is left off on purpose — it
      // flags every async onClick, whose returned promise React harmlessly ignores.
      '@typescript-eslint/no-floating-promises': 'error',
    },
  },
);
