import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { TriangleAlert } from 'lucide-react';
import { BusyOverlay, LanguageToggle, ThemeToggle } from '@/components';
import Logo from '@/components/Logo';
import { useLanguage } from '@/i18n/useLanguage';
import { useTheme } from '@/hooks/useTheme';
import { useAuth } from '@/auth/useAuth';

/** Google's mark, drawn inline — the CSP-safe way, and it must not be recoloured (brand rules). */
function GoogleMark() {
  return (
    <svg width="18" height="18" viewBox="0 0 48 48" aria-hidden="true" className="shrink-0">
      <path fill="#EA4335" d="M24 9.5c3.54 0 6.71 1.22 9.21 3.6l6.85-6.85C35.9 2.38 30.47 0 24 0 14.62 0 6.51 5.38 2.56 13.22l7.98 6.19C12.43 13.72 17.74 9.5 24 9.5z" />
      <path fill="#4285F4" d="M46.98 24.55c0-1.57-.15-3.09-.38-4.55H24v9.02h12.94c-.58 2.96-2.26 5.48-4.78 7.18l7.73 6c4.51-4.18 7.09-10.36 7.09-17.65z" />
      <path fill="#FBBC05" d="M10.53 28.59c-.48-1.45-.76-2.99-.76-4.59s.27-3.14.76-4.59l-7.98-6.19C.92 16.46 0 20.12 0 24c0 3.88.92 7.54 2.56 10.78l7.97-6.19z" />
      <path fill="#34A853" d="M24 48c6.48 0 11.93-2.13 15.89-5.81l-7.73-6c-2.15 1.45-4.92 2.3-8.16 2.3-6.26 0-11.57-4.22-13.47-9.91l-7.98 6.19C6.51 42.62 14.62 48 24 48z" />
    </svg>
  );
}

/**
 * The signed-out screen (v2.10/M56).
 *
 * Deliberately the plainest screen in the app: no nav, no form, one button. Nothing to type means no
 * validation, no error states and no password field to get wrong. It reuses `Modal`'s surface
 * vocabulary (`bg-panel` / `border-edge` / rounded) rather than introducing a visual primitive.
 *
 * The language and theme toggles are present *before* sign-in on purpose — both preferences live in
 * localStorage and are applied pre-paint, so omitting them would leave a Spanish-speaking partner
 * looking at English with no way out.
 *
 * `denied` arrives as a prop rather than being read from the URL here. The server redirects with
 * `?denied=1` when Google authenticated someone who isn't on the whitelist — carrying no email, since
 * ACA's ingress logs record the full path including query string and an address there would land in
 * Log Analytics on every denial. `AuthGate` consumes that flag and clears it (`useDeniedFlag`), so by
 * the time this screen paints the URL is already clean and the retry cannot carry it back through the
 * Google round trip as a `returnUrl`.
 */
export default function LoginPage({ denied = false }: { denied?: boolean }) {
  const { t } = useTranslation();
  const { signIn } = useAuth();
  const { language, setLanguage } = useLanguage();
  const { theme, toggle } = useTheme();
  const [redirecting, setRedirecting] = useState(false);

  // A bfcache restore — Back from Google's consent screen — replays this page exactly as it was left,
  // React state and overlay included, with no navigation left to arrive. `pageshow` with `persisted`
  // is the only signal for that case; a normal load starts at `false` and needs no reset.
  useEffect(() => {
    const onPageShow = (e: PageTransitionEvent) => {
      if (e.persisted) setRedirecting(false);
    };
    window.addEventListener('pageshow', onPageShow);
    return () => window.removeEventListener('pageshow', onPageShow);
  }, []);

  return (
    <div className="min-h-screen">
      <header className="flex items-center justify-end gap-x-3 px-4 py-3">
        <ThemeToggle theme={theme} toggle={toggle} />
        <LanguageToggle language={language} setLanguage={setLanguage} />
      </header>

      <main className="grid place-items-center px-4 pb-16 pt-8 sm:pt-20">
        <div className="w-full max-w-sm rounded-xl border border-edge bg-panel px-6 py-8 text-center">
          <Logo className="mx-auto h-8 w-auto text-strong" />

          {denied ? (
            <>
              <span className="mt-5 inline-flex items-center gap-x-1.5 rounded-full border border-warn/30 bg-warn/15 px-3 py-1 text-xs font-semibold text-warnFg">
                <TriangleAlert className="h-3.5 w-3.5" aria-hidden />
                {t('auth.denied.badge')}
              </span>
              <p className="mt-3 text-sm text-muted">{t('auth.denied.body')}</p>
            </>
          ) : (
            <h1 className="mt-5 text-base font-semibold text-strong">{t('auth.login.title')}</h1>
          )}

          {/* Google's own colours on a white surface in both themes — their branding rules require it,
              and a recoloured SSO button reads as a phishing page. */}
          <button
            type="button"
            onClick={() => {
              setRedirecting(true);
              signIn();
            }}
            disabled={redirecting}
            className="mt-6 flex w-full items-center justify-center gap-x-2.5 rounded-lg border border-[#dadce0] bg-white px-4 py-2.5 text-sm font-medium text-[#1f1f1f] shadow-sm transition hover:bg-[#f8f9fa] disabled:cursor-wait"
          >
            <GoogleMark />
            {denied ? t('auth.denied.retry') : t('auth.login.google')}
          </button>

          <p className="mt-5 border-t border-edge pt-4 text-xs leading-relaxed text-subtle">
            {t('auth.login.fineprint', { year: new Date().getFullYear() })}
          </p>
        </div>
      </main>

      {/* No `delayMs`: this one answers a click, so instant feedback is the point — unlike the gate's
          probe, where the fast path must stay invisible. One message, so it never rotates: the wait
          ends when the browser leaves for Google, and narrating that would be noise. */}
      {redirecting && <BusyOverlay messages={[t('auth.login.redirecting')]} />}
    </div>
  );
}
