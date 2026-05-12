import { useEffect, useState } from "react";
import { api, UserProfile } from "../lib/api";

export default function ProfilePage({ userId }: { userId: number }) {
  const [profile, setProfile] = useState<UserProfile | null>(null);
  const [displayName, setDisplayName] = useState("");
  const [avatarUrl, setAvatarUrl] = useState("");
  const [err, setErr] = useState<string | null>(null);
  const [ok, setOk] = useState<string | null>(null);

  useEffect(() => {
    setErr(null);
    setOk(null);
    api
      .getUser(userId)
      .then((p) => {
        setProfile(p);
        setDisplayName(p.displayName ?? "");
        setAvatarUrl(p.avatarUrl ?? "");
      })
      .catch((e: any) => setErr(String(e?.message ?? e)));
  }, [userId]);

  async function save() {
    setErr(null);
    setOk(null);
    const name = displayName.trim();
    if (!name) {
      setErr("Имя обязательно.");
      return;
    }
    try {
      await api.upsertUser(userId, { userId, displayName: name, avatarUrl: avatarUrl.trim() || null });
      const p = await api.getUser(userId);
      setProfile(p);
      setOk("Профиль сохранён.");
    } catch (e: any) {
      setErr(String(e?.message ?? e));
    }
  }

  return (
    <div className="grid two">
      <div className="card">
        <div className="cardHeader">
          <div className="h2">Данные профиля</div>
        </div>
        <div className="cardBody">
          <div className="row" style={{ gap: 12, alignItems: "center" }}>
            <div className="logoBox" style={{ width: 54, height: 54, borderRadius: 14 }} aria-label="Avatar placeholder">
              {profile?.avatarUrl ? (
                <img
                  src={profile.avatarUrl}
                  alt="Avatar"
                  style={{ width: "100%", height: "100%", objectFit: "cover", borderRadius: 14 }}
                />
              ) : null}
            </div>
            <div>
              <div className="h1">{profile ? profile.displayName : `User ${userId}`}</div>
              <div className="muted" style={{ marginTop: 4 }}>ID {userId}</div>
              <div className="row" style={{ marginTop: 8, flexWrap: "wrap" }}>
                <span className="pill red">Счёт: {profile?.points ?? 0}</span>
              </div>
            </div>
          </div>

          <div className="sep" />

          <div className="grid">
            <div className="field">
              <div className="label">Имя</div>
              <input value={displayName} onChange={(e) => setDisplayName(e.target.value)} />
            </div>
            <div className="field">
              <div className="label">URL аватарки</div>
              <input value={avatarUrl} onChange={(e) => setAvatarUrl(e.target.value)} placeholder="https://..." />
              <div className="muted" style={{ fontSize: 12 }}>
                Можно оставить пустым.
              </div>
            </div>
            {err ? <div className="err">{err}</div> : null}
            {ok ? <div className="ok">{ok}</div> : null}
            <button className="btn btnPrimary" onClick={save}>Сохранить</button>
          </div>
        </div>
      </div>

      <div className="card">
        <div className="cardHeader">
          <div className="h2">Опубликованные опросы</div>
        </div>
        <div className="cardBody">
          {!profile ? (
            <div className="muted">Загрузка...</div>
          ) : profile.publishedSurveys.length === 0 ? (
            <div className="muted">Пока нет опубликованных опросов.</div>
          ) : (
            <div className="grid">
              {profile.publishedSurveys.map((s) => (
                <a key={s.id} className="card" href={`/surveys/${s.id}`} style={{ background: "rgba(31,32,38,.75)" }}>
                  <div className="cardBody">
                    <div className="row" style={{ justifyContent: "space-between", flexWrap: "wrap" }}>
                      <div style={{ minWidth: 0 }}>
                        <div className="h2" style={{ whiteSpace: "nowrap", overflow: "hidden", textOverflow: "ellipsis" }}>
                          {s.title}
                        </div>
                        <div className="muted" style={{ marginTop: 6, fontSize: 13 }}>
                          Создан: {new Date(s.createdAt).toLocaleString()}
                        </div>
                      </div>
                      <div className="row" style={{ flexWrap: "wrap" }}>
                        {s.enablePredictions ? <span className="pill red">Предсказания</span> : <span className="pill">Без предсказаний</span>}
                        <span className="pill">ID {s.id}</span>
                      </div>
                    </div>
                  </div>
                </a>
              ))}
            </div>
          )}
        </div>
      </div>
    </div>
  );
}

