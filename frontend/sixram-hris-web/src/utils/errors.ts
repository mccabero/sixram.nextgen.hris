import { ApiClientError } from '../api/client'

export function getValidationErrors(error: unknown): Record<string, string[]> {
  if (error instanceof ApiClientError && error.payload.errors) {
    return error.payload.errors
  }

  return {}
}

export function getFieldError(
  errors: Record<string, string[]>,
  ...fieldNames: string[]
): string | null {
  for (const fieldName of fieldNames) {
    const match = Object.entries(errors).find(([key]) =>
      key.localeCompare(fieldName, undefined, { sensitivity: 'accent' }) === 0,
    )

    if (match && match[1].length > 0) {
      return match[1][0]
    }
  }

  return null
}

export function formatError(error: unknown): string {
  if (error instanceof ApiClientError) {
    const validationMessage = error.payload.errors
      ? Object.values(error.payload.errors)
          .flat()
          .find((message) => message.length > 0)
      : undefined

    return validationMessage ?? error.payload.detail ?? error.message
  }

  if (error instanceof Error) {
    return error.message
  }

  return 'Something went wrong.'
}
