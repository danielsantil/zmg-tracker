export type ToastVariant = 'success' | 'error' | 'info';

/**
 * Fixed bottom-center advisory toast. Renders nothing when empty. `error` is the default so a
 * bare showToast(msg) stays red — most callers are revert/failure paths.
 */
export function Toast({ message, variant = 'error' }: { message: string | null; variant?: ToastVariant }) {
  if (!message) return null;
  const variants = {
    success: 'bg-emerald-600/90',
    error: 'bg-red-500/90',
    info: 'bg-slate-700/90',
  };
  return (
    <div
      role="status"
      aria-live="polite"
      className={`fixed bottom-4 left-1/2 z-20 flex -translate-x-1/2 items-center gap-2 rounded-lg px-4 py-2 text-sm text-white shadow-lg mb-[env(safe-area-inset-bottom)] motion-safe:animate-[toast-in_150ms_ease-out] ${variants[variant]}`}
    >
      {variant === 'success' && <span aria-hidden="true">✓</span>}
      {message}
    </div>
  );
}
