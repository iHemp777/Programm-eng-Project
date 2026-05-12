import { NavLink, Route, Routes, useLocation } from "react-router-dom";
import SurveysPage from "./pages/SurveysPage";
import CreateSurveyPage from "./pages/CreateSurveyPage";
import SurveyDetailPage from "./pages/SurveyDetailPage";
import ProfilePage from "./pages/ProfilePage";
import { getUserId, setUserId } from "./lib/session";
import { useMemo, useState } from "react";

export default function App() {
  const location = useLocation();
  const [userId, setUserIdState] = useState<number>(() => getUserId());
  const headerTitle = useMemo(() => {
    if (location.pathname.startsWith("/surveys/new")) return "Создание опроса";
    if (location.pathname.startsWith("/surveys/")) return "Опрос";
    if (location.pathname.startsWith("/profile")) return "Профиль";
    return "Опросы";
  }, [location.pathname]);

  return (
    <div className="app">
      <aside className="sidebar">
        <div className="brand">
          <div className="logoBox" aria-label="Logo placeholder" />
          <div>
            <div className="brandTitle">Survey App</div>
            <div className="brandSub">Опросы, голосование, прогнозы</div>
          </div>
        </div>

        <div className="card" style={{ padding: 12 }}>
          <div className="field">
            <div className="label">Ваш ID пользователя</div>
            <input
              inputMode="numeric"
              value={String(userId)}
              onChange={(e) => {
                const n = Number(e.target.value);
                if (!Number.isFinite(n)) return;
                setUserIdState(n);
                setUserId(n);
              }}
            />
            <div className="muted" style={{ fontSize: 12, lineHeight: 1.35 }}>
              Используется как идентификатор для голосования, прогнозов и профиля.
            </div>
          </div>
        </div>

        <nav className="nav" aria-label="Навигация">
          <NavLink to="/" end className={({ isActive }) => (isActive ? "active" : "")}>
            Опросы
          </NavLink>
          <NavLink to="/surveys/new" className={({ isActive }) => (isActive ? "active" : "")}>
            Создать опрос
          </NavLink>
          <NavLink to="/profile" className={({ isActive }) => (isActive ? "active" : "")}>
            Профиль
          </NavLink>
        </nav>
      </aside>

      <main className="main">
        <div className="topbar">
          <div className="h1">{headerTitle}</div>
          <div className="pill red">API Gateway: {String((import.meta as any).env?.VITE_API_BASE_URL ?? "http://localhost:5000")}</div>
        </div>

        <Routes>
          <Route path="/" element={<SurveysPage />} />
          <Route path="/surveys/new" element={<CreateSurveyPage />} />
          <Route path="/surveys/:id" element={<SurveyDetailPage userId={userId} />} />
          <Route path="/profile" element={<ProfilePage userId={userId} />} />
        </Routes>
      </main>
    </div>
  );
}

