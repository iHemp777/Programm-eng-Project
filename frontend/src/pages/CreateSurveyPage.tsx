import { useState } from "react";
import { api } from "../lib/api";
import { useNavigate } from "react-router-dom";

type DraftQuestion = { text: string; options: string[]; isRequired: boolean; order: number };

export default function CreateSurveyPage() {
  const nav = useNavigate();
  const [title, setTitle] = useState("");
  const [description, setDescription] = useState("");
  const [createdBy, setCreatedBy] = useState<number>(1);
  const [endsAt, setEndsAt] = useState<string>("");
  const [enablePredictions, setEnablePredictions] = useState<boolean>(false);
  const [questions, setQuestions] = useState<DraftQuestion[]>([
    { text: "Вопрос 1", options: ["Вариант 1", "Вариант 2"], isRequired: true, order: 1 }
  ]);
  const [err, setErr] = useState<string | null>(null);
  const [ok, setOk] = useState<string | null>(null);

  function updateQuestion(i: number, patch: Partial<DraftQuestion>) {
    setQuestions((prev) => prev.map((q, idx) => (idx === i ? { ...q, ...patch } : q)));
  }

  function addQuestion() {
    setQuestions((prev) => [
      ...prev,
      { text: `Вопрос ${prev.length + 1}`, options: ["Вариант 1", "Вариант 2"], isRequired: true, order: prev.length + 1 }
    ]);
  }

  function removeQuestion(i: number) {
    setQuestions((prev) => prev.filter((_, idx) => idx !== i).map((q, idx) => ({ ...q, order: idx + 1 })));
  }

  async function submit() {
    setErr(null);
    setOk(null);
    const cleanTitle = title.trim();
    if (!cleanTitle) {
      setErr("Название опроса обязательно.");
      return;
    }
    if (questions.length === 0) {
      setErr("Нужно добавить хотя бы один вопрос.");
      return;
    }

    const body = {
      title: cleanTitle,
      description: description.trim() || null,
      accessType: 0,
      isAnonymous: false,
      status: 1,
      startsAt: null,
      endsAt: endsAt ? new Date(endsAt).toISOString() : null,
      createdBy: createdBy,
      enablePredictions,
      questions: questions.map((q) => ({
        text: q.text.trim(),
        order: q.order,
        isRequired: q.isRequired,
        options: q.options
          .map((t, idx) => ({ text: t.trim(), order: idx + 1 }))
          .filter((o) => o.text.length > 0)
      }))
    };

    try {
      const created = await api.createSurvey(body);
      setOk(`Опрос создан: ID ${created.id}`);
      nav(`/surveys/${created.id}`);
    } catch (e: any) {
      setErr(String(e?.message ?? e));
    }
  }

  return (
    <div className="grid">
      <div className="card">
        <div className="cardHeader">
          <div className="h2">Параметры</div>
        </div>
        <div className="cardBody">
          <div className="grid two">
            <div className="field">
              <div className="label">Название</div>
              <input value={title} onChange={(e) => setTitle(e.target.value)} placeholder="Введите название" />
            </div>
            <div className="field">
              <div className="label">ID создателя (CreatedBy)</div>
              <input inputMode="numeric" value={String(createdBy)} onChange={(e) => setCreatedBy(Number(e.target.value) || 0)} />
            </div>
            <div className="field" style={{ gridColumn: "1 / -1" }}>
              <div className="label">Описание</div>
              <textarea value={description} onChange={(e) => setDescription(e.target.value)} placeholder="Опционально" />
            </div>
            <div className="field">
              <div className="label">Дата окончания (EndsAt)</div>
              <input type="datetime-local" value={endsAt} onChange={(e) => setEndsAt(e.target.value)} />
              <div className="muted" style={{ fontSize: 12 }}>
                Для прогнозов опрос должен быть публичным и ограниченным по времени.
              </div>
            </div>
            <div className="field">
              <div className="label">Предсказания</div>
              <select value={enablePredictions ? "1" : "0"} onChange={(e) => setEnablePredictions(e.target.value === "1")}>
                <option value="0">Выключены</option>
                <option value="1">Разрешены</option>
              </select>
              <div className="muted" style={{ fontSize: 12 }}>
                Если включено, пользователь должен выбрать лидера по каждому вопросу.
              </div>
            </div>
          </div>

          {err ? <div className="err" style={{ marginTop: 10 }}>{err}</div> : null}
          {ok ? <div className="ok" style={{ marginTop: 10 }}>{ok}</div> : null}

          <div className="sep" />
          <button className="btn btnPrimary" onClick={submit}>Создать</button>
        </div>
      </div>

      <div className="card">
        <div className="cardHeader">
          <div className="row" style={{ justifyContent: "space-between" }}>
            <div className="h2">Вопросы</div>
            <button className="btn" onClick={addQuestion}>Добавить вопрос</button>
          </div>
        </div>
        <div className="cardBody">
          <div className="grid">
            {questions.map((q, i) => (
              <div key={i} className="card" style={{ background: "rgba(31,32,38,.75)" }}>
                <div className="cardBody">
                  <div className="row" style={{ justifyContent: "space-between", flexWrap: "wrap" }}>
                    <div className="pill red">Вопрос {i + 1}</div>
                    <button className="btn btnDanger" onClick={() => removeQuestion(i)} disabled={questions.length <= 1}>
                      Удалить
                    </button>
                  </div>

                  <div className="sep" />

                  <div className="grid two">
                    <div className="field" style={{ gridColumn: "1 / -1" }}>
                      <div className="label">Текст</div>
                      <input value={q.text} onChange={(e) => updateQuestion(i, { text: e.target.value })} />
                    </div>
                    <div className="field">
                      <div className="label">Обязательный</div>
                      <select value={q.isRequired ? "1" : "0"} onChange={(e) => updateQuestion(i, { isRequired: e.target.value === "1" })}>
                        <option value="1">Да</option>
                        <option value="0">Нет</option>
                      </select>
                    </div>
                    <div className="field">
                      <div className="label">Варианты (по одному в строке)</div>
                      <textarea
                        value={q.options.join("\n")}
                        onChange={(e) => updateQuestion(i, { options: e.target.value.split("\n") })}
                      />
                    </div>
                  </div>
                </div>
              </div>
            ))}
          </div>
        </div>
      </div>
    </div>
  );
}

