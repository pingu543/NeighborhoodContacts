// Displays contact details for a specific contact.
// This page is accessed by clicking on a contact in the ContactListPage or from AccountComponent.
// If the contact belongs to the user or if the user is an admin, the user can edit the contact details.
// These are the editable details:
// - Contact name
// - Contact email
// - Contact number
// - About me
// - New password
// If the contact does not belong to the user, the user can only view the contact details.
// The owner/admin can edit password (enter current password, enter new password, enter new password again).
// Admin will also have a delete button.
// Admin can see and edit Property, IsActive, IsVisible, IsAdmin.

function ContactDetailsPage() {
  return (
    <p>Hello world!</p>
  );
}

export default ContactDetailsPage;