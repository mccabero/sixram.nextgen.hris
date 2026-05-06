export function formatFileSize(bytes: number): string {
  if (bytes <= 0) {
    return '0 B'
  }

  const units = ['B', 'KB', 'MB', 'GB']
  let value = bytes
  let unitIndex = 0

  while (value >= 1024 && unitIndex < units.length - 1) {
    value /= 1024
    unitIndex += 1
  }

  const precision = unitIndex === 0 ? 0 : value >= 10 ? 1 : 2
  return `${value.toFixed(precision)} ${units[unitIndex]}`
}

export function openBlobInNewTab(blob: Blob, fileName: string): void {
  const url = URL.createObjectURL(blob)
  const newWindow = window.open(url, '_blank', 'noopener,noreferrer')

  if (!newWindow) {
    downloadBlob(blob, fileName)
  }

  setTimeout(() => URL.revokeObjectURL(url), 60_000)
}

export function downloadBlob(blob: Blob, fileName: string): void {
  const url = URL.createObjectURL(blob)
  const anchor = document.createElement('a')
  anchor.href = url
  anchor.download = fileName
  anchor.rel = 'noopener'
  document.body.appendChild(anchor)
  anchor.click()
  anchor.remove()
  setTimeout(() => URL.revokeObjectURL(url), 5_000)
}
