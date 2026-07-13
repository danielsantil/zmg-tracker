/**
 * Soft advisory glyph for a release missing UPC/ISRC after DSP distribution (v1.1 M7).
 * Amber, never a red error, never blocking — hover shows which id is missing.
 */
export function IdentifierWarning({ upc, isrc }: { upc?: string | null; isrc?: string | null }) {
  const missing = [!upc && 'UPC', !isrc && 'ISRC'].filter(Boolean).join(', ');
  return (
    <span
      title={missing ? `Missing ${missing}` : 'Missing identifier'}
      aria-label={missing ? `Missing ${missing}` : 'Missing identifier'}
      className="cursor-help text-amber-400"
    >
      ⚠
    </span>
  );
}
