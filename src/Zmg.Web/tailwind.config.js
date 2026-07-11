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
    },
  },
  plugins: [],
}
