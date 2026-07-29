import { useEffect, useState } from 'react';
import { useSearchParams } from 'react-router-dom';

/** The server's one-shot signal that the last sign-in attempt was refused. */
const DENIED_PARAM = 'denied';

/**
 * Reads `?denied=1` **once** and strips it from the URL on the way past (post-v2.10 fix).
 *
 * The flag describes an event — "the attempt you just made was refused" — not a state of the page, so
 * leaving it in the address bar makes it outlive the thing it describes. Left there it did real
 * damage rather than looking untidy: `currentReturnPath()` captures `pathname + search + hash` when
 * the retry button is clicked, so `/?denied=1` travelled to the server as `returnUrl`, survived
 * `SafeLocalPath` (it is a legitimate same-origin path), and came back as the post-login redirect —
 * landing a *successfully* signed-in user on the dashboard still wearing the denial's URL. Clearing
 * it only after a good sign-in would have hidden that symptom while leaving the round trip intact.
 *
 * So it is consumed here, at the gate, on arrival: read into state (the banner must survive its own
 * URL being cleaned) and removed with `replace`, which keeps the Back button pointing wherever the
 * partner actually came from instead of at a dead denial URL. Living in the gate rather than in
 * `LoginPage` matters for one case — a remote failure while a session is *already* live redirects
 * here too, and that render never reaches the login screen.
 */
export function useDeniedFlag(): boolean {
  const [params, setParams] = useSearchParams();
  const [denied] = useState(() => params.get(DENIED_PARAM) === '1');

  useEffect(() => {
    if (!params.has(DENIED_PARAM)) return;
    const next = new URLSearchParams(params);
    next.delete(DENIED_PARAM);
    setParams(next, { replace: true });
  }, [params, setParams]);

  return denied;
}
