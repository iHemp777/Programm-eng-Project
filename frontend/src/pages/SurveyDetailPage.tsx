import { useEffect, useMemo, useState } from "react";
import { api, Prediction, Survey, VoteResults } from "../lib/api";
import { Link, useParams } from "react-router-dom";

function isoToLocal(iso?: string | null) {
  if (!iso) return "";
  const d = new Date(iso);
  return d.toLocaleString();
}

export default function SurveyDetailPage({ userId }: { userId: number }) {
  const params = useParams();
  const id = Number(params.id);
  const [survey, setSurvey] = useState<Survey | null>(null);
  const [results, setResults] = useState<VoteResults | null>(null);
  const [prediction, setPrediction] = useState<Prediction | null>(null);
  const [err, setErr] = useState<string | null>(null);
  const [ok, setOk] = useState<string | null>(null);

  const now = Date.now();
  const endsAtMs = survey?.endsAt ? new Date(survey.endsAt).getTime() : null;
  const finished = endsAtMs != null ? now >= endsAtMs : false;

  const [voteAnswers, setVoteAnswers] = useState<Record<number, number>>({});
  const [predAnswers, setPredAnswers] = useState<Record<number, number>>({});

  useEffect(() => {
    setErr(null);
    setOk(null);
    setSurvey(null);
    setResults(null);
    setPrediction(null);

    api
      .getSurvey(id)
      .then((s) => {
        setSurvey(s);
        const initVote: Record<number, number> = {};
        const initPred: Record<number, number> = {};
        for (const q of s.questions) {
          if (q.options.length > 0) {
            initVote[q.id] = q.options[0].id;
            initPred[q.id] = q.options[0].id;
          }
        }
        setVoteAnswers(initVote);
        setPredAnswers(initPred);
      })
      .catch((e: any) => setErr(String(e?.message ?? e)));

    api
      .getPrediction(id, userId)
      .then((p) => setPrediction(p))
      .catch(() => undefined);
  }, [id, userId]);

  const canPredict = useMemo(() => {
    if (!survey) return false;
    if (!survey.enablePredictions) return false;
    if (!survey.endsAt) return false;
    if (finished) return false;
    if (survey.status !== 1) return false;
    return true;
  }, [survey, finished]);

  async function loadResults() {
    setErr(null);
    setOk(null);
    try {
      const r = await api.getVoteResults(id);
      setResults(r);
    } catch (e: any) {
      setErr(String(e?.message ?? e));
    }
  }

  async function submitVote() {
    if (!survey) return;
    setErr(null);
    setOk(null);
    const answers = survey.questions.map((q) => ({ questionId: q.id, optionId: voteAnswers[q.id] }));
    try {
      await api.submitVote({ surveyId: id, voterId: userId, answers });
      setOk("Голос отправлен.");
    } catch (e: any) {
      setErr(String(e?.message ?? e));
    }
  }

  async function createPrediction() {
    if (!survey) return;
    setErr(null);
    setOk(null);
    const answers = survey.questions.map((q) => ({ questionId: q.id, optionId: predAnswers[q.id] }));
    try {
      const p = await api.createPrediction(id, { userId, answers });
      setPrediction(p);
      setOk("Прогноз сохранён. Изменение не предусмотрено.");
    } catch (e: any) {
      setErr(String(e?.message ?? e));
    }
  }

  async function scorePredictions() {
    setErr(null);
    setOk(null);
    try {
      const r = await api.scorePredictions(id);
      setOk(`Начисление выполнено. Обработано прогнозов: ${r.scoredPredictions}`);
    } catch (e: any) {
      setErr(String(e?.message ?? e));
    }
  }

  if (!Number.isFinite(id) || id <= 0) {
    return (
      <div className="card">
        <div className="cardBody">
          <div className="err">Некорректный ID.</div>
          <div style={{ marginTop: 10 }}>
            <Link className="btn" to="/">Назад</Link>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="grid">
      <div className="card">
        <div className="cardBody">
          <div className="row" style={{ justifyContent: "space-between", flexWrap: "wrap" }}>
            <div style={{ minWidth: 0 }}>
              <div className="h1">{survey ? survey.title : "Загрузка..."}</div>
              {survey?.description ? <div className="muted" style={{ marginTop: 6 }}>{survey.description}</div> : null}
              {survey ? (
                <div className="row" style={{ marginTop: 10, flexWrap: "wrap" }}>
                  <span className="pill">ID {survey.id}</span>
                  {survey.endsAt ? <span className="pill red">Окончание: {isoToLocal(survey.endsAt)}</span> : <span className="pill">Без дедлайна</span>}
                  {survey.enablePredictions ? <span className="pill red">Предсказания включены</span> : <span className="pill">Предсказания выключены</span>}
                </div>
              ) : null}
            </div>
            <div className="row">
              <button className="btn" onClick={loadResults}>Результаты</button>
              {finished ? <button className="btn btnPrimary" onClick={scorePredictions}>Начислить баллы</button> : null}
            </div>
          </div>
          {err ? <div className="err" style={{ marginTop: 10 }}>{err}</div> : null}
          {ok ? <div className="ok" style={{ marginTop: 10 }}>{ok}</div> : null}
        </div>
      </div>

      {survey ? (
        <div className="grid two">
          <div className="card">
            <div className="cardHeader">
              <div className="h2">Голосование</div>
            </div>
            <div className="cardBody">
              <div className="muted" style={{ fontSize: 13, marginBottom: 10 }}>
                ID пользователя для голосования: {userId}
              </div>
              <div className="grid">
                {survey.questions
                  .slice()
                  .sort((a, b) => a.order - b.order)
                  .map((q) => (
                    <div key={q.id} className="card" style={{ background: "rgba(31,32,38,.75)" }}>
                      <div className="cardBody">
                        <div className="h2" style={{ marginBottom: 8 }}>
                          {q.order}. {q.text}
                        </div>
                        <div className="field">
                          <div className="label">Выберите вариант</div>
                          <select
                            value={voteAnswers[q.id] ?? ""}
                            onChange={(e) => setVoteAnswers((prev) => ({ ...prev, [q.id]: Number(e.target.value) }))}
                          >
                            {q.options
                              .slice()
                              .sort((a, b) => a.order - b.order)
                              .map((o) => (
                                <option key={o.id} value={o.id}>
                                  {o.text}
                                </option>
                              ))}
                          </select>
                        </div>
                      </div>
                    </div>
                  ))}
              </div>
              <div className="sep" />
              <button className="btn btnPrimary" onClick={submitVote}>Отправить голос</button>
            </div>
          </div>

          <div className="card">
            <div className="cardHeader">
              <div className="h2">Прогноз лидеров</div>
            </div>
            <div className="cardBody">
              {prediction ? (
                <div className="grid">
                  <div className="pill red">Прогноз создан</div>
                  <div className="muted" style={{ fontSize: 13 }}>
                    Создан: {isoToLocal(prediction.createdAt)}. {prediction.isScored ? `Счёт: ${prediction.score}` : "Ещё не начислен."}
                  </div>
                  <div className="sep" />
                  {survey.questions
                    .slice()
                    .sort((a, b) => a.order - b.order)
                    .map((q) => {
                      const chosen = prediction.answers.find((x) => x.questionId === q.id)?.optionId;
                      const chosenText = q.options.find((o) => o.id === chosen)?.text ?? "Не указано";
                      return (
                        <div key={q.id} className="card" style={{ background: "rgba(31,32,38,.75)" }}>
                          <div className="cardBody">
                            <div className="h2">{q.order}. {q.text}</div>
                            <div className="muted" style={{ marginTop: 6, fontSize: 13 }}>
                              Прогнозируемый лидер: {chosenText}
                            </div>
                          </div>
                        </div>
                      );
                    })}
                </div>
              ) : !canPredict ? (
                <div className="muted">
                  Прогноз недоступен. Проверьте, что опрос публичный, ограничен по времени, опубликован и предсказания включены.
                </div>
              ) : (
                <div className="grid">
                  <div className="muted" style={{ fontSize: 13 }}>
                    Нужно выбрать лидера для каждого вопроса. После сохранения изменить прогноз нельзя.
                  </div>
                  {survey.questions
                    .slice()
                    .sort((a, b) => a.order - b.order)
                    .map((q) => (
                      <div key={q.id} className="card" style={{ background: "rgba(31,32,38,.75)" }}>
                        <div className="cardBody">
                          <div className="h2" style={{ marginBottom: 8 }}>
                            {q.order}. {q.text}
                          </div>
                          <div className="field">
                            <div className="label">Лидер</div>
                            <select
                              value={predAnswers[q.id] ?? ""}
                              onChange={(e) => setPredAnswers((prev) => ({ ...prev, [q.id]: Number(e.target.value) }))}
                            >
                              {q.options
                                .slice()
                                .sort((a, b) => a.order - b.order)
                                .map((o) => (
                                  <option key={o.id} value={o.id}>
                                    {o.text}
                                  </option>
                                ))}
                            </select>
                          </div>
                        </div>
                      </div>
                    ))}
                  <button className="btn btnPrimary" onClick={createPrediction}>Сохранить прогноз</button>
                </div>
              )}
            </div>
          </div>
        </div>
      ) : null}

      {results ? (
        <div className="card">
          <div className="cardHeader">
            <div className="h2">Результаты голосования</div>
          </div>
          <div className="cardBody">
            <div className="row" style={{ flexWrap: "wrap" }}>
              <span className="pill">Всего голосов: {results.totalVotes}</span>
            </div>
            <div className="sep" />
            <div className="grid">
              {results.questions.map((q) => (
                <div key={q.questionId} className="card" style={{ background: "rgba(31,32,38,.75)" }}>
                  <div className="cardBody">
                    <div className="h2">Вопрос ID {q.questionId}</div>
                    <div className="sep" />
                    <div className="grid">
                      {q.options.map((o) => (
                        <div key={o.optionId} className="row" style={{ justifyContent: "space-between" }}>
                          <div className="muted">Вариант ID {o.optionId}</div>
                          <div className="pill red">{o.votesCount}</div>
                        </div>
                      ))}
                    </div>
                  </div>
                </div>
              ))}
            </div>
          </div>
        </div>
      ) : null}
    </div>
  );
}

