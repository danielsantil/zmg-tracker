import { useEffect, type ReactNode } from 'react';
import { Button } from './Button';
import { Modal } from './Modal';

export type ConfirmOptions = {
  title: string;
  body?: ReactNode;
  confirmLabel?: string;
  cancelLabel?: string;
  /** `danger` (red) for hard deletes, `archive` (amber) for the terminal-but-not-destructive archive. */
  confirmVariant?: 'primary' | 'danger' | 'archive';
  /** Hide the Cancel button — for an info-only modal (single OK, result ignored). */
  hideCancel?: boolean;
};

/**
 * The single dialog instance driven by `useConfirm`'s provider — not mounted directly by pages.
 * Enter confirms; Escape and the backdrop cancel (via Modal's onClose).
 */
export function ConfirmDialog({
  options,
  onResolve,
}: {
  options: ConfirmOptions | null;
  onResolve: (confirmed: boolean) => void;
}) {
  const open = options !== null;

  useEffect(() => {
    if (!open) return;
    const onKey = (e: KeyboardEvent) => {
      if (e.key === 'Enter') onResolve(true);
    };
    document.addEventListener('keydown', onKey);
    return () => document.removeEventListener('keydown', onKey);
  }, [open, onResolve]);

  return (
    <Modal open={open} onClose={() => onResolve(false)} title={options?.title}>
      {options?.body && <div className="text-sm text-body">{options.body}</div>}
      <div className="mt-5 flex justify-end gap-2">
        {!options?.hideCancel && (
          <Button variant="ghost" onClick={() => onResolve(false)}>
            {options?.cancelLabel ?? 'Cancel'}
          </Button>
        )}
        <Button variant={options?.confirmVariant ?? 'primary'} onClick={() => onResolve(true)}>
          {options?.confirmLabel ?? 'Confirm'}
        </Button>
      </div>
    </Modal>
  );
}
