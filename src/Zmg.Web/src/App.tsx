import { NavLink, Route, Routes } from 'react-router-dom';
import Home from './features/home/HomePage';
import AllReleases from './pages/AllReleases';
import Artists from './features/artists/ArtistsPage';
import ReleaseForm from './pages/ReleaseForm';
import ReleaseDetail from './pages/ReleaseDetail';
import Templates from './features/templates/TemplatesPage';

function Nav() {
  const link = ({ isActive }: { isActive: boolean }) =>
    `rounded-lg px-3 py-1.5 text-sm font-medium transition ${
      isActive ? 'bg-edge text-white' : 'text-slate-400 hover:text-slate-200'
    }`;
  return (
    <header className="sticky top-0 z-10 border-b border-edge bg-ink/80 backdrop-blur">
      <div className="mx-auto flex max-w-5xl items-center gap-2 px-4 py-3">
        <NavLink to="/" className="mr-2 flex items-center gap-2 font-semibold text-white">
          <span className="grid h-7 w-7 place-items-center rounded-lg bg-accent text-sm">Z</span>
          <span className="hidden sm:inline">ZMG Tracker</span>
        </NavLink>
        <NavLink to="/" end className={link}>
          Home
        </NavLink>
        <NavLink to="/releases" className={link}>
          All Releases
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

export default function App() {
  return (
    <div className="min-h-screen">
      <Nav />
      <main className="mx-auto max-w-5xl px-4 py-6">
        <Routes>
          <Route path="/" element={<Home />} />
          <Route path="/releases" element={<AllReleases />} />
          <Route path="/artists" element={<Artists />} />
          <Route path="/templates" element={<Templates />} />
          <Route path="/releases/new" element={<ReleaseForm />} />
          <Route path="/releases/:id" element={<ReleaseDetail />} />
          <Route path="/releases/:id/edit" element={<ReleaseForm />} />
        </Routes>
      </main>
    </div>
  );
}
