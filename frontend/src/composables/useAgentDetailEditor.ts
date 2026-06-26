import { ref } from 'vue';

type AsyncHandler = () => Promise<void>;
type SyncHandler = () => void;

const isEditing = ref(false);
const isSaving = ref(false);

let beginEditHandler: SyncHandler = () => {};
let cancelEditHandler: SyncHandler = () => {};
let saveDraftHandler: AsyncHandler = async () => {};
let saveAndPublishHandler: AsyncHandler = async () => {};
let toggleActivationHandler: AsyncHandler = async () => {};

export function useAgentDetailEditor() {
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

  function clearHandlers() {
    beginEditHandler = () => {};
    cancelEditHandler = () => {};
    saveDraftHandler = async () => {};
    saveAndPublishHandler = async () => {};
    toggleActivationHandler = async () => {};
    isEditing.value = false;
    isSaving.value = false;
  }

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
