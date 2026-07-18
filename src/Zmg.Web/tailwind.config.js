/** @type {import('tailwindcss').Config} */
export default {
  content: ['./index.html', './src/**/*.{ts,tsx}'],
  // The tokens do the theming (index.css flips channels under [data-theme]); this selector is here so
  // `dark:` variants remain available if ever needed. `data-theme` is always 'dark' or 'light'.
  darkMode: ['selector', '[data-theme="dark"]'],
  theme: {
    extend: {
      // Every color is backed by a CSS variable (see src/index.css) via the
      // `<alpha-value>` pattern, so opacity modifiers (bg-ink/80, border-edge/50, bg-warn/15) keep
      // working and dark/light is a values-only :root override — no JSX churn. The semantic roles
      // (info/warn/ok/danger) pair a base hue for tints with a per-theme foreground (`*Fg`).
      colors: {
        ink: 'rgb(var(--ink) / <alpha-value>)',
        panel: 'rgb(var(--panel) / <alpha-value>)',
        edge: 'rgb(var(--edge) / <alpha-value>)',
        accent: 'rgb(var(--accent) / <alpha-value>)',
        strong: 'rgb(var(--strong) / <alpha-value>)',
        body: 'rgb(var(--body) / <alpha-value>)',
        muted: 'rgb(var(--muted) / <alpha-value>)',
        subtle: 'rgb(var(--subtle) / <alpha-value>)',
        info: 'rgb(var(--info) / <alpha-value>)',
        infoFg: 'rgb(var(--info-fg) / <alpha-value>)',
        warn: 'rgb(var(--warn) / <alpha-value>)',
        warnFg: 'rgb(var(--warn-fg) / <alpha-value>)',
        ok: 'rgb(var(--ok) / <alpha-value>)',
        okFg: 'rgb(var(--ok-fg) / <alpha-value>)',
        danger: 'rgb(var(--danger) / <alpha-value>)',
        dangerFg: 'rgb(var(--danger-fg) / <alpha-value>)',
      },
      // The toast is centered with -translate-x-1/2, so both frames must keep that X offset —
      // an animation transform replaces the class's, it doesn't compose with it.
      keyframes: {
        'toast-in': {
          from: { opacity: '0', transform: 'translate(-50%, 0.5rem)' },
          to: { opacity: '1', transform: 'translate(-50%, 0)' },
        },
      },
    },
  },
  plugins: [],
}
