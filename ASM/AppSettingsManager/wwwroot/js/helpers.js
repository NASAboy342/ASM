/**
 * JavaScript helpers for Blazor interactions
 */

/**
 * Show the edit directory modal
 */
function showEditModal() {
    const modal = document.getElementById('editDirectoryModal');
    if (modal) {
        modal.style.display = 'block';
        modal.classList.add('show');
    }
}

/**
 * Hide the edit directory modal
 */
function hideEditModal() {
    const modal = document.getElementById('editDirectoryModal');
    if (modal) {
        modal.style.display = '';
        modal.classList.remove('show');
    }
}

/**
 * Focus the delete confirmation input field
 */
function focusDeleteConfirmInput() {
    const input = document.getElementById('deleteConfirmInput');
    if (input) {
        input.focus();
    }
}

/**
 * Show the delete confirmation modal
 */
function showDeleteModal() {
    const modal = document.getElementById('deleteConfirmModal');
    if (modal) {
        modal.style.display = 'block';
        modal.classList.add('show');
    }
}

/**
 * Hide the delete confirmation modal
 */
function hideDeleteModal() {
    const modal = document.getElementById('deleteConfirmModal');
    if (modal) {
        modal.style.display = '';
        modal.classList.remove('show');
    }
}

export default { showEditModal, hideEditModal, focusDeleteConfirmInput, showDeleteModal, hideDeleteModal };