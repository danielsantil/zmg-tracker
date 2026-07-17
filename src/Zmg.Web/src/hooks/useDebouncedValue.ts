import { useEffect, useState } from 'react';

/**
 * The value, settling to a new one only after `delay` ms of quiet. Feeds a debounced value into a
 * query key so each keystroke of a search box doesn't fire a request — replaces the hand-rolled
 * `setTimeout`/`clearTimeout` blocks the list pages and the song picker each used to carry (and the
 * `eslint-disable react-hooks/exhaustive-deps` that came with them).
 *
 * `delay` defaults to 250ms; pass 0 to settle immediately (browse-on-open, where the first paint
 * shouldn't wait).
 */
export function useDebouncedValue<T>(value: T, delay = 250): T {
  const [debounced, setDebounced] = useState(value);

  useEffect(() => {
    if (delay === 0) {
      setDebounced(value);
      return;
    }
    const t = setTimeout(() => setDebounced(value), delay);
    return () => clearTimeout(t);
  }, [value, delay]);

  return debounced;
}
