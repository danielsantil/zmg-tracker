import { cva } from 'class-variance-authority';

export type ToastVariant = 'success' | 'error' | 'info';

const toast = cva(
  // text-white (not text-strong): the variant backgrounds are saturated solids in both themes, so
  // the text must stay white — text-strong would flip to dark slate in light mode and vanish.
  'fixed bottom-4 left-1/2 z-20 flex -translate-x-1/2 items-center gap-2 rounded-lg px-4 py-2 text-sm text-white shadow-lg mb-[env(safe-area-inset-bottom)] motion-safe:animate-[toast-in_150ms_ease-out]',
  {
    variants: {
      variant: {
        success: 'bg-emerald-600/90',
        error: 'bg-red-500/90',
        info: 'bg-slate-700/90',
      },
    },
    defaultVariants: { variant: 'error' },
  },
);

/**
 * Fixed bottom-center advisory toast. Renders nothing when empty. `error` is the default so a
 * bare showToast(msg) stays red — most callers are revert/failure paths.
 */
export function Toast({ message, variant = 'error' }: { message: string | null; variant?: ToastVariant }) {
  if (!message) return null;
  return (
    <div role="status" aria-live="polite" className={toast({ variant })}>
      {variant === 'success' && <span aria-hidden="true">✓</span>}
      {message}
    </div>
  );
}
