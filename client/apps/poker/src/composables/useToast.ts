import { ref } from 'vue'

export type ToastVariant = 'success' | 'error' | 'info'

export interface Toast {
  id: number
  variant: ToastVariant
  message: string
  duration: number
}

let nextId = 0
const toasts = ref<Toast[]>([])

export function useToast() {
  function addToast(message: string, variant: ToastVariant = 'info', duration = 4000) {
    const id = ++nextId
    toasts.value.push({ id, variant, message, duration })
    setTimeout(() => removeToast(id), duration)
  }

  function removeToast(id: number) {
    toasts.value = toasts.value.filter((t) => t.id !== id)
  }

  return { toasts, addToast, removeToast }
}
