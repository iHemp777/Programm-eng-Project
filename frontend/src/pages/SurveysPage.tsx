import { useEffect, useMemo, useState } from "react";
import { api, SurveySummary } from "../lib/api";
import { Link } from "react-router-dom";

export default function SurveysPage() {
  const [items, setItems] = useState<SurveySummary[] | null>(null);
  const [err, setErr] = useState<string | null>(null);
  const [q, setQ] = useState("");

  useEffect(() => {
    api
      .getPublicSurveys()
      .then((x) => {
        setItems(x);
        setErr(null);
      })
      .catch((e: any) => setErr(String(e?.message ?? e)));
  }, []);

  const filtered = useMemo(() => {
    if (!items) return [];
    const s = q.trim().toLowerCase();
    if (!s) return items;
    return items.filter((it) => it.title.toLowerCase().includes(s) || (it.description ?? "").toLowerCase().includes(s));
  }, [items, q]);

  return (
    <div className="grid">
      <div className="card">
        <div className="cardBody">
          <div className="row" style={{ justifyContent: "space-between", flexWrap: "wrap" }}>
            <div className="field" style={{ minWidth: 260, flex: 1 }}>
              <div className="label">Поиск по названию или описанию</div>
              <input value={q} onChange={(e) => setQ(e.target.value)} placeholder="Введите текст" />
            </div>
            <div className="pill">Публичные опросы</div>
          </div>
          {err ? <div className="err" style={{ marginTop: 10 }}>{err}</div> : null}
        </div>
      </div>

      <div className="card">
        <div className="cardHeader">
          <div className="h2">Список</div>
        </div>
        <div className="cardBody">
          {!items ? (
            <div className="muted">Загрузка...</div>
          ) : filtered.length === 0 ? (
            <div className="muted">Ничего не найдено.</div>
          ) : (
            <div className="grid">
              {filtered.map((s) => (
                <div key={s.id} className="card" style={{ background: "rgba(31,32,38,.75)" }}>
                  <div className="cardBody">
                    <div className="row" style={{ justifyContent: "space-between" }}>
                      <div style={{ minWidth: 0 }}>
                        <div className="h2" style={{ marginBottom: 6, whiteSpace: "nowrap", overflow: "hidden", textOverflow: "ellipsis" }}>
                          {s.title}
                        </div>
                        {s.description ? <div className="muted" style={{ fontSize: 13 }}>{s.description}</div> : null}
                      </div>
                      <Link className="btn btnPrimary" to={`/surveys/${s.id}`}>Открыть</Link>
                    </div>
                    <div className="sep" />
                    <div className="row" style={{ flexWrap: "wrap" }}>
                      <span className="pill">ID {s.id}</span>
                      {s.endsAt ? <span className="pill red">Ограничен по времени</span> : <span className="pill">Без дедлайна</span>}
                      <span className="pill">Статус {s.status}</span>
                    </div>
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>
      </div>
    </div>
  );
}

