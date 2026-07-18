import { useEffect, useRef, useState } from 'react';
import { createPortal } from 'react-dom';

/**
 * Soft advisory glyph for a release. A single amber icon carries every warning the release has
 * (e.g. "Missing UPC", "Album is empty"); clicking lists them all. Never a red error, never
 * blocking. It's a real button so the messages are reachable by tap on mobile (a hover-only `title`
 * was invisible on touch devices); the popover is positioned `fixed` from the button rect so it
 * escapes any `overflow-hidden` ancestor, dismissed by tapping the backdrop. Renders nothing when
 * there are no warnings, so callers can drop it in unconditionally.
 *
 * Portals to <body> for the same reason as `RowMenu`: `fixed` resolves against a transformed
 * ancestor (`Modal`'s panel), not the viewport, so an in-place popover lands off-panel and under
 * the backdrop. It therefore also has to clear the modal layer (z-40).
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
        className="grid h-8 w-8 shrink-0 place-items-center rounded-lg text-warnFg hover:bg-edge"
      >
        ⚠
      </button>
      {open &&
        pos &&
        createPortal(
          <>
            <div
              className="fixed inset-0 z-50"
              onClick={(e) => {
                e.stopPropagation();
                e.preventDefault();
                setOpen(false);
              }}
            />
            <div
              style={pos}
              className="z-50 whitespace-nowrap rounded-lg border border-edge bg-panel px-3 py-2 text-sm text-warnFg shadow-lg"
            >
              <ul className="space-y-0.5">
                {warnings.map((w) => (
                  <li key={w}>{w}</li>
                ))}
              </ul>
            </div>
          </>,
          document.body,
        )}
    </>
  );
}
