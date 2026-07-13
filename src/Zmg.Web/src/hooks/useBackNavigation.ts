import { useCallback } from 'react';
import { useLocation, useNavigate } from 'react-router-dom';

/**
 * Returns a `goBack` that pops history when there is somewhere to go back to,
 * or falls back to Home when the detail/form page was opened directly (deep link
 * or refresh), where `location.key === 'default'`.
 */
export function useBackNavigation() {
  const navigate = useNavigate();
  const location = useLocation();

  return useCallback(() => {
    if (location.key === 'default') {
      navigate('/');
    } else {
      navigate(-1);
    }
  }, [navigate, location.key]);
}
