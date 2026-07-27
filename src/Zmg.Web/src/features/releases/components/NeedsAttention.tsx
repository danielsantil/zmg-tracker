import { Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useServerText } from '@/i18n/serverText';
import { useTaskText } from '@/hooks/useTaskText';
import type { PendingAction } from '@/types';
import { PendingKind } from '@/types';

/**
 * "Needs attention" block (M10; reworked M14) — this release's pending actions, computed server-side
 * from the loaded detail payload. Task-due items show days-to-release; the rolled-up "missing ISRC"
 * rows for the release's songs link into the catalog. Other data items (missing UPC, empty album) don't.
 */
export function NeedsAttention({ actions }: { actions: PendingAction[] }) {
  const { t } = useTranslation();
  const server = useServerText();
  const taskText = useTaskText();
  // TaskDue carries the task's own text in both languages (user content, verbatim); the data kinds
  // carry an advisory code. Each field means one thing, so nothing has to be disambiguated here.
  const labelOf = (a: PendingAction) =>
    a.kind === PendingKind.TaskDue
      ? taskText({ titleEn: a.taskTitleEn ?? '', titleEs: a.taskTitleEs })
      : server.code(a.warningCode ?? '');
  return (
    <div className="mb-6 overflow-hidden rounded-xl border border-warn/25 bg-warn/[0.06]">
      <div className="border-b border-warn/20 px-4 py-2.5 text-sm font-semibold text-warnFg">
        {t('releases.detail.needsAttention')}
      </div>
      <ul className="px-4 py-2">
        {actions.map((a, i) => (
          <li key={`${a.taskId ?? a.songId ?? a.kind}-${i}`} className="flex items-baseline gap-2 py-1 text-sm text-body">
            <span className="text-warnFg">•</span>
            {a.kind === PendingKind.MissingIsrc && a.songId ? (
              <Link to={`/catalog/${a.songId}`} className="text-body underline decoration-dotted hover:text-strong">
                {labelOf(a)} — {a.subject}
              </Link>
            ) : (
              <span>{labelOf(a)}</span>
            )}
            {a.kind === PendingKind.TaskDue && a.daysToRelease != null && (
              <span className="text-xs text-accent">
                —{' '}
                {a.daysToRelease === 0
                  ? t('pending.inline.releasesToday')
                  : t('pending.inline.daysToRelease', { count: a.daysToRelease })}
              </span>
            )}
          </li>
        ))}
      </ul>
    </div>
  );
}
