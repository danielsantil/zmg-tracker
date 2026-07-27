import type { TFunction } from 'i18next';
import { useTranslation } from 'react-i18next';
import i18n from './index';

/**
 * One server-minted message: a culture-invariant code plus the values it interpolates (M46). The API
 * ships no prose at all — `code` is an i18next key path (`error.song.duplicateTitle`,
 * `warning.missingUpc`), so rendering it is `t(code, args)` with no translation table in between.
 */
export interface ServerMessage {
  code: string;
  args?: Record<string, string> | null;
}

/**
 * Renders a server code through a `t`. This is the one place the `ParseKeys` typing has to give way:
 * a code arrives as *data* at runtime, so it can't be literal-checked and the widened signature is
 * honest about that. The `exists` guard is what replaces the lost type safety — it degrades to
 * showing the raw code rather than a blank when the server ships a key the bundle lacks (an API
 * deployed ahead of the SPA), which is a visible, greppable failure instead of a silent one.
 */
export function renderCode(t: TFunction, code: string, args?: Record<string, string> | null): string {
  const translate = t as unknown as (key: string, options?: Record<string, string>) => string;
  return i18n.exists(code) ? translate(code, args ?? undefined) : code;
}

/** Non-component version, for `api/client.ts` — it builds `ApiError` outside any React tree. */
export function translateMessage(m: ServerMessage): string {
  return renderCode(i18n.t, m.code, m.args);
}

/**
 * Component-side counterpart, bound to `useTranslation`'s `t` so a language switch re-renders the
 * text (the module-level `i18n.t` above wouldn't). Used wherever a code arrives as *data* rather
 * than through a thrown error: release warnings, pending actions, the create-form advisories.
 */
export function useServerText() {
  const { t } = useTranslation();
  return {
    code: (code: string, args?: Record<string, string> | null) => renderCode(t, code, args),
    message: (m: ServerMessage) => renderCode(t, m.code, m.args),
  };
}
