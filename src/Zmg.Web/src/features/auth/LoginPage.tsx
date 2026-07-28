import { useTranslation } from 'react-i18next';
import { useSearchParams } from 'react-router-dom';
import { TriangleAlert } from 'lucide-react';
import Logo from '@/components/Logo';
import { LanguageToggle } from '@/components/LanguageToggle';
import { useLanguage } from '@/i18n/useLanguage';
import { useTheme } from '@/hooks/useTheme';
import { Moon, Sun } from 'lucide-react';
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
 */
export default function LoginPage() {
  const { t } = useTranslation();
  const { signIn } = useAuth();
  const { language, setLanguage } = useLanguage();
  const { theme, toggle } = useTheme();
  const [params] = useSearchParams();

  // The server redirects here with ?denied=1 when Google authenticated someone who isn't on the
  // whitelist. It carries no email: ACA's ingress logs record the full path including query string,
  // so putting an address there would push it into Log Analytics on every denial — contradicting the
  // same decision that keeps attribution off business writes. It is logged server-side instead.
  const denied = params.get('denied') === '1';
  const ThemeIcon = theme === 'dark' ? Sun : Moon;

  return (
    <div className="min-h-screen">
      <header className="flex items-center justify-end gap-x-1 px-4 py-3">
        <LanguageToggle language={language} setLanguage={setLanguage} />
        <button
          type="button"
          onClick={toggle}
          aria-label={theme === 'dark' ? t('theme.toLight') : t('theme.toDark')}
          className="grid h-8 w-8 place-items-center rounded-lg text-muted transition hover:bg-edge hover:text-body"
        >
          <ThemeIcon className="h-4 w-4" aria-hidden />
        </button>
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
            <>
              <h1 className="mt-5 text-base font-semibold text-strong">{t('auth.login.title')}</h1>
              <p className="mt-1 text-sm text-muted">{t('auth.login.subtitle')}</p>
            </>
          )}

          {/* Google's own colours on a white surface in both themes — their branding rules require it,
              and a recoloured SSO button reads as a phishing page. */}
          <button
            type="button"
            onClick={signIn}
            className="mt-6 flex w-full items-center justify-center gap-x-2.5 rounded-lg border border-[#dadce0] bg-white px-4 py-2.5 text-sm font-medium text-[#1f1f1f] shadow-sm transition hover:bg-[#f8f9fa]"
          >
            <GoogleMark />
            {denied ? t('auth.denied.retry') : t('auth.login.google')}
          </button>

          <p className="mt-5 border-t border-edge pt-4 text-xs leading-relaxed text-subtle">
            {t('auth.login.fineprint')}
          </p>
        </div>
      </main>
    </div>
  );
}
