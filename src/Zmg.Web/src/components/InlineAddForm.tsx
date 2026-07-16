import { useState } from 'react';
import { Button } from './Button';
import { inputClass } from './Field';

/**
 * The "+ Add …" row shared by task/track lists: a trigger button that swaps to a
 * text input with Add/Cancel, Enter to submit, Escape to cancel. Manages its own
 * open/draft state; the parent only receives the trimmed title via `onAdd`.
 *
 * Every button is explicitly `type="button"`: M18 renders this inside the create form's <form>,
 * where the default `submit` type would save the release instead of adding a track.
 */
export function InlineAddForm({
  addLabel,
  placeholder,
  onAdd,
}: {
  addLabel: string;
  placeholder: string;
  onAdd: (title: string) => void;
}) {
  const [adding, setAdding] = useState(false);
  const [value, setValue] = useState('');

  function submit() {
    const title = value.trim();
    if (!title) return;
    onAdd(title);
    setValue('');
    setAdding(false);
  }

  function cancel() {
    setAdding(false);
    setValue('');
  }

  return (
    <div className="border-t border-edge px-3 py-2">
      {adding ? (
        <div className="flex gap-2">
          <input
            autoFocus
            className={inputClass}
            placeholder={placeholder}
            value={value}
            onChange={(e) => setValue(e.target.value)}
            onKeyDown={(e) => {
              if (e.key === 'Enter') submit();
              if (e.key === 'Escape') cancel();
            }}
          />
          <Button type="button" onClick={submit}>
            Add
          </Button>
          <Button type="button" variant="ghost" onClick={cancel}>
            Cancel
          </Button>
        </div>
      ) : (
        <button
          type="button"
          className="rounded-lg px-2 py-1.5 text-sm text-slate-400 hover:text-accent"
          onClick={() => setAdding(true)}
        >
          {addLabel}
        </button>
      )}
    </div>
  );
}
