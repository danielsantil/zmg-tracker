import type { ReactNode } from 'react';
import type { Artist } from '@/types';
import { Button } from './Button';
import { inputClass } from './Field';

/**
 * The filter row shared by Home / All Releases / Catalog: a wrapping flex of controls, an optional
 * Clear button (shown when `onClear` is set — i.e. some filter is active), and an optional trailing
 * node (Catalog's "Archived Songs →" link floats right via its own `ml-auto`). The `hasFilters`
 * memo the three pages each hand-rolled becomes "pass onClear only when filters are active".
 */
export function FilterBar({
  children,
  onClear,
  trailing,
}: {
  children: ReactNode;
  onClear?: () => void;
  trailing?: ReactNode;
}) {
  return (
    <div className="mb-5 flex flex-wrap items-center gap-3">
      {children}
      {onClear && (
        <Button variant="ghost" onClick={onClear}>
          Clear
        </Button>
      )}
      {trailing}
    </div>
  );
}

/** The "All artists" + roster select shared by every filter bar. */
export function ArtistSelect({
  artists,
  value,
  onChange,
}: {
  artists: Artist[];
  value: string;
  onChange: (id: string) => void;
}) {
  return (
    <select className={`${inputClass} max-w-[12rem]`} value={value} onChange={(e) => onChange(e.target.value)}>
      <option value="">All artists</option>
      {artists.map((a) => (
        <option key={a.id} value={a.id}>
          {a.name}
        </option>
      ))}
    </select>
  );
}

/** Single/Album type filter. Value is the enum as a string ('' = all), matching the query param. */
export function TypeSelect({ value, onChange }: { value: string; onChange: (v: string) => void }) {
  return (
    <select className={`${inputClass} max-w-[10rem]`} value={value} onChange={(e) => onChange(e.target.value)}>
      <option value="">All types</option>
      <option value="0">Single</option>
      <option value="1">Album</option>
    </select>
  );
}

/** Status filter; the available statuses differ per page, so callers pass them. */
export function StatusSelect({
  value,
  onChange,
  options,
}: {
  value: string;
  onChange: (v: string) => void;
  options: string[];
}) {
  return (
    <select className={`${inputClass} max-w-[10rem]`} value={value} onChange={(e) => onChange(e.target.value)}>
      <option value="">All statuses</option>
      {options.map((s) => (
        <option key={s} value={s}>
          {s}
        </option>
      ))}
    </select>
  );
}

/** Debounced-search text input for the list pages (feed the value through `useDebouncedValue`). */
export function SearchInput({
  value,
  onChange,
  placeholder = 'Search by title…',
}: {
  value: string;
  onChange: (v: string) => void;
  placeholder?: string;
}) {
  return (
    <input
      className={`${inputClass} max-w-[16rem]`}
      placeholder={placeholder}
      value={value}
      onChange={(e) => onChange(e.target.value)}
    />
  );
}
