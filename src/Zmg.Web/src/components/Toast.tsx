/** Fixed bottom-center advisory toast (error/revert messages). Renders nothing when empty. */
export function Toast({ message }: { message: string | null }) {
  if (!message) return null;
  return (
    <div className="fixed bottom-4 left-1/2 z-20 -translate-x-1/2 rounded-lg bg-red-500/90 px-4 py-2 text-sm text-white shadow-lg">
      {message}
    </div>
  );
}
