import alertify from 'alertifyjs'

// eslint-disable-next-line @typescript-eslint/no-explicit-any
;(alertify as any).defaults = {
  ...(alertify as any).defaults,
  notifier: {
    delay: 5,
    position: 'bottom-right',
    closeButton: true,
  },
}

/** Parses ASP.NET Core ValidationProblemDetails or plain-text error responses. */
export async function parseApiError(res: Response, fallback = 'Something went wrong.'): Promise<string> {
  const text = await res.text()
  try {
    const json = JSON.parse(text) as Record<string, unknown>
    if (json.errors && typeof json.errors === 'object') {
      const messages = Object.values(json.errors as Record<string, string[]>).flat()
      if (messages.length > 0) return messages.join('\n')
    }
    if (typeof json.detail === 'string' && json.detail) return json.detail
    if (typeof json.title === 'string' && json.title) return json.title
  } catch { /* not JSON */ }
  return text || fallback
}

/** Shows one or more error notifications via alertify. */
export function notifyError(message: string): void {
  const lines = message.split('\n').filter(Boolean)
  lines.forEach(line => alertify.error(line))
}

/** Shows a success notification via alertify. */
export function notifySuccess(message: string): void {
  alertify.success(message)
}
