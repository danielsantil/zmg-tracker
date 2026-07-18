import type { ReactNode } from 'react';

export const inputClass =
  'w-full rounded-lg border border-edge bg-panel px-3 py-2 text-sm text-strong outline-none focus:border-accent';

// Applied on top of inputClass when a field fails client-side validation.
export const inputErrorClass = 'border-red-500 focus:border-red-500';

export function Field({
  label,
  children,
  hint,
  error,
}: {
  label: string;
  children: ReactNode;
  hint?: string;
  error?: string;
}) {
  return (
    <label className="block">
      <span className="mb-1 block text-sm font-medium text-body">{label}</span>
      {children}
      {error ? (
        <span className="mt-1 block text-xs text-red-400">{error}</span>
      ) : (
        hint && <span className="mt-1 block text-xs text-subtle">{hint}</span>
      )}
    </label>
  );
}
