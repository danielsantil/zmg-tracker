/**
 * The red error banner, unifying the two shapes it used to take (a single `string` ×8 and a `<ul>`
 * of `errors[]` ×4). Renders nothing when there's no error. A single string shows as one item; the
 * Tailwind reset strips the list marker, so it reads like the old single-line `<p>`.
 */
export function ErrorBanner({ error }: { error: string | string[] | null | undefined }) {
  const messages = error == null ? [] : Array.isArray(error) ? error : [error];
  if (messages.length === 0) return null;

  return (
    <ul className="mb-4 rounded-lg bg-danger/10 px-4 py-3 text-sm text-dangerFg">
      {messages.map((msg) => (
        <li key={msg}>{msg}</li>
      ))}
    </ul>
  );
}
