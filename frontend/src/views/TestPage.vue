<script setup lang="ts">
import { computed, ref } from 'vue';
import {
  IconChevronDown,
  IconDots,
  IconDotsVertical,
  IconDownload,
  IconEye,
  IconFilter,
  IconPencil,
  IconPlus,
  IconRefresh,
  IconSettings,
  IconTrash
} from '@tabler/icons-vue';
import BaseButton from '../components/buttons/BaseButton.vue';
import IconButton from '../components/buttons/IconButton.vue';
import SplitButton from '../components/buttons/SplitButton.vue';
import DataTable, {
  type DataTableColumn,
  type DataTableDropdownFilter,
  type DataTableRow,
  type DataTableToolbarAction
} from '../components/tables/DataTable.vue';
import Checkbox from '../components/choices/Checkbox.vue';
import RadioButton from '../components/choices/RadioButton.vue';
import type { ComboboxOption } from '../components/combobox/Combobox.vue';
import ComboboxSingle from '../components/combobox/ComboboxSingle.vue';
import ComboboxMultiple from '../components/combobox/ComboboxMultiple.vue';
import ComboboxSingleSplit from '../components/combobox/ComboboxSingleSplit.vue';
import ComboboxMultipleSplit from '../components/combobox/ComboboxMultipleSplit.vue';
import DropdownList, { type DropdownOption } from '../components/dropdown/DropdownList.vue';
import ContextMenu, { type ContextMenuItem } from '../components/menus/ContextMenu.vue';
import DatePicker from '../components/pickers/DatePicker.vue';
import DateRangePicker from '../components/pickers/DateRangePicker.vue';
import DateRangePickerAdvanced from '../components/pickers/DateRangePickerAdvanced.vue';
import DateTimePicker from '../components/pickers/DateTimePicker.vue';
import Dialog from '../components/dialog/Dialog.vue';
import PopupInlineOneColumn from '../components/popup/PopupInlineOneColumn.vue';
import PopupInlineTwoColumns from '../components/popup/PopupInlineTwoColumns.vue';
import PopupTopOneColumn from '../components/popup/PopupTopOneColumn.vue';
import PopupTopTwoColumns from '../components/popup/PopupTopTwoColumns.vue';
import MonthYearPicker from '../components/pickers/MonthYearPicker.vue';
import InlineNotification from '../components/notifications/InlineNotification.vue';
import TextBoxTopLabel from '../components/forms/TextBoxTopLabel.vue';
import TextBoxInlineLabel from '../components/forms/TextBoxInlineLabel.vue';
import TimePicker from '../components/pickers/TimePicker.vue';
import TimePickerWithSeconds from '../components/pickers/TimePickerWithSeconds.vue';
import { useToastStore, type ToastTone } from '../stores/useToastStore';
import { useNotificationStore, type NotificationTone } from '../stores/useNotificationStore';
import type { DateRangeModel, DateRangePreset } from '../utils/datePicker';
import {
  addDays,
  addMonths,
  endOfMonth,
  formatDate,
  startOfMonth,
  today
} from '../utils/datePicker';

const toastStore = useToastStore();
const notificationStore = useNotificationStore();
const isDialogOpen = ref(false);
const isPopupInlineOpen = ref(false);
const isPopupInlineTwoColumnsOpen = ref(false);
const isPopupTopOneColumnOpen = ref(false);
const isPopupTopTwoColumnsOpen = ref(false);
const textFieldNormalValue = ref('Nguyễn Văn A');
const textFieldHoverValue = ref('Hover state');
const textFieldFocusValue = ref('Focus state');
const textFieldReadonlyValue = ref('Readonly value');
const textFieldErrorValue = ref('');
const textFieldValidateValue = ref('misa-agent');
const textFieldVerifyingValue = ref('pending-value');
const dropdownSupplierValue = ref('');
const dropdownAccountValue = ref('vietcombank');
const dropdownIndustryValue = ref('retail');
const dropdownHoverValue = ref('');
const dropdownFocusValue = ref('');
const dropdownDisabledValue = ref('');
const dropdownErrorValue = ref('');
const comboboxCountryValue = ref('');
const comboboxCityValue = ref('hcm');
const comboboxMultipleValue = ref<string[]>(['retail', 'technology', 'finance']);
const comboboxSplitValue = ref('');
const comboboxMultipleSplitValue = ref<string[]>(['an-phat', 'binh-minh', 'dai-nam']);
const comboboxHoverValue = ref('');
const comboboxFocusValue = ref('');
const comboboxDisabledValue = ref('');
const comboboxErrorValue = ref('');
const comboboxLoadingValue = ref('');
const datePickerValue = ref('14/06/2024');
const dateTimePickerValue = ref('14/06/2024 09:30');
const timePickerValue = ref('09:30');
const timePickerWithSecondsValue = ref('09:30:45');
const monthYearPickerValue = ref('06/2024');
const dateRangeValue = ref<DateRangeModel>({
  start: '10/06/2024',
  end: '14/06/2024'
});
const dateRangeAdvancedValue = ref<DateRangeModel>({
  start: '10/06/2024',
  end: '14/06/2024'
});
const contextMenuOpen = ref(false);
const contextMenuX = ref(0);
const contextMenuY = ref(0);
const contextMenuSelection = ref('Chưa chọn thao tác nào.');
const checkboxProfileValue = ref(false);
const checkboxErrorValue = ref(true);
const childCheckboxAgentValue = ref(true);
const childCheckboxTenantValue = ref(false);
const radioStatusValue = ref('active');
const radioErrorValue = ref('');

const parentCheckboxValue = computed({
  get() {
    return childCheckboxAgentValue.value && childCheckboxTenantValue.value;
  },
  set(value: boolean) {
    childCheckboxAgentValue.value = value;
    childCheckboxTenantValue.value = value;
  }
});

const parentCheckboxIndeterminate = computed(() => {
  const selectedCount = [childCheckboxAgentValue.value, childCheckboxTenantValue.value].filter(Boolean).length;
  return selectedCount > 0 && selectedCount < 2;
});

const supplierOptions: DropdownOption[] = [
  { label: 'Công ty An Phát', value: 'an-phat' },
  { label: 'Công ty Bình Minh', value: 'binh-minh' },
  { label: 'Công ty Đại Nam', value: 'dai-nam' },
  { label: 'Công ty Hoàng Gia', value: 'hoang-gia' },
  { label: 'Công ty Minh Long', value: 'minh-long' },
  { label: 'Công ty Phú Khang', value: 'phu-khang' },
  { label: 'Công ty Thành Công', value: 'thanh-cong' }
];

const accountOptions: DropdownOption[] = [
  { label: 'ACB - 986 456 001', value: 'acb' },
  { label: 'BIDV - 126 002 789', value: 'bidv' },
  { label: 'Techcombank - 190 388 198', value: 'techcombank' },
  { label: 'Vietcombank - 102 009 456', value: 'vietcombank' },
  { label: 'VPBank - 881 445 200', value: 'vpbank' }
];

const industryOptions: DropdownOption[] = [
  { label: 'Bán lẻ', value: 'retail' },
  { label: 'Công nghệ thông tin', value: 'technology' },
  { label: 'Dịch vụ tài chính', value: 'finance' },
  {
    label: 'Sản xuất và phân phối thiết bị công nghiệp quy mô lớn',
    value: 'industrial-equipment'
  },
  { label: 'Thương mại điện tử', value: 'ecommerce' },
  { label: 'Y tế', value: 'healthcare' }
];

const countryOptions: ComboboxOption[] = [
  { label: 'Canada', value: 'canada', description: 'Bắc Mỹ' },
  { label: 'Hàn Quốc', value: 'korea', description: 'Đông Á' },
  { label: 'Nhật Bản', value: 'japan', description: 'Đông Á' },
  { label: 'Singapore', value: 'singapore', description: 'Đông Nam Á' },
  { label: 'Thái Lan', value: 'thailand', description: 'Đông Nam Á' },
  { label: 'Việt Nam', value: 'vietnam', description: 'Đông Nam Á' }
];

const cityOptions: ComboboxOption[] = [
  { label: 'Cần Thơ', value: 'can-tho', description: 'Thành phố trực thuộc trung ương' },
  { label: 'Đà Nẵng', value: 'da-nang', description: 'Thành phố trực thuộc trung ương' },
  { label: 'Hà Nội', value: 'ha-noi', description: 'Thủ đô' },
  { label: 'Hải Phòng', value: 'hai-phong', description: 'Thành phố cảng' },
  { label: 'TP. Hồ Chí Minh', value: 'hcm', description: 'Trung tâm kinh tế phía Nam' },
  { label: 'Huế', value: 'hue', description: 'Thành phố di sản' }
];

const comboboxIndustryOptions: ComboboxOption[] = industryOptions;
const comboboxSupplierOptions: ComboboxOption[] = supplierOptions;

const buttonVariants: Array<{
  variant: 'brand' | 'info' | 'warning' | 'danger' | 'success' | 'neutral' | 'neutralInverse';
  label: string;
}> = [
  { variant: 'brand', label: 'Brand' },
  { variant: 'info', label: 'Info' },
  { variant: 'warning', label: 'Warning' },
  { variant: 'danger', label: 'Danger' },
  { variant: 'success', label: 'Success' },
  { variant: 'neutral', label: 'Neutral' },
  { variant: 'neutralInverse', label: 'Neutral Inv.' }
] as const;

const toastSamples: Array<{
  tone: ToastTone;
  label: string;
  message: string;
}> = [
  {
    tone: 'success',
    label: 'Success',
    message: 'Đã lưu thông tin thành công.'
  },
  {
    tone: 'error',
    label: 'Error',
    message: 'Đã xảy ra lỗi hệ thống, vui lòng thử lại sau.'
  },
  {
    tone: 'warning',
    label: 'Warning',
    message: 'Tính năng đang phát triển. Vui lòng quay lại sau.'
  },
  {
    tone: 'info',
    label: 'Info',
    message: 'Đã cập nhật phiên bản phần mềm mới nhất.'
  }
] as const;

const notificationSamples: Array<{
  tone: NotificationTone;
  label: string;
  title: string;
  message: string;
}> = [
  {
    tone: 'success',
    label: 'Success',
    title: 'Nhập khẩu Hồ sơ thành công',
    message: '50 hồ sơ đã được nhập khẩu thành công vào hệ thống.'
  },
  {
    tone: 'error',
    label: 'Error',
    title: 'Không thể kết nối máy chủ',
    message: 'Ứng dụng không thể kết nối đến máy chủ. Vui lòng thử lại sau.'
  },
  {
    tone: 'warning',
    label: 'Warning',
    title: 'Dung lượng sắp đầy',
    message: 'Bạn đã sử dụng 90% dung lượng gói hiện tại.'
  },
  {
    tone: 'info',
    label: 'Info',
    title: 'Có bản cập nhật mới',
    message: 'Phiên bản 2.5.1 đã được phát hành. Vui lòng tải lại trang để áp dụng.'
  }
] as const;

const inlineSamples: Array<{
  tone: NotificationTone;
  label: string;
  title: string;
  message: string;
  compact: boolean;
  closable: boolean;
}> = [
  {
    tone: 'success',
    label: 'Success',
    title: 'Nhập khẩu Hồ sơ thành công',
    message: '50 hồ sơ đã được nhập khẩu thành công vào hệ thống.',
    compact: false,
    closable: true
  },
  {
    tone: 'success',
    label: 'Compact',
    title: 'Thành công',
    message: 'Đã lưu thành công.',
    compact: true,
    closable: false
  },
  {
    tone: 'error',
    label: 'Error',
    title: 'Không thể kết nối máy chủ',
    message: 'Ứng dụng không thể kết nối đến máy chủ. Vui lòng thử lại sau.',
    compact: false,
    closable: true
  },
  {
    tone: 'warning',
    label: 'Warning',
    title: 'Dung lượng sắp đầy',
    message: 'Bạn đã sử dụng 90% dung lượng gói hiện tại.',
    compact: true,
    closable: true
  },
  {
    tone: 'info',
    label: 'Info',
    title: 'Có bản cập nhật mới',
    message: 'Phiên bản 2.5.1 đã được phát hành. Vui lòng tải lại trang để áp dụng.',
    compact: false,
    closable: false
  }
] as const;

const dataTableColumns: DataTableColumn[] = [
  { key: 'name', label: 'Text', minWidth: '220px', filterable: true },
  { key: 'dateTime', label: 'Date Time', minWidth: '160px', filterable: true },
  {
    key: 'amount',
    label: 'Number',
    minWidth: '140px',
    align: 'right',
    sortable: true,
    filterable: true,
    formatter: (value) => Number(value ?? 0).toLocaleString('vi-VN')
  }
];

const dataTableRows: DataTableRow[] = [
  { id: 1, name: 'Text', dateTime: '01/10/2024 08:15', amount: 2350000 },
  { id: 2, name: 'Text', dateTime: '15/09/2024 10:30', amount: 16500000 },
  { id: 3, name: 'Text', dateTime: '23/09/2024 14:45', amount: 8200000 },
  { id: 4, name: 'Text', dateTime: '07/10/2024 16:20', amount: 12000000 },
  { id: 5, name: 'Text', dateTime: '19/09/2024 09:05', amount: 5750000 },
  { id: 6, name: 'Text', dateTime: '30/09/2024 11:50', amount: 9600000 },
  { id: 7, name: 'Text', dateTime: '11/10/2024 13:10', amount: 14300000 },
  { id: 8, name: 'Text', dateTime: '25/09/2024 19:35', amount: 7700000 },
  { id: 9, name: 'Text', dateTime: '08/10/2024 07:40', amount: 18900000 },
  { id: 10, name: 'Text', dateTime: '21/10/2024 20:55', amount: 3800000 }
];

const dataTableDropdownFilters: DataTableDropdownFilter[] = [
  {
    id: 'name',
    label: 'Text',
    options: [
      {
        id: 'text-default',
        label: 'Text',
        value: 'text-default',
        predicate: (row) => String(row.name ?? '') === 'Text'
      }
    ]
  },
  {
    id: 'dateTime',
    label: 'Date Time',
    options: [
      {
        id: 'october',
        label: 'Tháng 10/2024',
        value: 'october',
        predicate: (row) => String(row.dateTime ?? '').includes('/10/2024')
      },
      {
        id: 'september',
        label: 'Tháng 09/2024',
        value: 'september',
        predicate: (row) => String(row.dateTime ?? '').includes('/09/2024')
      }
    ]
  },
  {
    id: 'amount',
    label: 'Number',
    options: [
      {
        id: 'high-value',
        label: 'Giá trị > 10tr',
        value: 'high-value',
        predicate: (row) => Number(row.amount ?? 0) > 10000000
      },
      {
        id: 'low-value',
        label: 'Giá trị < 8tr',
        value: 'low-value',
        predicate: (row) => Number(row.amount ?? 0) < 8000000
      }
    ]
  }
];

const dataTableToolbarActions: DataTableToolbarAction[] = [
  { id: 'reload', label: 'Tải lại', icon: IconRefresh },
  { id: 'export', label: 'Xuất khẩu dữ liệu', icon: IconDownload },
  { id: 'settings', label: 'Thiết lập bảng', icon: IconSettings },
  { id: 'filter', label: 'Bộ lọc nâng cao', icon: IconFilter }
];

const dataTableBulkActions: DataTableToolbarAction[] = [
  { id: 'approve', label: 'Duyệt', icon: IconEye },
  { id: 'edit', label: 'Chỉnh sửa', icon: IconPencil },
  { id: 'export', label: 'Xuất khẩu', icon: IconDownload },
  { id: 'archive', label: 'Lưu trữ', icon: IconDots },
  { id: 'assign', label: 'Giao việc', icon: IconChevronDown },
  { id: 'delete', label: 'Xóa', icon: IconTrash }
];

const dataTableSummaryRow = {
  name: 'Tổng cộng',
  dateTime: '',
  amount: '98.500.000'
};

const currentDate = today();

function startOfWeek(date: Date) {
  const nextDate = new Date(date.getFullYear(), date.getMonth(), date.getDate());
  const dayOffset = (nextDate.getDay() + 6) % 7;
  nextDate.setDate(nextDate.getDate() - dayOffset);
  return nextDate;
}

function endOfWeek(date: Date) {
  const nextDate = startOfWeek(date);
  nextDate.setDate(nextDate.getDate() + 6);
  return nextDate;
}

function toPresetRange(label: string, start: Date, end: Date): DateRangePreset {
  return {
    label,
    value: {
      start: formatDate(start),
      end: formatDate(end)
    }
  };
}

const dateRangePresets: DateRangePreset[] = [
  toPresetRange('7 ngày qua', addDays(currentDate, -6), currentDate),
  toPresetRange('30 ngày qua', addDays(currentDate, -29), currentDate),
  toPresetRange('Tuần này', startOfWeek(currentDate), endOfWeek(currentDate)),
  toPresetRange('Tuần trước', addDays(startOfWeek(currentDate), -7), addDays(startOfWeek(currentDate), -1)),
  toPresetRange('Tháng này', startOfMonth(currentDate), endOfMonth(currentDate)),
  toPresetRange(
    'Tháng trước',
    startOfMonth(addMonths(currentDate, -1)),
    endOfMonth(addMonths(currentDate, -1))
  ),
  toPresetRange('Năm nay', new Date(currentDate.getFullYear(), 0, 1), new Date(currentDate.getFullYear(), 11, 31)),
  toPresetRange('Năm trước', new Date(currentDate.getFullYear() - 1, 0, 1), new Date(currentDate.getFullYear() - 1, 11, 31))
];

const contextMenuItems: ContextMenuItem[] = [
  {
    id: 'view',
    label: 'Xem chi tiết',
    icon: IconEye
  },
  {
    id: 'edit',
    label: 'Chỉnh sửa',
    icon: IconPencil
  },
  {
    id: 'download',
    label: 'Tải xuống',
    icon: IconDownload,
    disabled: true
  },
  {
    id: 'delete',
    label: 'Xóa',
    icon: IconTrash,
    danger: true,
    separatorBefore: true
  }
];

function openContextMenuAt(x: number, y: number) {
  contextMenuX.value = x;
  contextMenuY.value = y;
  contextMenuOpen.value = true;
}

function openContextMenuFromPointer(event: MouseEvent) {
  openContextMenuAt(event.clientX, event.clientY);
}

function openContextMenuFromButton(event: MouseEvent) {
  const target = event.currentTarget as HTMLElement | null;
  const rect = target?.getBoundingClientRect();
  if (!rect) {
    return;
  }

  openContextMenuAt(rect.left, rect.bottom + 4);
}

function openContextMenuFromContextButton(event: MouseEvent) {
  event.preventDefault();
  const target = event.currentTarget as HTMLElement | null;
  const rect = target?.getBoundingClientRect();
  if (!rect) {
    return;
  }

  openContextMenuAt(rect.left, rect.bottom + 4);
}

function handleContextMenuSelect(item: ContextMenuItem) {
  contextMenuSelection.value = `Đã chọn: ${item.label}`;
}

function getDataTableRowActions() {
  return [
    { id: 'view', label: 'Xem chi tiết', icon: IconEye },
    { id: 'edit', label: 'Chỉnh sửa', icon: IconPencil },
    { id: 'download', label: 'Tải xuống', icon: IconDownload },
    { id: 'delete', label: 'Xóa', icon: IconTrash, danger: true }
  ];
}

function handleDataTableAction(actionId: string) {
  console.log('DataTable toolbar action:', actionId);
}

function handleDataTableBulkAction(actionId: string, rowIds: Array<string | number>) {
  console.log('DataTable bulk action:', actionId, rowIds);
}

function handleDataTableRowAction(actionId: string, row: DataTableRow) {
  console.log('DataTable row action:', actionId, row);
}

function showToast(sample: (typeof toastSamples)[number]) {
  toastStore.push({
    tone: sample.tone,
    title: sample.label,
    message: sample.message
  });
}

function showNotification(sample: (typeof notificationSamples)[number]) {
  notificationStore.push({
    tone: sample.tone,
    title: sample.title,
    message: sample.message
  });
}

function openDialog() {
  isDialogOpen.value = true;
}

function closeDialog() {
  isDialogOpen.value = false;
}

function openPopupInline() {
  isPopupInlineOpen.value = true;
}

function closePopupInline() {
  isPopupInlineOpen.value = false;
}

function openPopupInlineTwoColumns() {
  isPopupInlineTwoColumnsOpen.value = true;
}

function closePopupInlineTwoColumns() {
  isPopupInlineTwoColumnsOpen.value = false;
}

function openPopupTopOneColumn() {
  isPopupTopOneColumnOpen.value = true;
}

function closePopupTopOneColumn() {
  isPopupTopOneColumnOpen.value = false;
}

function openPopupTopTwoColumns() {
  isPopupTopTwoColumnsOpen.value = true;
}

function closePopupTopTwoColumns() {
  isPopupTopTwoColumnsOpen.value = false;
}
</script>

<template>
  <section class="test-page">
    <header class="test-page__header">
      <div>
        <h1 class="test-page__title">Test</h1>
        <p class="test-page__description">Màn trống để test toast và notification.</p>
      </div>
    </header>

    <div class="test-page__grid">
      <article class="test-page__card test-page__card--full">
        <h2 class="test-page__card-title">Text Fields</h2>

        <div class="test-page__field-section">
          <div class="test-page__field-group">
            <p class="test-page__group-label">Normal</p>
            <TextBoxTopLabel
              v-model="textFieldNormalValue"
              label="Họ và tên"
              required
              placeholder="Nhập họ và tên"
              hint="Dùng khi form ít trường và cần đọc từ trên xuống."
            />
          </div>

          <div class="test-page__field-group">
            <p class="test-page__group-label">Inline label</p>
            <TextBoxInlineLabel
              v-model="textFieldNormalValue"
              label="Mã agent"
              placeholder="Nhập mã agent"
            />
          </div>

          <div class="test-page__field-group test-page__field-group--narrow">
            <p class="test-page__group-label">Hover</p>
            <TextBoxTopLabel
              v-model="textFieldHoverValue"
              label="Tên agent"
              placeholder="Nhập tên agent"
              state="hover"
            />
          </div>

          <div class="test-page__field-group test-page__field-group--narrow">
            <p class="test-page__group-label">Focus</p>
            <TextBoxTopLabel
              v-model="textFieldFocusValue"
              label="Tên agent"
              placeholder="Nhập tên agent"
              state="focus"
            />
          </div>

          <div class="test-page__field-group">
            <p class="test-page__group-label">Readonly</p>
            <TextBoxTopLabel
              v-model="textFieldReadonlyValue"
              label="Mã agent"
              placeholder="Mã agent"
              state="readonly"
            />
          </div>

          <div class="test-page__field-group">
            <p class="test-page__group-label">Error</p>
            <TextBoxTopLabel
              v-model="textFieldErrorValue"
              label="Tên agent"
              placeholder="Nhập tên agent"
              error="Tên agent không được để trống."
              clearable
            />
          </div>

          <div class="test-page__field-group">
            <p class="test-page__group-label">Validate</p>
            <TextBoxTopLabel
              v-model="textFieldValidateValue"
              label="Tên agent"
              placeholder="Nhập tên agent"
              state="validate"
              validate-message="Tên agent hợp lệ."
              clearable
            />
          </div>

          <div class="test-page__field-group">
            <p class="test-page__group-label">Verifying</p>
            <TextBoxTopLabel
              v-model="textFieldVerifyingValue"
              label="Tìm kiếm"
              placeholder="Đang kiểm tra..."
              state="verifying"
              verifying-message="Đang kiểm tra dữ liệu..."
              clearable
            />
          </div>
        </div>
      </article>

      <article class="test-page__card test-page__card--full">
        <h2 class="test-page__card-title">Buttons</h2>

        <div class="test-page__button-section">
          <div class="test-page__button-group">
            <p class="test-page__group-label">BaseButton</p>
            <div class="test-page__actions">
              <BaseButton
                v-for="item in buttonVariants"
                :key="item.variant"
                :variant="item.variant"
                type="button"
              >
                {{ item.label }}
              </BaseButton>
            </div>
          </div>

          <div class="test-page__button-group">
            <p class="test-page__group-label">SplitButton</p>
            <div class="test-page__actions">
              <SplitButton variant="brand" @click="() => undefined" @menu-click="() => undefined">
                Lưu
              </SplitButton>
              <SplitButton variant="info" @click="() => undefined" @menu-click="() => undefined">
                Cập nhật
              </SplitButton>
              <SplitButton variant="danger" @click="() => undefined" @menu-click="() => undefined">
                Xóa
              </SplitButton>
            </div>
          </div>

          <div class="test-page__button-group">
            <p class="test-page__group-label">IconButton</p>
            <div class="test-page__actions">
              <IconButton ariaLabel="Thêm mới" title="Thêm mới">
                <IconPlus :size="20" stroke-width="1.5" aria-hidden="true" />
              </IconButton>
              <IconButton ariaLabel="Tải lại" title="Tải lại" variant="secondary">
                <IconRefresh :size="20" stroke-width="1.5" aria-hidden="true" />
              </IconButton>
              <IconButton ariaLabel="Lọc" title="Lọc" variant="secondary">
                <IconFilter :size="20" stroke-width="1.5" aria-hidden="true" />
              </IconButton>
              <IconButton ariaLabel="Xóa" title="Xóa" variant="danger">
                <IconTrash :size="20" stroke-width="1.5" aria-hidden="true" />
              </IconButton>
              <IconButton ariaLabel="Khác" title="Khác" variant="secondary">
                <IconDots :size="20" stroke-width="1.5" aria-hidden="true" />
              </IconButton>
            </div>
          </div>

          <div class="test-page__button-group">
            <p class="test-page__group-label">Context Menu</p>
            <div class="test-page__context-menu-demo">
              <div class="test-page__context-menu-canvas" @contextmenu.prevent="openContextMenuFromPointer">
                <div>
                  <strong>Nhấp chuột phải ở đây</strong>
                  <p>{{ contextMenuSelection }}</p>
                </div>
                <div class="test-page__context-menu-actions">
                  <button
                    type="button"
                    class="test-page__context-menu-trigger"
                    aria-label="Nút test context menu"
                    @contextmenu="openContextMenuFromContextButton"
                  >
                    Nút test
                  </button>
                  <button
                    type="button"
                    class="test-page__context-menu-trigger"
                    aria-label="Mở menu"
                    @click="openContextMenuFromButton"
                  >
                    <IconDotsVertical :size="20" stroke-width="1.5" aria-hidden="true" />
                  </button>
                  <button
                    type="button"
                    class="test-page__context-menu-trigger test-page__context-menu-trigger--dropdown"
                    aria-label="Mở menu xổ xuống"
                    @click="openContextMenuFromButton"
                  >
                    <span>Menu</span>
                    <IconChevronDown :size="20" stroke-width="1.5" aria-hidden="true" />
                  </button>
                </div>
              </div>
            </div>
          </div>
        </div>
        <ContextMenu
          v-model:open="contextMenuOpen"
          :x="contextMenuX"
          :y="contextMenuY"
          :items="contextMenuItems"
          @select="handleContextMenuSelect"
        />
      </article>

      <article class="test-page__card test-page__card--full">
        <h2 class="test-page__card-title">Choice Controls</h2>

        <div class="test-page__choice-grid">
          <div class="test-page__field-group">
            <p class="test-page__group-label">Checkbox</p>
            <div class="test-page__choice-stack">
              <Checkbox v-model="checkboxProfileValue" label="Quản lý Agent" />
              <Checkbox :model-value="true" label="Đã chọn" />
              <Checkbox v-model="checkboxErrorValue" label="Lỗi xác thực" error="Vui lòng chọn." />
            </div>
          </div>

          <div class="test-page__field-group">
            <p class="test-page__group-label">Checkbox Indeterminate</p>
            <div class="test-page__choice-stack">
              <Checkbox
                v-model="parentCheckboxValue"
                label="Chọn tất cả"
                :indeterminate="parentCheckboxIndeterminate"
              />
              <div class="test-page__choice-children">
                <Checkbox v-model="childCheckboxAgentValue" label="Xem danh sách Agent" />
                <Checkbox v-model="childCheckboxTenantValue" label="Quản lý Tenant" />
              </div>
            </div>
          </div>

          <div class="test-page__field-group">
            <p class="test-page__group-label">Radio Button</p>
            <div class="test-page__choice-stack">
              <RadioButton
                v-model="radioStatusValue"
                name="agent-status"
                value="draft"
                label="Bản nháp"
              />
              <RadioButton
                v-model="radioStatusValue"
                name="agent-status"
                value="active"
                label="Đang hoạt động"
              />
              <RadioButton
                v-model="radioErrorValue"
                name="agent-role"
                value="manager"
                label="Manager"
                error="Vui lòng chọn vai trò."
              />
            </div>
          </div>
        </div>
      </article>

      <article class="test-page__card test-page__card--full">
        <h2 class="test-page__card-title">Date Time Picker</h2>

        <div class="test-page__field-section test-page__dropdown-grid">
          <div class="test-page__field-group">
            <p class="test-page__group-label">Date</p>
            <DatePicker
              v-model="datePickerValue"
              label="Ngày bắt đầu"
              placeholder="DD/MM/YYYY"
              hint="Có thể nhập tay hoặc chọn từ popover."
            />
          </div>

          <div class="test-page__field-group">
            <p class="test-page__group-label">Date Time</p>
            <DateTimePicker
              v-model="dateTimePickerValue"
              label="Lịch họp"
              placeholder="DD/MM/YYYY HH:mm"
            />
          </div>

          <div class="test-page__field-group">
            <p class="test-page__group-label">Time</p>
            <TimePicker
              v-model="timePickerValue"
              label="Giờ bắt đầu"
              placeholder="HH:mm"
            />
          </div>

          <div class="test-page__field-group">
            <p class="test-page__group-label">Time with seconds</p>
            <TimePickerWithSeconds
              v-model="timePickerWithSecondsValue"
              label="Giờ chi tiết"
              placeholder="HH:mm:ss"
            />
          </div>

          <div class="test-page__field-group">
            <p class="test-page__group-label">Month Year</p>
            <MonthYearPicker
              v-model="monthYearPickerValue"
              label="Kỳ báo cáo"
              placeholder="MM/YYYY"
            />
          </div>

          <div class="test-page__field-group">
            <p class="test-page__group-label">Date Range</p>
            <DateRangePicker
              v-model="dateRangeValue"
              label="Khoảng thời gian"
              placeholder="DD/MM/YYYY - DD/MM/YYYY"
            />
          </div>

          <div class="test-page__field-group test-page__field-group--wide">
            <p class="test-page__group-label">Date Range Advanced</p>
            <DateRangePickerAdvanced
              v-model="dateRangeAdvancedValue"
              label="Báo cáo theo thời gian"
              placeholder="DD/MM/YYYY - DD/MM/YYYY"
              :presets="dateRangePresets"
            />
          </div>
        </div>
      </article>

      <article class="test-page__card test-page__card--full">
        <h2 class="test-page__card-title">Dropdown List</h2>

        <div class="test-page__field-section test-page__dropdown-grid">
          <div class="test-page__field-group">
            <p class="test-page__group-label">Normal</p>
            <DropdownList
              v-model="dropdownSupplierValue"
              label="Nhà cung cấp"
              placeholder="Chọn nhà cung cấp"
              :options="supplierOptions"
              hint="Dùng khi danh sách có từ 6 đến 10 lựa chọn."
            />
          </div>

          <div class="test-page__field-group">
            <p class="test-page__group-label">Selected</p>
            <DropdownList
              v-model="dropdownAccountValue"
              label="Tài khoản"
              placeholder="Chọn tài khoản"
              :options="accountOptions"
            />
          </div>

          <div class="test-page__field-group test-page__field-group--narrow">
            <p class="test-page__group-label">Long option</p>
            <DropdownList
              v-model="dropdownIndustryValue"
              label="Ngành nghề"
              placeholder="Chọn ngành nghề"
              :options="industryOptions"
            />
          </div>

          <div class="test-page__field-group test-page__field-group--narrow">
            <p class="test-page__group-label">Hover</p>
            <DropdownList
              v-model="dropdownHoverValue"
              label="Nhà cung cấp"
              placeholder="Chọn nhà cung cấp"
              :options="supplierOptions"
              state="hover"
            />
          </div>

          <div class="test-page__field-group test-page__field-group--narrow">
            <p class="test-page__group-label">Focus</p>
            <DropdownList
              v-model="dropdownFocusValue"
              label="Nhà cung cấp"
              placeholder="Chọn nhà cung cấp"
              :options="supplierOptions"
              state="focus"
            />
          </div>

          <div class="test-page__field-group test-page__field-group--narrow">
            <p class="test-page__group-label">Disabled</p>
            <DropdownList
              v-model="dropdownDisabledValue"
              label="Nhà cung cấp"
              placeholder="Chọn nhà cung cấp"
              :options="supplierOptions"
              disabled
            />
          </div>

          <div class="test-page__field-group test-page__field-group--narrow">
            <p class="test-page__group-label">Error</p>
            <DropdownList
              v-model="dropdownErrorValue"
              label="Nhà cung cấp"
              placeholder="Chọn nhà cung cấp"
              :options="supplierOptions"
              error="Vui lòng chọn nhà cung cấp."
            />
          </div>
        </div>
      </article>

      <article class="test-page__card test-page__card--full">
        <h2 class="test-page__card-title">Combobox</h2>

        <div class="test-page__field-section test-page__dropdown-grid">
          <div class="test-page__field-group">
            <p class="test-page__group-label">Single</p>
            <ComboboxSingle
              v-model="comboboxCountryValue"
              label="Quốc gia"
              placeholder="Chọn quốc gia"
              :options="countryOptions"
              hint="Có thể gõ trực tiếp để lọc danh sách."
            />
          </div>

          <div class="test-page__field-group">
            <p class="test-page__group-label">Single selected</p>
            <ComboboxSingle
              v-model="comboboxCityValue"
              label="Tỉnh thành"
              placeholder="Chọn tỉnh thành"
              :options="cityOptions"
            />
          </div>

          <div class="test-page__field-group">
            <p class="test-page__group-label">Multiple</p>
            <ComboboxMultiple
              v-model="comboboxMultipleValue"
              label="Ngành nghề"
              placeholder="Chọn ngành nghề"
              :options="comboboxIndustryOptions"
            />
          </div>

          <div class="test-page__field-group">
            <p class="test-page__group-label">Single Split</p>
            <ComboboxSingleSplit
              v-model="comboboxSplitValue"
              label="Khách hàng"
              placeholder="Chọn khách hàng"
              :options="comboboxSupplierOptions"
            />
          </div>

          <div class="test-page__field-group">
            <p class="test-page__group-label">Multiple Split</p>
            <ComboboxMultipleSplit
              v-model="comboboxMultipleSplitValue"
              label="Nhà cung cấp"
              placeholder="Chọn nhà cung cấp"
              :options="comboboxSupplierOptions"
            />
          </div>

          <div class="test-page__field-group test-page__field-group--narrow">
            <p class="test-page__group-label">Hover</p>
            <ComboboxSingle
              v-model="comboboxHoverValue"
              label="Quốc gia"
              placeholder="Chọn quốc gia"
              :options="countryOptions"
              state="hover"
            />
          </div>

          <div class="test-page__field-group test-page__field-group--narrow">
            <p class="test-page__group-label">Focus</p>
            <ComboboxSingle
              v-model="comboboxFocusValue"
              label="Quốc gia"
              placeholder="Chọn quốc gia"
              :options="countryOptions"
              state="focus"
            />
          </div>

          <div class="test-page__field-group test-page__field-group--narrow">
            <p class="test-page__group-label">Disabled</p>
            <ComboboxSingle
              v-model="comboboxDisabledValue"
              label="Quốc gia"
              placeholder="Chọn quốc gia"
              :options="countryOptions"
              disabled
            />
          </div>

          <div class="test-page__field-group test-page__field-group--narrow">
            <p class="test-page__group-label">Error</p>
            <ComboboxSingle
              v-model="comboboxErrorValue"
              label="Quốc gia"
              placeholder="Chọn quốc gia"
              :options="countryOptions"
              error="Vui lòng chọn quốc gia."
            />
          </div>

          <div class="test-page__field-group test-page__field-group--narrow">
            <p class="test-page__group-label">Loading</p>
            <ComboboxSingle
              v-model="comboboxLoadingValue"
              label="Quốc gia"
              placeholder="Đang tải quốc gia"
              :options="countryOptions"
              loading
            />
          </div>
        </div>
      </article>

      <article class="test-page__card test-page__card--full">
        <DataTable
          :columns="dataTableColumns"
          :rows="dataTableRows"
          :dropdown-filters="dataTableDropdownFilters"
          :toolbar-actions="dataTableToolbarActions"
          :bulk-actions="dataTableBulkActions"
          :summary-row="dataTableSummaryRow"
          :get-row-actions="getDataTableRowActions"
          @table-action="handleDataTableAction"
          @bulk-action="handleDataTableBulkAction"
          @row-action="handleDataTableRowAction"
        />
      </article>

      <article class="test-page__card">
        <h2 class="test-page__card-title">Toast</h2>
        <div class="test-page__actions">
          <BaseButton
            v-for="sample in toastSamples"
            :key="sample.tone"
            variant="secondary"
            type="button"
            @click="showToast(sample)"
          >
            {{ sample.label }}
          </BaseButton>
        </div>
      </article>

      <article class="test-page__card">
        <h2 class="test-page__card-title">Notification</h2>
        <div class="test-page__actions">
          <BaseButton
            v-for="sample in notificationSamples"
            :key="sample.tone"
            variant="secondary"
            type="button"
            @click="showNotification(sample)"
          >
            {{ sample.label }}
          </BaseButton>
        </div>
      </article>

      <article class="test-page__card test-page__card--full">
        <h2 class="test-page__card-title">Inline Notification</h2>
        <div class="test-page__inline-stack">
          <InlineNotification
            v-for="sample in inlineSamples"
            :key="`${sample.tone}-${sample.label}-${sample.compact ? 'compact' : 'default'}`"
            :tone="sample.tone"
            :title="sample.title"
            :message="sample.message"
            :compact="sample.compact"
            :closable="sample.closable"
          >
            <template v-if="sample.tone === 'info'" #action>
              <BaseButton variant="secondary" type="button">Tải lại</BaseButton>
            </template>
          </InlineNotification>
        </div>
      </article>

      <article class="test-page__card test-page__card--full">
        <h2 class="test-page__card-title">Dialog</h2>
        <div class="test-page__actions">
          <BaseButton variant="secondary" type="button" @click="openDialog">
            Mở dialog
          </BaseButton>
        </div>
      </article>

      <article class="test-page__card test-page__card--full">
        <h2 class="test-page__card-title">Popup Inline 1 Cột</h2>
        <div class="test-page__actions">
          <BaseButton variant="secondary" type="button" @click="openPopupInline">
            Mở popup
          </BaseButton>
        </div>
      </article>

      <article class="test-page__card test-page__card--full">
        <h2 class="test-page__card-title">Popup Inline 2 Cột</h2>
        <div class="test-page__actions">
          <BaseButton variant="secondary" type="button" @click="openPopupInlineTwoColumns">
            Mở popup
          </BaseButton>
        </div>
      </article>

      <article class="test-page__card test-page__card--full">
        <h2 class="test-page__card-title">Popup Top 1 Cột</h2>
        <div class="test-page__actions">
          <BaseButton variant="secondary" type="button" @click="openPopupTopOneColumn">
            Mở popup
          </BaseButton>
        </div>
      </article>

      <article class="test-page__card test-page__card--full">
        <h2 class="test-page__card-title">Popup Top 2 Cột</h2>
        <div class="test-page__actions">
          <BaseButton variant="secondary" type="button" @click="openPopupTopTwoColumns">
            Mở popup
          </BaseButton>
        </div>
      </article>

    </div>

    <Dialog
      :open="isDialogOpen"
      title="Xóa 2 tài liệu?"
      description="Các tài liệu bạn đang chọn sẽ bị xóa."
      confirm-label="Xóa"
      confirm-variant="danger"
      @cancel="closeDialog"
      @confirm="closeDialog"
    />

    <PopupInlineOneColumn
      :open="isPopupInlineOpen"
      title="Popup inline 1 cột"
      description="Popup cho form ngắn, label nằm bên trái và control nằm bên phải."
      @cancel="closePopupInline"
      @confirm="closePopupInline"
    >
      <div class="popup-form">
        <div class="popup-form__row">
          <span class="popup-form__label">Họ và tên</span>
          <div class="popup-form__control">
            <TextBoxTopLabel v-model="textFieldNormalValue" label-position="none" placeholder="Nhập họ và tên" />
          </div>
        </div>

        <div class="popup-form__row">
          <span class="popup-form__label">Quốc gia</span>
          <div class="popup-form__control">
            <ComboboxSingle
              v-model="comboboxCountryValue"
              label-position="none"
              placeholder="Chọn quốc gia"
              :options="countryOptions"
            />
          </div>
        </div>

        <div class="popup-form__row">
          <span class="popup-form__label">Ngày bắt đầu</span>
          <div class="popup-form__control">
            <DatePicker v-model="datePickerValue" label-position="none" placeholder="DD/MM/YYYY" />
          </div>
        </div>
      </div>
    </PopupInlineOneColumn>

    <PopupInlineTwoColumns
      :open="isPopupInlineTwoColumnsOpen"
      title="Popup inline 2 cột"
      description="Popup cho form nhiều trường, chia 2 cột với khoảng cách 32px."
      @cancel="closePopupInlineTwoColumns"
      @confirm="closePopupInlineTwoColumns"
    >
      <div class="popup-form">
        <div class="form-column">
          <div class="form-field">
            <span class="form-field__label">Họ và tên</span>
            <div class="form-field__control">
              <TextBoxTopLabel v-model="textFieldNormalValue" label-position="none" placeholder="Nhập họ và tên" />
            </div>
          </div>

          <div class="form-field">
            <span class="form-field__label">Quốc gia</span>
            <div class="form-field__control">
              <ComboboxSingle
                v-model="comboboxCountryValue"
                label-position="none"
                placeholder="Chọn quốc gia"
                :options="countryOptions"
              />
            </div>
          </div>

          <div class="form-field">
            <span class="form-field__label">Ngày bắt đầu</span>
            <div class="form-field__control">
              <DatePicker v-model="datePickerValue" label-position="none" placeholder="DD/MM/YYYY" />
            </div>
          </div>
        </div>

        <div class="form-column">
          <div class="form-field">
            <span class="form-field__label">Mã agent</span>
            <div class="form-field__control">
              <TextBoxTopLabel v-model="textFieldHoverValue" label-position="none" placeholder="Nhập mã agent" />
            </div>
          </div>

          <div class="form-field">
            <span class="form-field__label">Loại</span>
            <div class="form-field__control">
              <ComboboxSingle
                v-model="comboboxCityValue"
                label-position="none"
                placeholder="Chọn loại"
                :options="cityOptions"
              />
            </div>
          </div>

          <div class="form-field">
            <span class="form-field__label">Kỳ báo cáo</span>
            <div class="form-field__control">
              <MonthYearPicker v-model="monthYearPickerValue" label-position="none" placeholder="MM/YYYY" />
            </div>
          </div>
        </div>
      </div>
    </PopupInlineTwoColumns>

    <PopupTopOneColumn
      :open="isPopupTopOneColumnOpen"
      title="Popup top 1 cột"
      description="Popup label nằm trên control cho form 1 cột."
      @cancel="closePopupTopOneColumn"
      @confirm="closePopupTopOneColumn"
    >
      <div class="popup-form">
        <div class="popup-form__field">
          <span class="popup-form__label">Họ và tên</span>
          <div class="popup-form__control">
            <TextBoxTopLabel v-model="textFieldNormalValue" label-position="none" placeholder="Nhập họ và tên" />
          </div>
        </div>

        <div class="popup-form__field">
          <span class="popup-form__label">Quốc gia</span>
          <div class="popup-form__control">
            <ComboboxSingle
              v-model="comboboxCountryValue"
              label-position="none"
              placeholder="Chọn quốc gia"
              :options="countryOptions"
            />
          </div>
        </div>

        <div class="popup-form__field">
          <span class="popup-form__label">Ngày bắt đầu</span>
          <div class="popup-form__control">
            <DatePicker v-model="datePickerValue" label-position="none" placeholder="DD/MM/YYYY" />
          </div>
        </div>
      </div>
    </PopupTopOneColumn>

    <PopupTopTwoColumns
      :open="isPopupTopTwoColumnsOpen"
      title="Popup top 2 cột"
      description="Popup label nằm trên control cho form nhiều trường, chia 2 cột."
      @cancel="closePopupTopTwoColumns"
      @confirm="closePopupTopTwoColumns"
    >
      <div class="popup-form">
        <div class="popup-form__column">
          <div class="popup-form__field">
            <span class="popup-form__label">Họ và tên</span>
            <div class="popup-form__control">
              <TextBoxTopLabel v-model="textFieldNormalValue" label-position="none" placeholder="Nhập họ và tên" />
            </div>
          </div>

          <div class="popup-form__field">
            <span class="popup-form__label">Quốc gia</span>
            <div class="popup-form__control">
              <ComboboxSingle
                v-model="comboboxCountryValue"
                label-position="none"
                placeholder="Chọn quốc gia"
                :options="countryOptions"
              />
            </div>
          </div>

          <div class="popup-form__field">
            <span class="popup-form__label">Ngày bắt đầu</span>
            <div class="popup-form__control">
              <DatePicker v-model="datePickerValue" label-position="none" placeholder="DD/MM/YYYY" />
            </div>
          </div>
        </div>

        <div class="popup-form__column">
          <div class="popup-form__field">
            <span class="popup-form__label">Mã agent</span>
            <div class="popup-form__control">
              <TextBoxTopLabel v-model="textFieldHoverValue" label-position="none" placeholder="Nhập mã agent" />
            </div>
          </div>

          <div class="popup-form__field">
            <span class="popup-form__label">Loại</span>
            <div class="popup-form__control">
              <ComboboxSingle
                v-model="comboboxCityValue"
                label-position="none"
                placeholder="Chọn loại"
                :options="cityOptions"
              />
            </div>
          </div>

          <div class="popup-form__field">
            <span class="popup-form__label">Kỳ báo cáo</span>
            <div class="popup-form__control">
              <MonthYearPicker v-model="monthYearPickerValue" label-position="none" placeholder="MM/YYYY" />
            </div>
          </div>
        </div>
      </div>
    </PopupTopTwoColumns>

  </section>
</template>

<style scoped>
.test-page {
  display: grid;
  gap: 16px;
}

.test-page__header {
  display: flex;
  align-items: flex-end;
  justify-content: space-between;
  gap: 16px;
}

.test-page__title {
  margin: 0;
  font-size: var(--font-size-h2);
  line-height: var(--line-height-h2);
  font-weight: 700;
}

.test-page__description {
  margin: 4px 0 0;
  color: var(--color-text-subtle);
}

.test-page__grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 16px;
}

.test-page__card--full {
  grid-column: 1 / -1;
}

.test-page__card {
  display: grid;
  gap: 12px;
  padding: 16px;
  border: 1px solid var(--color-border);
  border-radius: var(--radius-md);
  background: var(--color-surface);
  box-shadow: var(--shadow-card);
}

.test-page__card-title {
  margin: 0;
  font-size: var(--font-size-h3);
  line-height: var(--line-height-h3);
  font-weight: 700;
}

.test-page__button-section {
  display: grid;
  gap: 16px;
}

.test-page__field-section {
  display: grid;
  gap: 16px;
}

.test-page__dropdown-grid {
  grid-template-columns: repeat(2, minmax(0, 1fr));
}

.test-page__field-group {
  display: grid;
  gap: 8px;
}

.test-page__field-group--narrow {
  width: min(100%, 320px);
}

.test-page__field-group--wide {
  grid-column: 1 / -1;
}

.test-page__button-group {
  display: grid;
  gap: 8px;
}

.test-page__context-menu-demo {
  display: grid;
}

.test-page__context-menu-canvas {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 16px;
  min-height: 96px;
  padding: 16px;
  border: 1px dashed var(--color-border-strong);
  border-radius: var(--radius-md);
  background: linear-gradient(180deg, rgba(255, 255, 255, 1), rgba(245, 246, 248, 1));
}

.test-page__context-menu-canvas p {
  margin: 4px 0 0;
  color: var(--color-text-subtle);
}

.test-page__context-menu-actions {
  display: flex;
  align-items: center;
  gap: 8px;
  flex-shrink: 0;
}

.test-page__context-menu-trigger {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: 8px;
  min-height: 32px;
  padding: 0 10px;
  border: 1px solid var(--color-border);
  border-radius: 8px;
  background: var(--color-surface);
  color: var(--color-text);
  font: inherit;
  cursor: pointer;
}

.test-page__context-menu-trigger:hover {
  background: var(--color-surface-muted);
}

.test-page__context-menu-trigger--dropdown {
  padding-inline: 12px;
}

.test-page__group-label {
  margin: 0;
  color: var(--color-text-subtle);
  font-size: var(--font-size-body);
  line-height: var(--line-height-body);
  font-weight: 500;
}

.test-page__actions {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
}

.test-page__inline-stack {
  display: grid;
  gap: 16px;
}

.test-page__choice-grid {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  gap: 16px;
}

.test-page__choice-stack {
  display: grid;
  gap: 12px;
}

.test-page__choice-children {
  display: grid;
  gap: 12px;
  padding-left: 24px;
}

@media (max-width: 960px) {
  .test-page__grid {
    grid-template-columns: 1fr;
  }

  .test-page__dropdown-grid {
    grid-template-columns: 1fr;
  }

  .test-page__choice-grid {
    grid-template-columns: 1fr;
  }
}
</style>
