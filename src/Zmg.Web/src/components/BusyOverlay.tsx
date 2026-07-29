import { useEffect, useState } from 'react';
import { Loader2 } from 'lucide-react';

/**
 * Full-screen "something is in flight" state, for waits the app cannot shorten (post-v2.10 fix).
 *
 * Two of those exist, and both are whole-page rather than sectional — which is why this is a
 * primitive and not two hand-written copies: the hand-off to Google's consent screen, and the auth
 * probe that has to answer before the app can render at all. `Loading` is the sectional counterpart
 * and stays that; this one covers the viewport.
 *
 * Borrows the existing vocabulary rather than inventing one: `Modal`'s backdrop and panel surface,
 * `CoverField`'s spinner, `Loading`'s "still going" escalation — here generalized into a sequence.
 * It is deliberately not a `Modal` — there is nothing to dismiss, Escape must not close it, and
 * blocking what is underneath is the point.
 *
 * **Every message must stay generic.** On the probe path this screen is visible precisely while the
 * app does not yet know who — or whether — you are, so wording that narrates the check ("verifying
 * your account", "checking permissions") would describe the authentication flow to whoever is
 * looking, signed in or not. Say the site is loading; say nothing about why.
 *
 * **`messages` cycles, so no line may become false on the second pass.** It advances one step every
 * `stepMs` and wraps, which rules out anything that claims a position rather than describing work:
 * "almost ready" is a promise that expires, and a closer like "thanks for your patience" reads as an
 * ending and then doesn't end. Every entry has to be equally true at second 4 and at second 40 —
 * that is also what keeps the wrap from announcing itself.
 *
 * `delayMs` exists for the warm case: a probe against a live container answers in ~40ms, and a
 * backdrop that flashes for one frame reads as jank rather than as feedback. Below the delay nothing
 * renders at all, so the fast path looks exactly as it did before this component existed. A single
 * `messages` entry simply never rotates, which is what the short waits want.
 */
export function BusyOverlay({
  messages,
  delayMs = 0,
  stepMs = 3000,
}: {
  messages: string[];
  delayMs?: number;
  stepMs?: number;
}) {
  const [visible, setVisible] = useState(delayMs === 0);
  const [step, setStep] = useState(0);

  useEffect(() => {
    if (delayMs === 0) return;
    const timer = setTimeout(() => setVisible(true), delayMs);
    return () => clearTimeout(timer);
  }, [delayMs]);

  // One timeout per step rather than a repeating interval, so the cadence restarts cleanly from each
  // render. A single-entry list has nothing to advance to and schedules nothing at all — the short
  // waits pass one message and never pay for a timer.
  useEffect(() => {
    if (messages.length < 2) return;
    const timer = setTimeout(() => setStep((s) => s + 1), stepMs);
    return () => clearTimeout(timer);
  }, [step, messages.length, stepMs]);

  if (!visible) return null;

  return (
    <div
      className="fixed inset-0 z-40 grid place-items-center bg-black/50 px-4"
      role="status"
      aria-live="polite"
    >
      <div className="flex items-center gap-x-3 rounded-xl border border-edge bg-panel px-5 py-4 text-sm text-body shadow-xl">
        <Loader2 className="h-4 w-4 shrink-0 animate-spin text-muted" aria-hidden />
        <span>{messages[step % messages.length]}</span>
      </div>
    </div>
  );
}
