/** @type {import('tailwindcss').Config} */
export default {
  content: ['./index.html', './src/**/*.{ts,tsx}'],
  theme: {
    extend: {
      // Every color is backed by a CSS variable (see src/index.css) via the
      // `<alpha-value>` pattern, so opacity modifiers (bg-ink/80, border-edge/50) keep working
      // and a later dark/light plan becomes a values-only :root override — no JSX churn.
      colors: {
        ink: 'rgb(var(--ink) / <alpha-value>)',
        panel: 'rgb(var(--panel) / <alpha-value>)',
        edge: 'rgb(var(--edge) / <alpha-value>)',
        accent: 'rgb(var(--accent) / <alpha-value>)',
        strong: 'rgb(var(--strong) / <alpha-value>)',
        body: 'rgb(var(--body) / <alpha-value>)',
        muted: 'rgb(var(--muted) / <alpha-value>)',
        subtle: 'rgb(var(--subtle) / <alpha-value>)',
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
