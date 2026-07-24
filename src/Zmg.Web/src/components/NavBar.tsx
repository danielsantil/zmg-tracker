import { useEffect, useRef, useState } from 'react';
import { NavLink, useLocation } from 'react-router-dom';
import { Menu, Moon, Sun, X } from 'lucide-react';
import { useTheme, type Theme } from '../hooks/useTheme';
import Logo from './Logo';

// One source for both the desktop row and the mobile sheet, so a destination can't drift between them.
const NAV_LINKS: { to: string; label: string; end?: boolean }[] = [
  { to: '/', label: 'Home', end: true },
  { to: '/releases', label: 'Releases' },
  { to: '/catalog', label: 'Catalog' },
  { to: '/artists', label: 'Artists' },
  { to: '/templates', label: 'Templates' },
];

const desktopLink = ({ isActive }: { isActive: boolean }) =>
  `rounded-lg px-3 py-1.5 text-sm font-medium transition ${
    isActive ? 'bg-edge text-strong' : 'text-muted hover:text-body'
  }`;

const mobileLink = ({ isActive }: { isActive: boolean }) =>
  `rounded-lg px-3 py-2.5 text-sm font-medium transition ${
    isActive ? 'bg-edge text-strong' : 'text-muted hover:bg-edge hover:text-body'
  }`;

// Shows the mode you'd switch TO: a sun in dark mode (→ light), a moon in light mode (→ dark).
// Presentational: theme/toggle come from NavBar's single useTheme so the logo and this button never
// disagree (useTheme is per-caller local state — two independent calls would diverge on toggle).
function ThemeToggle({ theme, toggle }: { theme: Theme; toggle: () => void }) {
  const Icon = theme === 'dark' ? Sun : Moon;
  return (
    <button
      type="button"
      onClick={toggle}
      aria-label={theme === 'dark' ? 'Switch to light mode' : 'Switch to dark mode'}
      className="grid h-8 w-8 place-items-center rounded-lg text-muted transition hover:bg-edge hover:text-body"
    >
      <Icon className="h-4 w-4" aria-hidden />
    </button>
  );
}

/**
 * The app header. Desktop (≥sm) is the horizontal link row; below sm the five links collapse into a
 * `☰` dropdown sheet while brand + theme toggle stay always-visible (M37).
 *
 * The sheet is a plain `absolute` child of the sticky header rather than a body portal: the header
 * isn't transformed and has no `overflow-hidden`, so it's its own `z-10` stacking context and the sheet
 * layers above page content on its own — no clipping to escape (contrast RowMenu, which lives inside a
 * transformed Modal and must portal out). Outside-click is a ref check, so there's no full-screen
 * overlay to fight the header's stacking context. It closes on route change and on outside click.
 */
export default function NavBar() {
  const [open, setOpen] = useState(false);
  const headerRef = useRef<HTMLElement>(null);
  const location = useLocation();
  const { theme, toggle } = useTheme();

  // Close on navigation — tapping a sheet link should land on the page, not leave the sheet hanging.
  useEffect(() => setOpen(false), [location]);

  // Close on any click outside the header (the sheet lives inside it, so its links count as inside).
  useEffect(() => {
    if (!open) return;
    const onPointerDown = (e: MouseEvent) => {
      if (headerRef.current && !headerRef.current.contains(e.target as Node)) setOpen(false);
    };
    document.addEventListener('mousedown', onPointerDown);
    return () => document.removeEventListener('mousedown', onPointerDown);
  }, [open]);

  return (
    <header ref={headerRef} className="sticky top-0 z-10 border-b border-edge bg-ink/80 backdrop-blur">
      <div className="mx-auto flex max-w-5xl items-center gap-x-2 px-4 py-3">
        <NavLink to="/" className="mr-2 flex items-center" aria-label="Zion Music Group — home">
          {/* Inline SVG wordmark; text-strong makes it near-black in light / near-white in dark. */}
          <Logo className="h-7 w-auto text-strong" />
        </NavLink>

        {/* Desktop: the inline row, unchanged from before. */}
        <nav className="hidden items-center gap-x-2 sm:flex">
          {NAV_LINKS.map((l) => (
            <NavLink key={l.to} to={l.to} end={l.end} className={desktopLink}>
              {l.label}
            </NavLink>
          ))}
        </nav>

        {/* Always-visible controls, right-aligned. The theme toggle stays; the v2.7 language selector
            will slot in just before it. The hamburger is mobile-only. */}
        <div className="ml-auto flex items-center gap-x-1">
          <ThemeToggle theme={theme} toggle={toggle} />
          <button
            type="button"
            className="grid h-8 w-8 place-items-center rounded-lg text-muted transition hover:bg-edge hover:text-body sm:hidden"
            aria-label={open ? 'Close menu' : 'Open menu'}
            aria-expanded={open}
            onClick={() => setOpen((o) => !o)}
          >
            {open ? <X className="h-4 w-4" aria-hidden /> : <Menu className="h-4 w-4" aria-hidden />}
          </button>
        </div>
      </div>

      {/* Mobile sheet: anchored under the bar, full-width (inset-x-0 → no sideways scroll). Solid
          bg-panel (like RowMenu), not the bar's translucent glass — links must stay readable over
          whatever page content sits behind. */}
      {open && (
        <nav className="absolute inset-x-0 top-full border-b border-edge bg-panel shadow-lg sm:hidden">
          <div className="mx-auto flex max-w-5xl flex-col gap-y-1 px-4 py-3">
            {NAV_LINKS.map((l) => (
              <NavLink key={l.to} to={l.to} end={l.end} className={mobileLink}>
                {l.label}
              </NavLink>
            ))}
          </div>
        </nav>
      )}
    </header>
  );
}
