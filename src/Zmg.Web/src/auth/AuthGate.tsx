import type { ReactNode } from 'react';
import { useTranslation } from 'react-i18next';
import { useAuth } from './useAuth';
import { useDeniedFlag } from './useDeniedFlag';
import { BusyOverlay } from '@/components';
import LoginPage from '@/features/auth/LoginPage';

/**
 * One gate around the entire app (v2.10/M56).
 *
 * Authorization is flat — on the whitelist means full access — so there is nothing to guard *per
 * route*. A route-by-route guard would be ceremony that can only ever be wrong in one direction, and
 * the direction it fails in is "left a page unprotected".
 *
 * Rendering the login screen *in place*, without navigating, is what makes `returnUrl` unnecessary on
 * the client: the URL never changes, so after signing in the browser is already where the partner was
 * trying to go. The API is the real boundary regardless — every `/api/*` call answers 401 without a
 * session, so this gate is a UX affordance rather than a security control.
 */
export function AuthGate({ children }: { children: ReactNode }) {
  const { t } = useTranslation();
  const { status } = useAuth();

  // Consumed unconditionally, before the status branch: the gate is the one component that mounts on
  // every arrival, signed in or out, so this is where `?denied=1` can be guaranteed to be read once
  // and cleared. Hooks cannot live below an early return anyway.
  const denied = useDeniedFlag();

  // Nothing renders until the probe answers, and that wait is the ACA cold start — up to ~20s, during
  // which M42's edge-served shell was painting a blank screen and undoing the only change that ever
  // improved cold start (M40/M41: the 16–22s is Azure sandbox provisioning, with no knob in code).
  //
  // Waiting is the deliberate choice over painting something optimistically. A hint — localStorage,
  // or a cookie the edge can see — is a *guess*: revoke a session in the database and the guess still
  // says "signed in" until the container contradicts it, showing app chrome to someone who was just
  // cut off. No data leaks either way (every /api/* answers 401), but the wrong screen for 20s is a
  // trust problem, and the only source of truth for "may this person be here" is the API.
  //
  // So the wait stays and is made legible instead: `delayMs` keeps the warm path (~40ms) unchanged —
  // the old comment here was right that a one-frame flash reads as jank — and anything slower gets a
  // backdrop that says the site is loading. It is 20s either way; it no longer looks broken.
  if (status === 'loading') {
    return (
      <div className="min-h-screen" aria-busy="true">
        {/* Five lines on a 20s cycle, against a 17–22s cold start: most waits end inside one pass, and
            the ones that don't wrap without it reading as a loop. Each describes work rather than
            progress, and none names what is actually being waited on — see BusyOverlay. */}
        <BusyOverlay
          delayMs={400}
          messages={[
            t('common.loadingSite'),
            t('common.stillLoading'),
            t('common.loadingPreparing'),
            t('common.loadingTouches'),
            t('common.loadingSetting'),
          ]}
        />
      </div>
    );
  }

  if (status === 'out') return <LoginPage denied={denied} />;

  return <>{children}</>;
}
