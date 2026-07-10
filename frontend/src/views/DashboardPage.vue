<script setup lang="ts">
import {
  IconArrowDownRight,
  IconArrowUpRight,
  IconRefresh,
  IconSettings
} from '@tabler/icons-vue';

// Dashboard hiện dùng dữ liệu mẫu để giữ layout ổn định khi chưa nối nguồn tổng hợp thật.
const internalAgentStats = [
  {
    id: 'sales',
    initials: 'BH',
    avatarClass: 'dashboard-avatar--blue',
    name: 'AVA Bán hàng (Beta)',
    description: 'Agent hỗ trợ kinh doanh',
    metricValue: '0',
    metricLabel: 'Hoạt động bán hàng'
  },
  {
    id: 'attendance',
    initials: 'CC',
    avatarClass: 'dashboard-avatar--orange',
    name: 'AVA Chấm Công (Beta)',
    description: 'Agent hỗ trợ hoạt động chấm công',
    metricValue: '0',
    metricLabel: 'Số hội thoại'
  },
  {
    id: 'accounting',
    initials: 'KT',
    avatarClass: 'dashboard-avatar--purple',
    name: 'AVA Kế toán (Beta)',
    description: 'Agent hỗ trợ liên quan đến kế toán',
    metricValue: '0',
    metricLabel: 'Ghi chép hóa đơn/chứng từ'
  }
] as const;

// Danh sách agent bên dưới cũng là dữ liệu minh họa để layout dashboard có đủ trạng thái hiển thị.
const agentRows = [
  {
    id: '1',
    initials: 'LD',
    avatarClass: 'dashboard-avatar--green',
    name: 'lvvldiep_test1',
    description: 'lvvldiep_test',
    primaryLabel: 'Số hội thoại',
    primaryValue: '0',
    usageValue: '0',
    costValue: '0d'
  },
  {
    id: '2a',
    initials: 'MK',
    avatarClass: 'dashboard-avatar--orange',
    name: 'Mạnh Kế toán',
    description: 'Agent hỗ trợ nghiệp vụ kế toán nội bộ',
    primaryLabel: 'Số hội thoại',
    primaryValue: '0',
    usageValue: '0',
    costValue: '0d'
  },
  {
    id: '2b',
    initials: 'CS',
    avatarClass: 'dashboard-avatar--blue',
    name: 'CS Bán hàng',
    description: 'Agent hỗ trợ chăm sóc khách hàng',
    primaryLabel: 'Số hội thoại',
    primaryValue: '0',
    usageValue: '0',
    costValue: '0d'
  },
  {
    id: '2c',
    initials: 'HR',
    avatarClass: 'dashboard-avatar--purple',
    name: 'HR Tuyển dụng',
    description: 'Agent hỗ trợ tuyển dụng và nhân sự',
    primaryLabel: 'Số hội thoại',
    primaryValue: '0',
    usageValue: '0',
    costValue: '0d'
  },
  {
    id: '2',
    initials: 'MF',
    avatarClass: 'dashboard-avatar--green',
    name: 'Mạnh Food',
    description: 'Bán đồ ăn',
    primaryLabel: 'Số hội thoại',
    primaryValue: '0',
    usageValue: '0',
    costValue: '0d'
  },
  {
    id: '2d',
    initials: 'QC',
    avatarClass: 'dashboard-avatar--green',
    name: 'QC Kiểm soát',
    description: 'Agent hỗ trợ quy trình kiểm soát chất lượng',
    primaryLabel: 'Số hội thoại',
    primaryValue: '0',
    usageValue: '0',
    costValue: '0d'
  },
  {
    id: '2e',
    initials: 'KT',
    avatarClass: 'dashboard-avatar--orange',
    name: 'Kế toán tổng hợp',
    description: 'Agent hỗ trợ theo dõi chứng từ và sổ sách',
    primaryLabel: 'Số hội thoại',
    primaryValue: '0',
    usageValue: '0',
    costValue: '0d'
  },
  {
    id: '2f',
    initials: 'DV',
    avatarClass: 'dashboard-avatar--blue',
    name: 'Dịch vụ khách hàng',
    description: 'Agent hỗ trợ trả lời các yêu cầu dịch vụ',
    primaryLabel: 'Số hội thoại',
    primaryValue: '0',
    usageValue: '0',
    costValue: '0d'
  },
  {
    id: '2g',
    initials: 'IT',
    avatarClass: 'dashboard-avatar--purple',
    name: 'IT Hỗ trợ',
    description: 'Agent hỗ trợ xử lý yêu cầu kỹ thuật',
    primaryLabel: 'Số hội thoại',
    primaryValue: '0',
    usageValue: '0',
    costValue: '0d'
  },
  {
    id: '3',
    initials: 'BH',
    avatarClass: 'dashboard-avatar--blue',
    name: 'AVA Bán hàng (Beta)',
    description: 'Agent hỗ trợ nhân viên kinh doanh',
    primaryLabel: 'Hoạt động bán hàng',
    primaryValue: '0',
    usageValue: '0',
    costValue: '0d'
  },
  {
    id: '3a',
    initials: 'MK',
    avatarClass: 'dashboard-avatar--green',
    name: 'Marketing nội bộ',
    description: 'Agent hỗ trợ nội dung và chiến dịch marketing',
    primaryLabel: 'Hoạt động bán hàng',
    primaryValue: '0',
    usageValue: '0',
    costValue: '0d'
  },
  {
    id: '4',
    initials: 'CC',
    avatarClass: 'dashboard-avatar--orange',
    name: 'AVA Chấm Công (Beta)',
    description: 'Agent hỗ trợ hoạt động chấm công',
    primaryLabel: 'Số hội thoại',
    primaryValue: '0',
    usageValue: '0',
    costValue: '0d'
  },
  {
    id: '4a',
    initials: 'SN',
    avatarClass: 'dashboard-avatar--blue',
    name: 'Sale Nhanh',
    description: 'Agent hỗ trợ tạo đơn và tra cứu nhanh',
    primaryLabel: 'Số hội thoại',
    primaryValue: '0',
    usageValue: '0',
    costValue: '0d'
  },
  {
    id: '5',
    initials: 'KT',
    avatarClass: 'dashboard-avatar--purple',
    name: 'AVA Kế toán (Beta)',
    description: 'Agent hỗ trợ liên quan đến kế toán',
    primaryLabel: 'Ghi chép hóa đơn/chứng từ',
    primaryValue: '0',
    usageValue: '0',
    costValue: '0d'
  },
  {
    id: '5a',
    initials: 'TC',
    avatarClass: 'dashboard-avatar--purple',
    name: 'Tài chính',
    description: 'Agent hỗ trợ theo dõi chi phí và dòng tiền',
    primaryLabel: 'Số hội thoại',
    primaryValue: '0',
    usageValue: '0',
    costValue: '0d'
  }
] as const;
</script>

<template>
  <section class="dashboard-page">
    <div class="dashboard-page__header">
      <div>
        <h1 class="dashboard-page__title">Tổng quan</h1>
      </div>

      <div class="dashboard-page__toolbar">
        <span class="dashboard-page__updated">Cập nhật lần cuối: 08h51</span>
        <button class="dashboard-icon-button" type="button" aria-label="Tải lại dữ liệu">
          <IconRefresh :size="24" stroke-width="1.5" aria-hidden="true" />
        </button>
        <select class="dashboard-select" aria-label="Thời gian">
          <option>Tháng này</option>
          <option>Tuần này</option>
          <option>Hôm nay</option>
        </select>
      </div>
    </div>

    <section class="dashboard-stats" aria-label="Chỉ số tổng quan">
      <article class="dashboard-card dashboard-stat-card">
        <div class="dashboard-stat-card__title">Tổng Agent hoạt động</div>
        <div class="dashboard-stat-card__value">5</div>
        <div class="dashboard-trend dashboard-trend--up">
          <IconArrowUpRight :size="24" stroke-width="1.5" aria-hidden="true" />
          <strong>0</strong>
          <span>So với kì trước</span>
        </div>
      </article>

      <article class="dashboard-card dashboard-stat-card">
        <div class="dashboard-stat-card__title">Số lượt sử dụng</div>
        <div class="dashboard-stat-card__value">0</div>
        <div class="dashboard-trend dashboard-trend--down">
          <IconArrowDownRight :size="24" stroke-width="1.5" aria-hidden="true" />
          <strong>0</strong>
          <span>So với kì trước</span>
        </div>
      </article>

      <article class="dashboard-card dashboard-stat-card">
        <div class="dashboard-stat-card__title">Chi phí vận hành</div>
        <div class="dashboard-stat-card__value">0d</div>
        <div class="dashboard-trend dashboard-trend--down">
          <IconArrowDownRight :size="24" stroke-width="1.5" aria-hidden="true" />
          <strong>0</strong>
          <span>So với kì trước</span>
        </div>
      </article>
    </section>

    <section class="dashboard-panels">
      <article class="dashboard-card dashboard-panel">
        <div class="dashboard-panel__header">
          <div class="dashboard-panel__title">Hiệu quả của Agent nội bộ</div>
          <div class="dashboard-panel__actions">
            <span class="dashboard-page__updated">Cập nhật lần cuối: 08h51</span>
            <button class="dashboard-icon-button" type="button" aria-label="Tải lại">
              <IconRefresh :size="24" stroke-width="1.5" aria-hidden="true" />
            </button>
            <button class="dashboard-icon-button" type="button" aria-label="Thiết lập">
              <IconSettings :size="24" stroke-width="1.5" aria-hidden="true" />
            </button>
          </div>
        </div>

        <div class="dashboard-compact-list">
          <div v-for="item in internalAgentStats" :key="item.id" class="dashboard-compact-item">
            <div class="dashboard-agent">
              <div class="dashboard-avatar" :class="item.avatarClass">{{ item.initials }}</div>
              <div class="dashboard-agent__text">
                <div class="dashboard-agent__name">
                  <span class="dashboard-agent__name-text">{{ item.name }}</span>
                  <span class="dashboard-verified">✓</span>
                </div>
                <div class="dashboard-agent__description">{{ item.description }}</div>
              </div>
            </div>

            <div class="dashboard-compact-item__metric">
              <div class="dashboard-compact-item__value">{{ item.metricValue }}</div>
              <div class="dashboard-compact-item__label">{{ item.metricLabel }}</div>
            </div>
          </div>
        </div>
      </article>

      <article class="dashboard-card dashboard-panel">
        <div class="dashboard-panel__header">
          <div class="dashboard-panel__title">Hiệu quả của Agent bên ngoài</div>
          <div class="dashboard-panel__actions">
            <button class="dashboard-icon-button" type="button" aria-label="Tải lại">
              <IconRefresh :size="24" stroke-width="1.5" aria-hidden="true" />
            </button>
            <button class="dashboard-icon-button" type="button" aria-label="Thiết lập">
              <IconSettings :size="24" stroke-width="1.5" aria-hidden="true" />
            </button>
          </div>
        </div>

        <div class="dashboard-empty-state">
          <div>
            <div class="dashboard-empty-state__illustration" aria-hidden="true"></div>
            <div>Chưa có dữ liệu</div>
          </div>
        </div>
      </article>
    </section>

    <section class="dashboard-card dashboard-table-card">
      <div class="dashboard-table-card__header">
        <div class="dashboard-table-card__title-wrap">
          <div class="dashboard-panel__title">Danh sách Agent</div>
          <div class="dashboard-tabs" role="tablist" aria-label="Loại Agent">
            <button class="dashboard-tab dashboard-tab--active" type="button" role="tab" aria-selected="true">Agent nội bộ</button>
            <button class="dashboard-tab" type="button" role="tab" aria-selected="false">Agent bên ngoài</button>
          </div>
        </div>

        <div class="dashboard-panel__actions">
          <span class="dashboard-page__updated">Cập nhật lần cuối: 08h51</span>
          <button class="dashboard-icon-button" type="button" aria-label="Tải lại danh sách">
            <IconRefresh :size="24" stroke-width="1.5" aria-hidden="true" />
          </button>
          <button class="dashboard-icon-button" type="button" aria-label="Thiết lập bảng">
            <IconSettings :size="24" stroke-width="1.5" aria-hidden="true" />
          </button>
        </div>
      </div>

      <div class="dashboard-table-toolbar">
        <div class="dashboard-table-toolbar__left">
          <input class="dashboard-search" type="search" placeholder="Tìm kiếm Agent" />

          <select class="dashboard-select" aria-label="Lọc trạng thái">
            <option>Tất cả trạng thái</option>
            <option>Đang hoạt động</option>
            <option>Tạm dừng</option>
          </select>
        </div>
      </div>

      <div class="dashboard-table-wrap">
        <table class="dashboard-table">
          <thead>
            <tr>
              <th class="dashboard-table__agent-col">Agent</th>
              <th>Chỉ số chính</th>
              <th>Số lượt sử dụng</th>
              <th class="dashboard-table__cost-col">Chi phí</th>
              <th class="dashboard-table__action-col">Thao tác</th>
            </tr>
          </thead>

          <tbody>
            <tr v-for="row in agentRows" :key="row.id">
              <td>
                <div class="dashboard-agent">
                  <div class="dashboard-avatar" :class="row.avatarClass">{{ row.initials }}</div>
                  <div class="dashboard-agent__text">
                    <div class="dashboard-agent__name">
                      <span class="dashboard-agent__name-text">{{ row.name }}</span>
                      <span class="dashboard-verified">✓</span>
                    </div>
                    <div class="dashboard-agent__description">{{ row.description }}</div>
                  </div>
                </div>
              </td>
              <td>
                <div class="dashboard-table__label">{{ row.primaryLabel }}</div>
                <div class="dashboard-table__value">{{ row.primaryValue }}</div>
              </td>
              <td>
                <div class="dashboard-table__label">Số lượt sử dụng</div>
                <div class="dashboard-table__value">{{ row.usageValue }}</div>
              </td>
              <td class="dashboard-table__cost-col">
                <div class="dashboard-table__label">Chi phí</div>
                <div class="dashboard-table__value">{{ row.costValue }}</div>
              </td>
              <td class="dashboard-table__action-col">
                <a href="#" class="dashboard-detail-link">Xem chi tiết</a>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </section>
  </section>
</template>

<style scoped>
.dashboard-page {
  display: grid;
  gap: var(--content-gap);
}

.dashboard-page__header,
.dashboard-table-card__header,
.dashboard-panel__header,
.dashboard-table-toolbar,
.dashboard-table-card__footer {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 16px;
}

.dashboard-page__title,
.dashboard-panel__title {
  margin: 0;
  color: var(--color-text);
}

.dashboard-page__title {
  font-size: var(--font-size-h2);
  line-height: var(--line-height-h2);
  font-weight: 700;
}

.dashboard-page__toolbar,
.dashboard-panel__actions,
.dashboard-table-toolbar__left,
.dashboard-table-card__title-wrap {
  display: flex;
  align-items: center;
  gap: 8px;
}

.dashboard-toast-controls {
  display: flex;
  align-items: center;
  gap: 8px;
  flex-wrap: wrap;
}

.dashboard-toast-control {
  min-width: 0;
}

.dashboard-page__updated {
  color: var(--color-text-subtle);
  font-size: var(--font-size-body);
  font-style: italic;
  white-space: nowrap;
}

.dashboard-card {
  border: 1px solid var(--color-border);
  border-radius: var(--radius-md);
  background: var(--color-surface);
  box-shadow: var(--shadow-card);
}

.dashboard-select,
.dashboard-search {
  height: var(--field-height);
  border-radius: var(--field-radius);
}

.dashboard-select,
.dashboard-search {
  border: 1px solid var(--color-border);
  background: var(--color-surface);
  color: var(--color-text);
  font: inherit;
}

.dashboard-select {
  min-width: 168px;
  padding: 0 12px;
}

.dashboard-search {
  width: 260px;
  padding: 0 12px;
  color: var(--color-text-placeholder);
}

.dashboard-stats,
.dashboard-panels {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  gap: var(--content-gap);
}

.dashboard-panels {
  grid-template-columns: repeat(2, minmax(0, 1fr));
}

.dashboard-stat-card {
  min-height: 120px;
  padding: var(--card-padding);
}

.dashboard-stat-card__title {
  margin-bottom: 12px;
  font-size: var(--font-size-h3);
  line-height: var(--line-height-h3);
  font-weight: 700;
}

.dashboard-stat-card__value {
  margin-bottom: 12px;
  font-size: 24px;
  line-height: 32px;
  font-weight: 700;
}

.dashboard-trend {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  color: var(--color-text-subtle);
}

.dashboard-trend strong {
  font-weight: 700;
}

.dashboard-trend--up i,
.dashboard-trend--up strong {
  color: var(--color-success);
}

.dashboard-trend--down i,
.dashboard-trend--down strong {
  color: var(--color-danger);
}

.dashboard-panel {
  overflow: hidden;
}

.dashboard-panel__header,
.dashboard-table-card {
  padding: var(--card-padding);
}

.dashboard-compact-list {
  padding: 0 var(--card-padding) var(--card-padding);
}

.dashboard-compact-item {
  min-height: 64px;
  display: grid;
  grid-template-columns: minmax(0, 1fr) 160px;
  align-items: center;
  gap: 16px;
  border-top: 1px solid var(--color-border);
}

.dashboard-compact-item:first-child {
  border-top: 0;
}

.dashboard-agent {
  display: flex;
  align-items: center;
  gap: 12px;
  min-width: 0;
}

.dashboard-avatar {
  width: 32px;
  height: 32px;
  border-radius: 50%;
  display: grid;
  place-items: center;
  flex: 0 0 32px;
  color: #ffffff;
  font-size: 12px;
  font-weight: 700;
}

.dashboard-avatar--blue {
  background: linear-gradient(135deg, #67e8f9, #2563eb);
}

.dashboard-avatar--orange {
  background: linear-gradient(135deg, #fb923c, #7c3aed);
}

.dashboard-avatar--purple {
  background: linear-gradient(135deg, #a78bfa, #ec4899);
}

.dashboard-avatar--green {
  background: linear-gradient(135deg, #86efac, #10b981);
}

.dashboard-agent__text {
  min-width: 0;
}

.dashboard-agent__name {
  display: flex;
  align-items: center;
  gap: 4px;
  min-width: 0;
  font-weight: 600;
}

.dashboard-agent__name-text,
.dashboard-agent__description {
  overflow: hidden;
  white-space: nowrap;
  text-overflow: ellipsis;
}

.dashboard-agent__description,
.dashboard-compact-item__label,
.dashboard-table__label,
.dashboard-empty-state {
  color: var(--color-text-subtle);
}

.dashboard-verified {
  width: 12px;
  height: 12px;
  border-radius: 50%;
  display: inline-grid;
  place-items: center;
  background: var(--color-brand);
  color: #ffffff;
  font-size: 8px;
  line-height: 1;
  flex: 0 0 auto;
}

.dashboard-compact-item__metric {
  text-align: right;
}

.dashboard-compact-item__value,
.dashboard-table__value {
  font-size: 16px;
  line-height: 20px;
  font-weight: 700;
}

.dashboard-empty-state {
  min-height: 216px;
  display: grid;
  place-items: center;
  padding: 24px;
  text-align: center;
}

.dashboard-empty-state__illustration {
  position: relative;
  width: 120px;
  height: 64px;
  margin: 0 auto 8px;
  border-radius: 8px;
  background: linear-gradient(135deg, #dff3ff, #cce6ff);
}

.dashboard-empty-state__illustration::before {
  content: "";
  position: absolute;
  left: -10px;
  top: 20px;
  width: 36px;
  height: 32px;
  border-radius: 6px;
  background: #3563ff;
  box-shadow: 0 6px 14px rgba(53, 99, 255, 0.28);
}

.dashboard-empty-state__illustration::after {
  content: "";
  position: absolute;
  right: -6px;
  bottom: 10px;
  width: 16px;
  height: 16px;
  border-radius: 4px;
  background: #3563ff;
}

.dashboard-tabs {
  display: inline-flex;
  align-items: center;
  gap: 4px;
  padding: 3px;
  border-radius: var(--radius-sm);
  background: #f1f3f7;
}

.dashboard-tab {
  height: 26px;
  border: 0;
  border-radius: 6px;
  padding: 0 12px;
  background: transparent;
  color: var(--color-text-subtle);
  font: inherit;
  font-weight: 500;
}

.dashboard-tab--active {
  background: var(--color-surface);
  color: var(--color-text);
  box-shadow: 0 1px 2px rgba(15, 23, 42, 0.08);
}

.dashboard-table-card {
  display: flex;
  flex-direction: column;
  gap: 12px;
  height: clamp(420px, 58vh, 620px);
  min-height: 0;
}

.dashboard-table-wrap {
  flex: 1;
  min-height: 0;
  overflow: auto;
}

.dashboard-table {
  width: 100%;
  min-width: 780px;
  border-collapse: collapse;
  table-layout: fixed;
}

.dashboard-table thead tr {
  height: var(--table-header-height);
  border-bottom: 1px solid var(--color-border);
}

.dashboard-table tbody tr {
  height: 72px;
  border-bottom: 1px solid var(--color-border);
}

.dashboard-table tbody tr:hover {
  background: var(--color-brand-soft);
}

.dashboard-table th,
.dashboard-table td {
  padding: 0 8px;
  text-align: left;
  vertical-align: middle;
}

.dashboard-table th {
  color: var(--color-text-subtle);
  font-weight: 500;
}

.dashboard-table th:first-child,
.dashboard-table td:first-child {
  padding-left: 0;
}

.dashboard-table th:last-child,
.dashboard-table td:last-child {
  padding-right: 0;
}

.dashboard-table__agent-col {
  width: 36%;
}

.dashboard-table__cost-col,
.dashboard-table__action-col {
  text-align: right;
}

.dashboard-table__cost-col {
  width: 14%;
}

.dashboard-table__action-col {
  width: 110px;
}

.dashboard-detail-link {
  color: var(--color-brand);
  text-decoration: none;
  font-weight: 500;
}

.dashboard-detail-link:hover {
  text-decoration: underline;
}

@media (max-width: 1100px) {
  .dashboard-stats,
  .dashboard-panels {
    grid-template-columns: 1fr;
  }
}

@media (max-width: 880px) {
  .dashboard-page__header,
  .dashboard-table-card__header,
  .dashboard-panel__header,
  .dashboard-table-toolbar {
    align-items: flex-start;
    flex-direction: column;
  }

  .dashboard-page__toolbar,
  .dashboard-panel__actions,
  .dashboard-table-toolbar__left,
  .dashboard-table-toolbar__right,
  .dashboard-table-card__title-wrap {
    width: 100%;
    flex-wrap: wrap;
  }

  .dashboard-toast-controls {
    width: 100%;
  }

  .dashboard-search {
    width: 100%;
  }
}
</style>
