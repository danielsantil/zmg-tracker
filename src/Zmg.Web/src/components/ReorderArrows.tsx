/**
 * The inline ↑/↓ reorder pair used by every ordered list (checklist rows, tracklist).
 * Reordering is move-up/move-down by rule — not drag-and-drop — so this is the one control.
 */
export function ReorderArrows({
  isFirst,
  isLast,
  onMove,
}: {
  isFirst: boolean;
  isLast: boolean;
  onMove: (dir: -1 | 1) => void;
}) {
  return (
    <div className="flex shrink-0 items-center gap-1">
      <button
        type="button"
        aria-label="Move up"
        disabled={isFirst}
        onClick={() => onMove(-1)}
        className="px-1.5 text-slate-500 hover:text-slate-300 disabled:opacity-30"
      >
        ↑
      </button>
      <button
        type="button"
        aria-label="Move down"
        disabled={isLast}
        onClick={() => onMove(1)}
        className="px-1.5 text-slate-500 hover:text-slate-300 disabled:opacity-30"
      >
        ↓
      </button>
    </div>
  );
}
