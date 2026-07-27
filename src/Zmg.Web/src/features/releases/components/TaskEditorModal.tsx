import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Button, Field, Modal, inputClass, inputErrorClass } from '@/components';
import { PHASE_ORDER, phaseLabelKeys } from '@/lib/phase';
import { Phase } from '@/types';
import type { TaskDraft } from './taskDraft';

/**
 * The one editor for a checklist task, used by both the templates screen and a release's checklist,
 * for both add and edit (v2.9).
 *
 * Its reason for existing is that **both languages are entered explicitly**. While text was edited
 * inline, one field had to stand for two columns, so the server was left inferring which language an
 * edit belonged to and whether it was an edit at all — the source of most of v2.8's checklist bugs.
 * Two fields side by side make the question disappear rather than answering it better.
 *
 * Phase, timeframe and notes moved in here too, so a task has exactly one place it is edited: the
 * kebab's Edit item. That retires the inline rename, the inline notes editor and the inline timeframe
 * editor, along with the "Move to phase" shortcuts, which the phase select now covers.
 */
export function TaskEditorModal({
  open,
  mode,
  initial,
  supportsNotes = false,
  onSave,
  onClose,
}: {
  open: boolean;
  mode: 'add' | 'edit';
  initial: TaskDraft;
  supportsNotes?: boolean;
  onSave: (draft: TaskDraft) => void;
  onClose: () => void;
}) {
  const { t } = useTranslation();
  const [draft, setDraft] = useState(initial);
  const [touched, setTouched] = useState(false);

  // Reseed whenever the modal is opened on a different task; keeping it keyed on `initial` alone
  // would carry the previous task's text into the next one.
  useEffect(() => {
    if (open) {
      setDraft(initial);
      setTouched(false);
    }
  }, [open, initial]);

  const titleMissing = !draft.titleEn.trim();
  const set = <K extends keyof TaskDraft>(key: K, value: TaskDraft[K]) =>
    setDraft((d) => ({ ...d, [key]: value }));

  function submit(e: React.FormEvent) {
    e.preventDefault();
    setTouched(true);
    if (titleMissing) return;
    onSave({
      ...draft,
      titleEn: draft.titleEn.trim(),
      titleEs: draft.titleEs.trim(),
      notes: draft.notes.trim(),
    });
  }

  // Blank clears the bound; anything non-numeric or negative is simply not a timeframe.
  function parseDays(value: string): number | null {
    const n = parseInt(value, 10);
    return Number.isFinite(n) && n >= 0 ? n : null;
  }

  return (
    <Modal open={open} onClose={onClose} title={t(mode === 'add' ? 'tasks.editor.addTitle' : 'tasks.editor.editTitle')}>
      <form className="space-y-4" onSubmit={submit}>
        <Field label={t('tasks.editor.phase')}>
          <select
            className={inputClass}
            value={draft.phase}
            onChange={(e) => set('phase', Number(e.target.value) as Phase)}
          >
            {PHASE_ORDER.map((phase) => (
              <option key={phase} value={phase}>
                {t(phaseLabelKeys[phase])}
              </option>
            ))}
          </select>
        </Field>

        <Field
          label={t('tasks.editor.titleEn')}
          error={touched && titleMissing ? t('tasks.editor.titleEnRequired') : undefined}
        >
          <input
            autoFocus
            className={`${inputClass} ${touched && titleMissing ? inputErrorClass : ''}`}
            value={draft.titleEn}
            onChange={(e) => set('titleEn', e.target.value)}
          />
        </Field>

        <Field label={t('tasks.editor.titleEs')} hint={t('tasks.editor.titleEsHint')}>
          <input
            className={inputClass}
            value={draft.titleEs}
            onChange={(e) => set('titleEs', e.target.value)}
          />
        </Field>

        {draft.phase === Phase.Pre && (
          <Field label={t('tasks.editor.timeframe')} hint={t('tasks.editor.timeframeHint')}>
            <div className="flex items-center gap-2">
              <input
                type="number"
                min={0}
                aria-label={t('tasks.timeframeEditor.min')}
                placeholder={t('tasks.timeframeEditor.min')}
                className={`${inputClass} w-24`}
                value={draft.minDaysBefore ?? ''}
                onChange={(e) => set('minDaysBefore', parseDays(e.target.value))}
              />
              <span className="text-subtle">–</span>
              <input
                type="number"
                min={0}
                aria-label={t('tasks.timeframeEditor.max')}
                placeholder={t('tasks.timeframeEditor.max')}
                className={`${inputClass} w-24`}
                value={draft.maxDaysBefore ?? ''}
                onChange={(e) => set('maxDaysBefore', parseDays(e.target.value))}
              />
            </div>
          </Field>
        )}

        {supportsNotes && (
          <Field label={t('tasks.editor.notes')}>
            <textarea
              rows={2}
              className={inputClass}
              placeholder={t('tasks.notesPlaceholder')}
              value={draft.notes}
              onChange={(e) => set('notes', e.target.value)}
            />
          </Field>
        )}

        <div className="flex justify-end gap-2 pt-1">
          <Button type="button" variant="ghost" onClick={onClose}>
            {t('common.cancel')}
          </Button>
          <Button type="submit">{t('common.save')}</Button>
        </div>
      </form>
    </Modal>
  );
}
