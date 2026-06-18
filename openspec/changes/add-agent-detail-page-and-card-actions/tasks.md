## 1. Detail Page Navigation

- [x] 1.1 Add frontend page-level navigation or route-state handling so clicking an agent card opens a dedicated agent detail screen.
- [x] 1.2 Move existing agent detail and edit presentation from the modal flow into the dedicated detail page while reusing current detail-loading logic.

## 2. Card Action Menu

- [x] 2.1 Add a top-right action menu on internal and tenant agent cards with `Xem chi tiết`, `Sửa`, and `Xóa` actions.
- [x] 2.2 Wire card menu actions so `Xem chi tiết` opens the detail page, `Sửa` opens the detail page in edit flow, and `Xóa` opens the existing confirmation flow.
- [x] 2.3 Ensure card menu clicks stop propagation so they do not also trigger the card's default click handler.

## 3. Context Preservation and Verification

- [x] 3.1 Preserve and restore active scope, selected tenant, filters, and pagination state when returning from the detail page.
- [x] 3.2 Refresh the correct internal or tenant agent list after save or delete from the detail page or card menu.
- [x] 3.3 Verify internal-agent and tenant-agent detail, edit, and delete actions still call the correct scoped APIs and respect tenant boundaries.
