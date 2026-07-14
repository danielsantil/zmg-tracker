import { useEffect, useRef, useState } from 'react';

/**
 * Soft advisory glyph for a release missing UPC/ISRC after DSP distribution (v1.1 M7).
 * Amber, never a red error, never blocking. It's a real button so the message is
 * reachable by tap on mobile (a hover-only `title` was invisible on touch devices);
 * clicking toggles a small popover, positioned `fixed` from the button rect so it
 * escapes any `overflow-hidden` ancestor, dismissed by tapping the backdrop.
 */
export function IdentifierWarning({ upc, isrc }: { upc?: string | null; isrc?: string | null }) {
  const missing = [!upc && 'UPC', !isrc && 'ISRC'].filter(Boolean).join(', ');
  const label = missing ? `Missing ${missing}` : 'Missing identifier';
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

  return (
    <>
      <button
        ref={btnRef}
        type="button"
        aria-label={label}
        title={label}
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
            {label}
          </div>
        </>
      )}
    </>
  );
}
