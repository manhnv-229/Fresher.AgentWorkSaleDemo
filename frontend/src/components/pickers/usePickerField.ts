import { computed, ref, useAttrs, type Ref } from 'vue';

type PickerFieldProps = {
  id?: string;
  name?: string;
  label?: string;
  labelPosition?: 'hidden' | 'top' | 'none';
  placeholder?: string;
  disabled?: boolean;
  readonly?: boolean;
  error?: string;
  ariaLabel?: string;
  state?: 'normal' | 'hover' | 'focus' | 'disabled' | 'error' | 'readonly';
};

export function usePickerField(props: PickerFieldProps, fallbackId: string, popoverOpen?: Ref<boolean>) {
  const attrs = useAttrs();
  const inputRef = ref<HTMLInputElement | null>(null);
  const isFocused = ref(false);

  // Ưu tiên id do caller truyền vào, fallback theo name rồi id cố định để aria luôn hợp lệ.
  const inputId = computed(() => props.id || (attrs.id as string | undefined) || props.name || fallbackId);
  const isInvalidState = computed(() => Boolean(props.error) || props.state === 'error');
  const isHoverState = computed(() => props.state === 'hover');
  const isReadonlyState = computed(() => props.readonly || props.state === 'readonly');
  const isDisabled = computed(() => props.disabled || props.state === 'disabled');
  const isVisualFocused = computed(() => isFocused.value || props.state === 'focus' || popoverOpen?.value);
  const hasLabel = computed(() => props.labelPosition !== 'none' && Boolean(props.label));
  const showTooltip = computed(() => isFocused.value && Boolean(props.error));
  const describedBy = computed(() => {
    if (isInvalidState.value) {
      return `${inputId.value}-error`;
    }

    return undefined;
  });

  // Chỉ forward attrs hành vi; class/style do component picker tự kiểm soát.
  const inputAttrs = computed(() => {
    const { class: _class, style: _style, ...rest } = attrs;
    return rest;
  });

  // State focus dùng để hiển thị tooltip lỗi và trạng thái viền tương ứng.
  function handleFocus() {
    isFocused.value = true;
  }

  function handleBlurState() {
    isFocused.value = false;
  }

  return {
    inputRef,
    inputId,
    inputAttrs,
    isDisabled,
    isHoverState,
    isInvalidState,
    isReadonlyState,
    isVisualFocused,
    hasLabel,
    showTooltip,
    describedBy,
    handleFocus,
    handleBlurState
  };
}
