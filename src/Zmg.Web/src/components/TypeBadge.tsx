import { useTranslation } from 'react-i18next';
import { ReleaseType } from '@/types';

export function TypeBadge({ type }: { type: ReleaseType }) {
  const { t } = useTranslation();
  const label = type === ReleaseType.Album ? t('releaseType.album') : t('releaseType.single');
  return (
    <span className="rounded-full bg-edge px-2 py-0.5 text-xs font-medium text-body">
      {label}
    </span>
  );
}
