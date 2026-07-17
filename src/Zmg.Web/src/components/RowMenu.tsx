import { useCallback, useEffect, useRef, useState, type ReactNode } from 'react';
import { createPortal } from 'react-dom';

/**
 * The `⋮` row-actions button plus its dropdown. The menu renders with `fixed`
 * positioning computed from the button's rect, so it escapes the `overflow-hidden`
 * phase sections it lives inside (an `absolute` menu was clipped near a list's end).
 * It flips upward when there isn't room below. `children` receives a `close` fn.
 *
 * The dropdown portals to <body>: a `fixed` element inside a *transformed* ancestor resolves
 * against that ancestor instead of the viewport (and gets clipped by it), which put the menu
 * off-panel and under the backdrop when a card is rendered inside `Modal` (whose panel is
 * `-translate-x/y-1/2`). Portaling keeps `fixed` viewport-relative, so it must also sit above
 * the modal layer (z-40).
 */
export function RowMenu({
  children,
  label = 'Task actions',
}: {
  children: (close: () => void) => ReactNode;
  label?: string;
}) {
  const [open, setOpen] = useState(false);
  const [pos, setPos] = useState<React.CSSProperties | null>(null);
  const btnRef = useRef<HTMLButtonElement>(null);

  const close = useCallback(() => setOpen(false), []);

  function openMenu() {
    const rect = btnRef.current?.getBoundingClientRect();
    if (!rect) return;
    const estMenuHeight = 260; // generous estimate for up to ~6 items
    const spaceBelow = window.innerHeight - rect.bottom;
    const flipUp = spaceBelow < estMenuHeight && rect.top > spaceBelow;
    setPos({
      position: 'fixed',
      right: window.innerWidth - rect.right,
      ...(flipUp
        ? { bottom: window.innerHeight - rect.top + 4 }
        : { top: rect.bottom + 4 }),
    });
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
        aria-label={label}
        className="grid h-8 w-8 place-items-center rounded-lg text-slate-400 hover:bg-edge hover:text-slate-200"
        onClick={() => (open ? setOpen(false) : openMenu())}
      >
        ⋮
      </button>
      {open &&
        pos &&
        createPortal(
          <>
            <div className="fixed inset-0 z-50" onClick={close} />
            <div
              style={pos}
              className="z-50 w-44 overflow-hidden rounded-lg border border-edge bg-panel text-sm shadow-lg"
            >
              {children(close)}
            </div>
          </>,
          document.body,
        )}
    </>
  );
}

/** `tone` mirrors the Button variants: red for hard deletes, amber for archive. */
export function MenuItem({
  children,
  onClick,
  tone = 'default',
}: {
  children: ReactNode;
  onClick: () => void;
  tone?: 'default' | 'danger' | 'archive';
}) {
  const tones = {
    default: 'text-slate-200',
    danger: 'text-red-300',
    archive: 'text-amber-300',
  };
  return (
    <button className={`block w-full px-3 py-3 text-left hover:bg-edge ${tones[tone]}`} onClick={onClick}>
      {children}
    </button>
  );
}
