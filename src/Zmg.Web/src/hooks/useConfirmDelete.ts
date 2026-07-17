import { useQueryClient, type QueryKey } from '@tanstack/react-query';
import type { ConfirmOptions } from '@/components';
import { errorMessage } from '@/api';
import { useConfirm } from './useConfirm';

/**
 * The confirm → mutate → toast-on-failure → refresh shape the list pages repeat for every
 * archive/delete action (M24.6). With Query the old "reload the whole list" step becomes an
 * `invalidateQueries` over the keys the action affects.
 *
 * Returns a `run(item)` to wire onto a menu/button. The confirm options are built per item (so the
 * dialog can name it), returning early on cancel. `showToast` is the page's own — a page renders one
 * `<Toast>`, so failures from every action must flow through it rather than a hook-local instance.
 */
export function useConfirmDelete<T>(opts: {
  confirm: (item: T) => ConfirmOptions | Promise<ConfirmOptions>;
  mutate: (item: T) => Promise<void>;
  invalidate: QueryKey[];
  errorFallback: string;
  showToast: (msg: string) => void;
  onSuccess?: () => void;
}): (item: T) => Promise<void> {
  const confirm = useConfirm();
  const queryClient = useQueryClient();

  return async (item: T) => {
    if (!(await confirm(await opts.confirm(item)))) return;
    try {
      await opts.mutate(item);
      opts.invalidate.forEach((queryKey) => queryClient.invalidateQueries({ queryKey }));
      opts.onSuccess?.();
    } catch (e) {
      opts.showToast(errorMessage(e, opts.errorFallback));
    }
  };
}
