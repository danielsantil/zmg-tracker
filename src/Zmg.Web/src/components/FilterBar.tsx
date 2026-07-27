import type { ReactNode } from 'react';
import { useTranslation } from 'react-i18next';
import type { Artist, ReleaseStatus } from '@/types';
import { Button } from './Button';
import { inputClass } from './Field';
import { statusLabelKeys } from '@/lib/status';

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
  const { t } = useTranslation();
  return (
    <div className="mb-5 flex flex-wrap items-center gap-3">
      {children}
      {onClear && (
        <Button variant="ghost" onClick={onClear}>
          {t('common.clear')}
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
  const { t } = useTranslation();
  return (
    <select className={`${inputClass} max-w-[12rem]`} value={value} onChange={(e) => onChange(e.target.value)}>
      <option value="">{t('filters.allArtists')}</option>
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
  const { t } = useTranslation();
  return (
    <select className={`${inputClass} max-w-[10rem]`} value={value} onChange={(e) => onChange(e.target.value)}>
      <option value="">{t('filters.allTypes')}</option>
      <option value="0">{t('releaseType.single')}</option>
      <option value="1">{t('releaseType.album')}</option>
    </select>
  );
}

/** Status filter; the available statuses differ per page, so callers pass them (as server codes). */
export function StatusSelect({
  value,
  onChange,
  options,
}: {
  value: string;
  onChange: (v: string) => void;
  options: ReleaseStatus[];
}) {
  const { t } = useTranslation();
  return (
    <select className={`${inputClass} max-w-[10rem]`} value={value} onChange={(e) => onChange(e.target.value)}>
      <option value="">{t('filters.allStatuses')}</option>
      {options.map((s) => (
        <option key={s} value={s}>
          {t(statusLabelKeys[s])}
        </option>
      ))}
    </select>
  );
}

/** Debounced-search text input for the list pages (feed the value through `useDebouncedValue`). */
export function SearchInput({
  value,
  onChange,
  placeholder,
}: {
  value: string;
  onChange: (v: string) => void;
  placeholder?: string;
}) {
  const { t } = useTranslation();
  return (
    <input
      className={`${inputClass} max-w-[16rem]`}
      placeholder={placeholder ?? t('filters.searchByTitle')}
      value={value}
      onChange={(e) => onChange(e.target.value)}
    />
  );
}
