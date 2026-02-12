export class ApiError extends Error {
  status: number;
  details?: unknown;

  constructor(message: string, status: number, details?: unknown) {
    super(message);
    this.status = status;
    this.details = details;
  }
}

const mergeHeaders = (headers: HeadersInit | undefined, body?: BodyInit | null) => {
  const merged = new Headers(headers || {});
  if (body && !merged.has("Content-Type")) {
    merged.set("Content-Type", "application/json");
  }
  return merged;
};

export const apiFetch = async <T>(
  baseUrl: string,
  path: string,
  options: RequestInit = {}
): Promise<T> => {
  const response = await fetch(`${baseUrl}${path}`, {
    ...options,
    headers: mergeHeaders(options.headers, options.body)
  });

  if (!response.ok) {
    let details: unknown = null;
    try {
      details = await response.json();
    } catch {
      details = null;
    }

    const message =
      typeof details === "object" && details
        ? String(
            (details as { title?: string; detail?: string }).title ||
              (details as { title?: string; detail?: string }).detail ||
              "Request failed."
          )
        : "Request failed.";
    throw new ApiError(message, response.status, details);
  }

  if (response.status === 204) {
    return null as T;
  }

  return (await response.json()) as T;
};
