import { useEffect, useState } from "react";
import { useTranslation } from 'react-i18next';

/** The one loading placeholder for a whole page/section (was 13 hand-written copies). */
export function Loading() {
  const { t } = useTranslation();
  const [slow, setSlow] = useState(false);

  useEffect(() => {
    const timer = setTimeout(() => setSlow(true), 4000);
    return () => clearTimeout(timer);
  }, []);

  return (
    <div className="text-muted">
      <p>{t('common.loading')}</p>
      {slow && <p className="mt-2">{t('common.stillLoading')}</p>}
    </div>
  );
}
