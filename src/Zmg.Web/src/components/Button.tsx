import { cva, type VariantProps } from 'class-variance-authority';

// `archive` is terminal but not destructive — amber keeps red reserved for hard deletes.
const button = cva(
  'inline-flex items-center justify-center rounded-lg px-3 py-2 text-sm font-medium transition disabled:opacity-50 disabled:cursor-not-allowed',
  {
    variants: {
      variant: {
        // Solid accent → white text in BOTH themes (text-strong flips to dark in light mode).
        primary: 'bg-accent text-white hover:bg-accent/90',
        ghost: 'bg-edge text-body hover:bg-edge/70',
        danger: 'bg-danger/15 text-dangerFg ring-1 ring-danger/30 hover:bg-danger/25',
        archive: 'bg-warn/15 text-warnFg ring-1 ring-warn/30 hover:bg-warn/25',
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
