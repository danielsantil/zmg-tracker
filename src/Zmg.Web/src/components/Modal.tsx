import { useEffect, useRef, type ReactNode } from 'react';
import { createPortal } from 'react-dom';

/**
 * Overlay primitive: a bottom sheet on mobile, a centered card from `sm` up. Portals to
 * <body> so it escapes the transformed/overflow-hidden containers it's opened from.
 * Escape and a backdrop click both call `onClose`; body scroll is locked while open.
 */
export function Modal({
  open,
  onClose,
  title,
  children,
}: {
  open: boolean;
  onClose: () => void;
  title?: string;
  children: ReactNode;
}) {
  const panel = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!open) return;
    // Focus the panel for keyboard/Escape handling — but not if a child already claimed focus
    // (e.g. an `autoFocus` search input), since this effect runs after the child mounts and
    // would otherwise steal it back.
    if (!panel.current?.contains(document.activeElement)) panel.current?.focus();
    const onKey = (e: KeyboardEvent) => {
      if (e.key === 'Escape') onClose();
    };
    const prevOverflow = document.body.style.overflow;
    document.body.style.overflow = 'hidden';
    document.addEventListener('keydown', onKey);
    return () => {
      document.removeEventListener('keydown', onKey);
      document.body.style.overflow = prevOverflow;
    };
  }, [open, onClose]);

  if (!open) return null;

  return createPortal(
    <div className="fixed inset-0 z-40">
      <div className="fixed inset-0 bg-black/50" onClick={onClose} />
      <div
        ref={panel}
        tabIndex={-1}
        role="dialog"
        aria-modal="true"
        aria-label={title}
        className="fixed inset-x-0 bottom-0 max-h-[85vh] overflow-y-auto rounded-t-2xl border border-edge bg-panel p-5 shadow-xl outline-none sm:inset-x-auto sm:bottom-auto sm:left-1/2 sm:top-1/2 sm:w-full sm:max-w-md sm:-translate-x-1/2 sm:-translate-y-1/2 sm:rounded-2xl"
      >
        {title && <h2 className="mb-3 text-lg font-semibold text-white">{title}</h2>}
        {children}
      </div>
    </div>,
    document.body,
  );
}
