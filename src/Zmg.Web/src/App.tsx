import { NavLink, Route, Routes } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { ConfirmProvider } from './hooks/ConfirmProvider';
import Home from './features/home/HomePage';
import AllReleases from './features/releases/AllReleasesPage';
import ArchivedReleases from './features/releases/ArchivedReleasesPage';
import Artists from './features/artists/ArtistsPage';
import ArtistForm from './features/artists/ArtistFormPage';
import ReleaseForm from './features/releases/ReleaseFormPage';
import ReleaseDetail from './features/releases/ReleaseDetailPage';
import Catalog from './features/catalog/CatalogPage';
import ArchivedSongs from './features/catalog/ArchivedSongsPage';
import SongForm from './features/catalog/SongFormPage';
import SongDetail from './features/catalog/SongDetailPage';
import Templates from './features/templates/TemplatesPage';

function Nav() {
  const link = ({ isActive }: { isActive: boolean }) =>
    `rounded-lg px-3 py-1.5 text-sm font-medium transition ${
      isActive ? 'bg-edge text-white' : 'text-slate-400 hover:text-slate-200'
    }`;
  return (
    <header className="sticky top-0 z-10 border-b border-edge bg-ink/80 backdrop-blur">
      {/* Wraps to a second row below ~440px: the five links don't fit a 375px phone, and an
          unwrapped row made the whole document scroll sideways. */}
      <div className="mx-auto flex max-w-5xl flex-wrap items-center gap-x-2 gap-y-1 px-4 py-3">
        <NavLink to="/" className="mr-2 flex items-center gap-2 font-semibold text-white">
          <span className="grid h-7 w-7 place-items-center rounded-lg bg-accent text-sm">Z</span>
          <span className="hidden sm:inline">ZMG Tracker</span>
        </NavLink>
        <NavLink to="/" end className={link}>
          Home
        </NavLink>
        <NavLink to="/releases" className={link}>
          Releases
        </NavLink>
        <NavLink to="/catalog" className={link}>
          Catalog
        </NavLink>
        <NavLink to="/artists" className={link}>
          Artists
        </NavLink>
        <NavLink to="/templates" className={link}>
          Templates
        </NavLink>
      </div>
    </header>
  );
}

// One client for the app. A 60s staleTime means navigating between pages serves cached data instead
// of refetching (the artist roster, previously fetched 8× per navigation, now loads once); mutations
// invalidate the affected keys explicitly, so edits still show immediately.
const queryClient = new QueryClient({
  defaultOptions: { queries: { staleTime: 60_000 } },
});

export default function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <ConfirmProvider>
        <div className="min-h-screen">
          <Nav />
          <main className="mx-auto max-w-5xl px-4 py-6">
            <Routes>
              <Route path="/" element={<Home />} />
              <Route path="/releases" element={<AllReleases />} />
              <Route path="/releases/archived" element={<ArchivedReleases />} />
              <Route path="/catalog" element={<Catalog />} />
              <Route path="/catalog/new" element={<SongForm />} />
              <Route path="/catalog/archived" element={<ArchivedSongs />} />
              <Route path="/catalog/:id" element={<SongDetail />} />
              <Route path="/artists" element={<Artists />} />
              <Route path="/artists/new" element={<ArtistForm />} />
              <Route path="/artists/:id" element={<ArtistForm />} />
              <Route path="/templates" element={<Templates />} />
              <Route path="/releases/new" element={<ReleaseForm />} />
              <Route path="/releases/:id" element={<ReleaseDetail />} />
              <Route path="/releases/:id/edit" element={<ReleaseForm />} />
            </Routes>
          </main>
        </div>
      </ConfirmProvider>
    </QueryClientProvider>
  );
}
