import PropertyGroupControl from "./PropertyGroupControl";
import PropertyControl from "./PropertyControl";

// A long bar with buttons:
// Property Group.
// Add contact. When clicked, it opens a form with the following fields:
// - Email (text input as username)
// - Address (dropdown)
// - Confirm button. (shows the auto generated password with copy button).
// - Cancel button
// Add property.
// - Text field for full address.
// - Confirm button
// - Cancel button
// Edit property.
// - Dropdown to select property to edit.
// - Dropdown to select new property group (prefilled with the current property group).
// - Text field for full address (prefilled with the current address).
// - Confirm button
// - Delete button
// - Cancel button

function AdminControl() {
    const handleGroupChange = (groupId: string | null) => {
        // Optionally handle selection at this level; other parts of the app also get the global event.
        console.log("Selected property group:", groupId);
    };

    return (
        <div className="mb-3 p-3 border rounded bg-light">
            <PropertyGroupControl onChange={handleGroupChange} />
            <PropertyControl />

            {/* TODO: add other admin controls (Add contact, Add/Edit property, etc.) here */}
            <div className="mt-3">
                <button className="btn btn-sm btn-outline-secondary me-2" disabled>
                    Contacts (TODO)
                </button>
            </div>
        </div>
    );
}

export default AdminControl;