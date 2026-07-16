export function Button({
  children,
  variant = 'primary',
  ...props
}: React.ButtonHTMLAttributes<HTMLButtonElement> & {
  variant?: 'primary' | 'ghost' | 'danger' | 'archive';
}) {
  const base =
    'inline-flex items-center justify-center rounded-lg px-3 py-2 text-sm font-medium transition disabled:opacity-50 disabled:cursor-not-allowed';
  // `archive` is terminal but not destructive — amber keeps red reserved for hard deletes.
  const variants = {
    primary: 'bg-accent text-white hover:bg-accent/90',
    ghost: 'bg-edge text-slate-200 hover:bg-edge/70',
    danger: 'bg-red-500/15 text-red-300 ring-1 ring-red-500/30 hover:bg-red-500/25',
    archive: 'bg-amber-500/15 text-amber-300 ring-1 ring-amber-500/30 hover:bg-amber-500/25',
  };
  return (
    <button className={`${base} ${variants[variant]}`} {...props}>
      {children}
    </button>
  );
}
