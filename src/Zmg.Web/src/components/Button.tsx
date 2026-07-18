import { cva, type VariantProps } from 'class-variance-authority';

// `archive` is terminal but not destructive — amber keeps red reserved for hard deletes.
const button = cva(
  'inline-flex items-center justify-center rounded-lg px-3 py-2 text-sm font-medium transition disabled:opacity-50 disabled:cursor-not-allowed',
  {
    variants: {
      variant: {
        primary: 'bg-accent text-strong hover:bg-accent/90',
        ghost: 'bg-edge text-body hover:bg-edge/70',
        danger: 'bg-red-500/15 text-red-300 ring-1 ring-red-500/30 hover:bg-red-500/25',
        archive: 'bg-amber-500/15 text-amber-300 ring-1 ring-amber-500/30 hover:bg-amber-500/25',
      },
    },
    defaultVariants: { variant: 'primary' },
  },
);

export function Button({
  children,
  variant,
  className,
  ...props
}: React.ButtonHTMLAttributes<HTMLButtonElement> & VariantProps<typeof button>) {
  // cva merges any passed className onto the variant classes (the old spread let className replace them).
  return (
    <button className={button({ variant, className })} {...props}>
      {children}
    </button>
  );
}
