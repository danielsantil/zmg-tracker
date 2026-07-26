import { useEffect, useState } from "react";

/** The one loading placeholder for a whole page/section (was 13 hand-written copies). */
export function Loading() {
  const [slow, setSlow] = useState(false);

  useEffect(() => {
    const timer = setTimeout(() => setSlow(true), 4000);
    return () => clearTimeout(timer);
  }, []);

  return (
    <div className="text-muted">
      <p>Loading...</p>
      {slow && <p className="mt-2">Still loading...</p>}
    </div>
  );
}
