import { useRef, useState } from 'react';
import { ImagePlus, Loader2 } from 'lucide-react';
import clsx from 'clsx';
import { api, errorMessage } from '@/api';
import { Button, inputClass } from '@/components';

const MAX_BYTES = 5 * 1024 * 1024;
const ALLOWED_TYPES = ['image/png', 'image/jpeg', 'image/webp'];

/**
 * The release cover control (M31): an empty tile that opens the file picker, plus a "paste an image
 * URL" link for an inline URL. Both routes POST to the server, which stores the image in R2 and
 * answers with its public URL — so `coverUrl` is always ours, never a hotlink to someone else's host.
 * Filled state is the thumbnail with Replace / Remove. Identical in create and edit.
 */
export function CoverField({
  value,
  onChange,
  onUploadingChange,
}: {
  value: string;
  onChange: (url: string) => void;
  /** Lets the form block its submit while an upload is in flight — saving now would persist the
      previous coverUrl (or none) and silently orphan the image being stored. */
  onUploadingChange?: (uploading: boolean) => void;
}) {
  const fileInput = useRef<HTMLInputElement>(null);
  const [uploading, setUploading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [urlOpen, setUrlOpen] = useState(false);
  const [url, setUrl] = useState('');

  function setBusy(busy: boolean) {
    setUploading(busy);
    onUploadingChange?.(busy);
  }

  async function upload(send: () => Promise<{ url: string }>) {
    setBusy(true);
    setError(null);
    try {
      const stored = await send();
      onChange(stored.url);
      setUrlOpen(false);
      setUrl('');
    } catch (e) {
      // The form stays usable — a failed cover never blocks saving the release.
      setError(errorMessage(e, 'Could not upload that image.'));
    } finally {
      setBusy(false);
    }
  }

  // Mirrors the server's guards so an obviously bad file never costs a round trip.
  function pickFile(e: React.ChangeEvent<HTMLInputElement>) {
    const file = e.target.files?.[0];
    e.target.value = ''; // so re-picking the same file still fires change
    if (!file) return;
    if (!ALLOWED_TYPES.includes(file.type)) {
      setError('Cover must be a PNG, JPEG or WebP image.');
      return;
    }
    if (file.size > MAX_BYTES) {
      setError('Cover image must be 5 MB or smaller.');
      return;
    }
    void upload(() => api.uploads.cover(file));
  }

  function submitUrl() {
    if (!url.trim() || uploading) return;
    void upload(() => api.uploads.coverFromUrl(url.trim()));
  }

  return (
    <div className="block">
      <span className="mb-1 block text-sm font-medium text-body">Cover</span>

      <div className="flex items-start gap-3">
        <button
          type="button"
          onClick={() => fileInput.current?.click()}
          disabled={uploading}
          aria-label={value ? 'Replace cover image' : 'Upload cover image'}
          className={clsx(
            'grid h-24 w-24 shrink-0 place-items-center overflow-hidden rounded-lg border border-edge bg-panel transition',
            !uploading && 'hover:border-accent',
            uploading && 'cursor-wait',
          )}
        >
          {uploading ? (
            <Loader2 className="h-5 w-5 animate-spin text-muted" />
          ) : value ? (
            <img src={value} alt="" className="h-full w-full object-cover" />
          ) : (
            <ImagePlus className="h-6 w-6 text-subtle" />
          )}
        </button>

        <div className="min-w-0 flex-1">
          {value ? (
            <div className="flex flex-wrap gap-2">
              <Button type="button" variant="ghost" onClick={() => fileInput.current?.click()} disabled={uploading}>
                Replace
              </Button>
              <Button
                type="button"
                variant="ghost"
                onClick={() => {
                  onChange('');
                  setError(null);
                }}
                disabled={uploading}
              >
                Remove
              </Button>
            </div>
          ) : urlOpen ? (
            <div className="flex gap-2">
              <input
                className={inputClass}
                value={url}
                onChange={(e) => setUrl(e.target.value)}
                onKeyDown={(e) => {
                  if (e.key === 'Enter') {
                    e.preventDefault(); // Enter inside a form would submit the release instead
                    submitUrl();
                  }
                }}
                placeholder="https://…"
                autoFocus
              />
              <Button type="button" onClick={submitUrl} disabled={uploading || !url.trim()}>
                Use
              </Button>
            </div>
          ) : (
            <p className="text-xs text-subtle">
              PNG, JPEG or WebP, up to 5 MB — or{' '}
              <button
                type="button"
                className="text-accent underline underline-offset-2"
                onClick={() => setUrlOpen(true)}
              >
                paste an image URL
              </button>
              .
            </p>
          )}

          {error && <p className="mt-2 text-xs text-dangerFg">{error}</p>}
        </div>
      </div>

      <input
        ref={fileInput}
        type="file"
        accept={ALLOWED_TYPES.join(',')}
        className="hidden"
        onChange={pickFile}
      />
    </div>
  );
}
