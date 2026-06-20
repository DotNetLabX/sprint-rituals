import type { InjectionKey, Ref, ComputedRef } from 'vue'
import type { AppSettings, BoardOption, StatusOption } from '../../types'
import { useSettingsStore } from '../../stores/settingsStore'
import { useAuthStore } from '../../stores/authStore'

type SettingsStore = ReturnType<typeof useSettingsStore>
type AuthStore = ReturnType<typeof useAuthStore>

export const settingsFormKey: InjectionKey<AppSettings> = Symbol('settingsForm')
export const boardsKey: InjectionKey<Ref<BoardOption[]>> = Symbol('boards')
export const boardsLoadingKey: InjectionKey<Ref<boolean>> = Symbol('boardsLoading')
export const boardsErrorKey: InjectionKey<Ref<boolean>> = Symbol('boardsError')
export const statusesKey: InjectionKey<Ref<StatusOption[]>> = Symbol('statuses')
export const statusesLoadingKey: InjectionKey<Ref<boolean>> = Symbol('statusesLoading')
export const statusesErrorKey: InjectionKey<Ref<boolean>> = Symbol('statusesError')
export const isReadOnlyKey: InjectionKey<ComputedRef<boolean>> = Symbol('isReadOnly')
export const settingsStoreKey: InjectionKey<SettingsStore> = Symbol('settingsStore')
export const authStoreKey: InjectionKey<AuthStore> = Symbol('authStore')
