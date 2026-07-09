import { ref } from 'vue';

type AsyncHandler = () => Promise<void>;
type SyncHandler = () => void;

// Shared editor state giữa page detail và layout/footer/header.
const isEditing = ref(false);
const isSaving = ref(false);

let beginEditHandler: SyncHandler = () => {};
let cancelEditHandler: SyncHandler = () => {};
let saveDraftHandler: AsyncHandler = async () => {};
let saveAndPublishHandler: AsyncHandler = async () => {};
let toggleActivationHandler: AsyncHandler = async () => {};

// Cung cấp một command bus rất mỏng để layout có thể kích hoạt hành động của detail page hiện tại.
export function useAgentDetailEditor() {
  // Đăng ký tập handler do page detail hiện tại sở hữu.
  function registerHandlers(handlers: {
    beginEdit: SyncHandler;
    cancelEdit: SyncHandler;
    saveDraft: AsyncHandler;
    saveAndPublish: AsyncHandler;
    toggleActivation: AsyncHandler;
  }) {
    beginEditHandler = handlers.beginEdit;
    cancelEditHandler = handlers.cancelEdit;
    saveDraftHandler = handlers.saveDraft;
    saveAndPublishHandler = handlers.saveAndPublish;
    toggleActivationHandler = handlers.toggleActivation;
  }

  // Dọn handler khi rời trang để tránh layout gọi nhầm logic của page cũ.
  function clearHandlers() {
    beginEditHandler = () => {};
    cancelEditHandler = () => {};
    saveDraftHandler = async () => {};
    saveAndPublishHandler = async () => {};
    toggleActivationHandler = async () => {};
    isEditing.value = false;
    isSaving.value = false;
  }

  // Các hàm dưới đây chỉ delegate sang page đã đăng ký handler tương ứng.
  function beginEdit() {
    beginEditHandler();
  }

  function cancelEdit() {
    cancelEditHandler();
  }

  async function saveDraft() {
    await saveDraftHandler();
  }

  async function saveAndPublish() {
    await saveAndPublishHandler();
  }

  async function toggleActivation() {
    await toggleActivationHandler();
  }

  return {
    isEditing,
    isSaving,
    registerHandlers,
    clearHandlers,
    beginEdit,
    cancelEdit,
    saveDraft,
    saveAndPublish,
    toggleActivation
  };
}
