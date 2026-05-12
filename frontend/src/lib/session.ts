const KEY = "survey-ui.userId";

export function getUserId(): number {
  const raw = localStorage.getItem(KEY);
  const n = raw ? Number(raw) : NaN;
  return Number.isFinite(n) && n > 0 ? n : 1;
}

export function setUserId(id: number) {
  localStorage.setItem(KEY, String(id));
}

