import { useEffect, useRef, useState } from 'react';

/**
 * Soft advisory glyph for a release. A single amber icon carries every warning the release has
 * (e.g. "Missing UPC", "Album is empty"); clicking lists them all. Never a red error, never
 * blocking. It's a real button so the messages are reachable by tap on mobile (a hover-only `title`
 * was invisible on touch devices); the popover is positioned `fixed` from the button rect so it
 * escapes any `overflow-hidden` ancestor, dismissed by tapping the backdrop. Renders nothing when
 * there are no warnings, so callers can drop it in unconditionally.
 */
export function SoftWarning({ warnings }: { warnings: string[] }) {
  const [open, setOpen] = useState(false);
  const [pos, setPos] = useState<React.CSSProperties | null>(null);
  const btnRef = useRef<HTMLButtonElement>(null);

  function toggle() {
    if (open) {
      setOpen(false);
      return;
    }
    const rect = btnRef.current?.getBoundingClientRect();
    if (!rect) return;
    setPos({ position: 'fixed', top: rect.bottom + 4, left: rect.left });
    setOpen(true);
  }

  // Fixed coords don't track the page, so close on scroll/resize.
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

  if (warnings.length === 0) return null;
  const summary = warnings.join(', ');

  return (
    <>
      <button
        ref={btnRef}
        type="button"
        aria-label={summary}
        title={summary}
        onClick={(e) => {
          e.stopPropagation();
          e.preventDefault();
          toggle();
        }}
        className="grid h-8 w-8 shrink-0 place-items-center rounded-lg text-amber-400 hover:bg-edge"
      >
        ⚠
      </button>
      {open && pos && (
        <>
          <div
            className="fixed inset-0 z-20"
            onClick={(e) => {
              e.stopPropagation();
              e.preventDefault();
              setOpen(false);
            }}
          />
          <div
            style={pos}
            className="z-30 whitespace-nowrap rounded-lg border border-edge bg-panel px-3 py-2 text-sm text-amber-300 shadow-lg"
          >
            <ul className="space-y-0.5">
              {warnings.map((w) => (
                <li key={w}>{w}</li>
              ))}
            </ul>
          </div>
        </>
      )}
    </>
  );
}
