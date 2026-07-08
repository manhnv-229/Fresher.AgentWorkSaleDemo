export type ButtonVariant =
  | 'brand'
  | 'info'
  | 'warning'
  | 'danger'
  | 'success'
  | 'neutral'
  | 'neutralInverse'
  | 'primary'
  | 'secondary';

export function normalizeButtonVariant(variant: ButtonVariant): string {
  switch (variant) {
    case 'primary':
    case 'brand':
      return 'brand';
    case 'secondary':
    case 'neutralInverse':
      return 'neutral-inverse';
    default:
      return variant;
  }
}
