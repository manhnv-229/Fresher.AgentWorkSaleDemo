<script setup lang="ts">
import { computed, ref, useAttrs } from 'vue';
import { IconCircleCheck, IconLoader2, IconX } from '@tabler/icons-vue';

defineOptions({
  inheritAttrs: false
});

const props = withDefaults(
  defineProps<{
    id?: string;
    name?: string;
    type?: string;
    placeholder?: string;
    autocomplete?: string;
    disabled?: boolean;
    label?: string;
    labelPosition?: 'hidden' | 'top' | 'none';
    required?: boolean;
    hasAction?: boolean;
    clearable?: boolean;
    error?: string;
    hint?: string;
    ariaLabel?: string;
    selectAllOnFocus?: boolean;
    state?: 'normal' | 'hover' | 'focus' | 'readonly' | 'error' | 'validate' | 'verifying';
    validateMessage?: string;
    verifyingMessage?: string;
  }>(),
  {
    type: 'text',
    disabled: false,
    labelPosition: 'top',
    required: false,
    hasAction: false,
    clearable: false,
    error: '',
    hint: '',
    ariaLabel: '',
    selectAllOnFocus: true,
    state: 'normal',
    validateMessage: 'Dữ liệu hợp lệ.',
    verifyingMessage: 'Đang kiểm tra...'
  }
);

const emit = defineEmits<{
  clear: [];
  input: [event: Event];
  focus: [event: FocusEvent];
  blur: [event: FocusEvent];
}>();

const model = defineModel<string>({ required: true });
const attrs = useAttrs();
const inputId = computed(() => props.id || (attrs.id as string | undefined) || props.name || 'field');
const isFocused = ref(false);
const showTooltip = computed(() => isFocused.value && Boolean(props.error));
const hasLabel = computed(() => props.labelPosition !== 'none' && Boolean(props.label));
const isReadonlyState = computed(() => props.state === 'readonly');
const isFocusState = computed(() => props.state === 'focus');
const isHoverState = computed(() => props.state === 'hover');
const isValidateState = computed(() => props.state === 'validate');
const isVerifyingState = computed(() => props.state === 'verifying');
const isInvalidState = computed(() => Boolean(props.error) || props.state === 'error');
const isVisualFocused = computed(() => isFocused.value || isFocusState.value);
const wrapperClass = computed(() => attrs.class);

const inputAttrs = computed(() => {
  const { class: _class, style: _style, ...rest } = attrs;
  return rest;
});

function handleFocus(event: FocusEvent) {
  isFocused.value = true;

  const target = event.target as HTMLInputElement | null;
  if (props.selectAllOnFocus && target && typeof target.select === 'function' && !target.readOnly && !target.disabled) {
    target.select();
  }

  emit('focus', event);
}

function handleBlur(event: FocusEvent) {
  isFocused.value = false;
  emit('blur', event);
}

function handleInput(event: Event) {
  emit('input', event);
}

function clearValue() {
  if (model.value === '') {
    return;
  }

  model.value = '';
  emit('clear');
}
</script>

<template>
  <label
    v-if="hasLabel"
    class="field"
    :class="[
      wrapperClass,
      `field--label-${labelPosition}`,
      {
        'field--with-action': hasAction,
        'field--clearable': clearable,
        'field--invalid': isInvalidState,
        'field--hover': isHoverState,
        'field--focus': isVisualFocused,
        'field--readonly': isReadonlyState,
        'field--validate': isValidateState,
        'field--verifying': isVerifyingState
      }
    ]"
  >
    <span v-if="labelPosition === 'hidden'" class="sr-only">{{ label }}</span>
    <span v-else class="field__label">
      {{ label }}
      <span v-if="required" class="field__required" aria-hidden="true">*</span>
    </span>

    <div class="field__body">
      <div class="field__control">
        <input
          :id="inputId"
          v-model="model"
          :type="type"
          :name="name"
          :autocomplete="autocomplete"
          :placeholder="placeholder"
          :readonly="isReadonlyState"
          :disabled="disabled"
          :aria-label="!label || labelPosition === 'none' ? (ariaLabel || label || placeholder || undefined) : undefined"
          :aria-invalid="isInvalidState ? 'true' : 'false'"
          :aria-describedby="
            isInvalidState
              ? `${inputId}-error`
              : isValidateState
                ? `${inputId}-hint`
                : isVerifyingState
                  ? `${inputId}-hint`
                  : hint
                    ? `${inputId}-hint`
                    : undefined
          "
          v-bind="inputAttrs"
          @focus="handleFocus"
          @blur="handleBlur"
          @input="handleInput"
        />
        <slot name="action" />
        <button
          v-if="clearable && model && model.trim() && !isValidateState && !isVerifyingState"
          type="button"
          class="field__action field__action--clear"
          :aria-label="`Xóa ${label || inputId || name || 'nội dung tìm kiếm'}`"
          title="Xóa nội dung"
          :disabled="disabled || isReadonlyState"
          @mousedown.prevent
          @click="clearValue"
        >
          <IconX :size="20" stroke-width="1.5" aria-hidden="true" />
        </button>
        <div v-if="isVerifyingState" class="field__status-icon" aria-hidden="true">
          <IconLoader2 :size="20" stroke="1.5" class="spin" />
        </div>
        <div v-else-if="isValidateState" class="field__status-icon field__status-icon--success" aria-hidden="true">
          <IconCircleCheck :size="20" stroke-width="1.5" />
        </div>
        <div v-if="showTooltip" class="field__tooltip" role="tooltip">
          {{ error }}
        </div>
      </div>

      <span
        v-if="isInvalidState && !showTooltip"
        :id="`${inputId}-error`"
        class="field__feedback field__feedback--error"
        role="alert"
      >
        {{ error }}
      </span>
      <span v-else-if="isValidateState" :id="`${inputId}-hint`" class="field__feedback field__feedback--success">
        {{ validateMessage }}
      </span>
      <span v-else-if="isVerifyingState" :id="`${inputId}-hint`" class="field__feedback">
        {{ verifyingMessage }}
      </span>
      <span v-else-if="hint" :id="`${inputId}-hint`" class="field__feedback">
        {{ hint }}
      </span>
    </div>
  </label>

  <div
    v-else
    class="field field--label-none"
    :class="[
      wrapperClass,
      {
        'field--with-action': hasAction,
        'field--clearable': clearable,
        'field--invalid': isInvalidState,
        'field--hover': isHoverState,
        'field--focus': isVisualFocused,
        'field--readonly': isReadonlyState,
        'field--validate': isValidateState,
        'field--verifying': isVerifyingState
      }
    ]"
  >
    <div class="field__body">
      <div class="field__control">
        <input
          :id="inputId"
          v-model="model"
          :type="type"
          :name="name"
          :autocomplete="autocomplete"
          :placeholder="placeholder"
          :readonly="isReadonlyState"
          :disabled="disabled"
          :aria-label="ariaLabel || label || placeholder || undefined"
          :aria-invalid="isInvalidState ? 'true' : 'false'"
          :aria-describedby="
            isInvalidState
              ? `${inputId}-error`
              : isValidateState
                ? `${inputId}-hint`
                : isVerifyingState
                  ? `${inputId}-hint`
                  : hint
                    ? `${inputId}-hint`
                    : undefined
          "
          v-bind="inputAttrs"
          @focus="handleFocus"
          @blur="handleBlur"
          @input="handleInput"
        />
        <slot name="action" />
        <button
          v-if="clearable && model && model.trim() && !isValidateState && !isVerifyingState"
          type="button"
          class="field__action field__action--clear"
          :aria-label="`Xóa ${ariaLabel || label || inputId || name || 'nội dung tìm kiếm'}`"
          title="Xóa nội dung"
          :disabled="disabled || isReadonlyState"
          @mousedown.prevent
          @click="clearValue"
        >
          <IconX :size="20" stroke-width="1.5" aria-hidden="true" />
        </button>
        <div v-if="isVerifyingState" class="field__status-icon" aria-hidden="true">
          <IconLoader2 :size="20" stroke="1.5" class="spin" />
        </div>
        <div v-else-if="isValidateState" class="field__status-icon field__status-icon--success" aria-hidden="true">
          <IconCircleCheck :size="20" stroke-width="1.5" />
        </div>
        <div v-if="showTooltip" class="field__tooltip" role="tooltip">
          {{ error }}
        </div>
      </div>

      <span
        v-if="isInvalidState && !showTooltip"
        :id="`${inputId}-error`"
        class="field__feedback field__feedback--error"
        role="alert"
      >
        {{ error }}
      </span>
      <span v-else-if="isValidateState" :id="`${inputId}-hint`" class="field__feedback field__feedback--success">
        {{ validateMessage }}
      </span>
      <span v-else-if="isVerifyingState" :id="`${inputId}-hint`" class="field__feedback">
        {{ verifyingMessage }}
      </span>
      <span v-else-if="hint" :id="`${inputId}-hint`" class="field__feedback">
        {{ hint }}
      </span>
    </div>
  </div>
</template>
