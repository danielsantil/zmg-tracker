import { useCallback, useEffect, useRef, useState } from 'react';
import { createPortal } from 'react-dom';
import { NavLink, useLocation } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { LogOut, Menu, Moon, Sun, X } from 'lucide-react';
import { useTheme, type Theme } from '../hooks/useTheme';
import { useLanguage } from '../i18n/useLanguage';
import { useAuth } from '../auth/useAuth';
import { LanguageToggle } from './LanguageToggle';
import Logo from './Logo';

// One source for both the desktop row and the mobile sheet, so a destination can't drift between them.
// `as const` keeps the key literals narrow enough for the typed `t()` to check them at compile time —
// which is also why `end` is spelled out on every row rather than left optional on all but Home.
const NAV_LINKS = [
  { to: '/', labelKey: 'nav.home', end: true },
  { to: '/releases', labelKey: 'nav.releases', end: false },
  { to: '/catalog', labelKey: 'nav.catalog', end: false },
  { to: '/artists', labelKey: 'nav.artists', end: false },
  { to: '/templates', labelKey: 'nav.templates', end: false },
] as const;

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
  const { t } = useTranslation();
  const Icon = theme === 'dark' ? Sun : Moon;
  return (
    <button
      type="button"
      onClick={toggle}
      aria-label={theme === 'dark' ? t('theme.toLight') : t('theme.toDark')}
      className="grid h-8 w-8 place-items-center rounded-lg text-muted transition hover:bg-edge hover:text-body"
    >
      <Icon className="h-4 w-4" aria-hidden />
    </button>
  );
}

/** Initials for the avatar — from the display name when there is one, else the address. */
function initials(user: { displayName: string | null; email: string }): string {
  const source = user.displayName?.trim() || user.email;
  const parts = source.split(/[\s.@_-]+/).filter(Boolean);
  return (parts[0]?.[0] ?? '?').concat(parts.length > 1 ? parts[1][0] : '').toUpperCase();
}

/**
 * Email + sign out, behind an initials avatar (≥sm only).
 *
 * Portals to `<body>` and positions from the trigger's rect, the same mechanism as `RowMenu` and for
 * the same reason (v2.2): a `fixed` popover inside a transformed ancestor resolves against that
 * ancestor rather than the viewport. Below `sm` this renders nothing — the hamburger sheet carries
 * the same two rows instead, so there is one popover implementation rather than two.
 */
function AccountMenu() {
  const { t } = useTranslation();
  const { user, signOut } = useAuth();
  const [open, setOpen] = useState(false);
  const [pos, setPos] = useState<React.CSSProperties | null>(null);
  const btnRef = useRef<HTMLButtonElement>(null);

  const close = useCallback(() => setOpen(false), []);

  // Fixed coordinates don't track the page, so close on scroll/resize — same as RowMenu.
  useEffect(() => {
    if (!open) return;
    const dismiss = () => setOpen(false);
    window.addEventListener('scroll', dismiss, true);
    window.addEventListener('resize', dismiss);
    return () => {
      window.removeEventListener('scroll', dismiss, true);
      window.removeEventListener('resize', dismiss);
    };
  }, [open]);

  if (!user) return null;

  function openMenu() {
    const rect = btnRef.current?.getBoundingClientRect();
    if (!rect) return;
    setPos({ position: 'fixed', top: rect.bottom + 6, right: window.innerWidth - rect.right });
    setOpen(true);
  }

  return (
    <>
      <button
        ref={btnRef}
        type="button"
        aria-label={t('auth.account.menu')}
        aria-expanded={open}
        onClick={() => (open ? close() : openMenu())}
        className="hidden h-8 w-8 place-items-center rounded-full bg-accent text-[0.68rem] font-bold text-white transition hover:bg-accent/90 sm:grid"
      >
        {initials(user)}
      </button>

      {open &&
        pos &&
        createPortal(
          <>
            <div className="fixed inset-0 z-50" onClick={close} />
            <div style={pos} className="z-50 w-60 overflow-hidden rounded-lg border border-edge bg-panel shadow-lg">
              <div className="px-3 py-2.5">
                {user.displayName && (
                  <div className="text-sm font-semibold text-strong">{user.displayName}</div>
                )}
                <div className="break-all font-mono text-xs text-muted">{user.email}</div>
              </div>
              <button
                type="button"
                onClick={() => {
                  close();
                  void signOut();
                }}
                className="flex w-full items-center gap-x-2 border-t border-edge px-3 py-2.5 text-left text-sm text-body hover:bg-edge"
              >
                <LogOut className="h-4 w-4" aria-hidden />
                {t('auth.account.signOut')}
              </button>
            </div>
          </>,
          document.body,
        )}
    </>
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
  const { t } = useTranslation();
  const { theme, toggle } = useTheme();
  const { language, setLanguage } = useLanguage();
  const { user, signOut } = useAuth();

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
        <NavLink to="/" className="mr-2 flex items-center" aria-label={t('nav.brandHome')}>
          {/* Inline SVG wordmark; text-strong makes it near-black in light / near-white in dark. */}
          <Logo className="h-7 w-auto text-strong" />
        </NavLink>

        {/* Desktop: the inline row, unchanged from before. */}
        <nav className="hidden items-center gap-x-2 sm:flex">
          {NAV_LINKS.map((l) => (
            <NavLink key={l.to} to={l.to} end={l.end} className={desktopLink}>
              {t(l.labelKey)}
            </NavLink>
          ))}
        </nav>

        {/* Always-visible controls, right-aligned. Language then theme (M43); the hamburger is
            mobile-only. */}
        <div className="ml-auto flex items-center gap-x-1">
          <LanguageToggle language={language} setLanguage={setLanguage} />
          <ThemeToggle theme={theme} toggle={toggle} />
          <AccountMenu />
          <button
            type="button"
            className="grid h-8 w-8 place-items-center rounded-lg text-muted transition hover:bg-edge hover:text-body sm:hidden"
            aria-label={open ? t('nav.closeMenu') : t('nav.openMenu')}
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
                {t(l.labelKey)}
              </NavLink>
            ))}

            {/* The account rows live here below sm rather than in a second popover implementation. */}
            {user && (
              <div className="mt-2 border-t border-edge pt-2">
                <div className="px-3 py-1.5 font-mono text-xs text-muted">{user.email}</div>
                <button
                  type="button"
                  onClick={() => void signOut()}
                  className="flex w-full items-center gap-x-2 rounded-lg px-3 py-2.5 text-left text-sm font-medium text-body hover:bg-edge"
                >
                  <LogOut className="h-4 w-4" aria-hidden />
                  {t('auth.account.signOut')}
                </button>
              </div>
            )}
          </div>
        </nav>
      )}
    </header>
  );
}
