/** @type {import('tailwindcss').Config} */
export default {
  content: ['./index.html', './src/**/*.{ts,tsx}'],
  theme: {
    extend: {
      colors: {
        ink: '#0f1115',
        panel: '#171a21',
        edge: '#252a34',
        accent: '#7c5cff',
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
