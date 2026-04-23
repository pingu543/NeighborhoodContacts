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

import { useState } from "react";
import { useNavigate } from "react-router-dom";
import ContactsList from "../../components/contacts/ContactsList";
import AdminControl from "../../components/admin-control/AdminControlComponent";
import { useAuth } from "../../context/AuthContext";
import ContactDetails from "../../components/contacts/ContactDetails";


function ContactDetailsPage() {
    const { isSignedIn, isAdmin } = useAuth();
    const [error, setError] = useState<string | null>(null);
    const navigate = useNavigate();

  return (
      <div>
          {isSignedIn === null ? (
              <div>Checking sign-in…</div>
          ) : isSignedIn === false ? (
              <div className="mb-3">Please sign in to view this contact.</div>
          ) : ( 
                      <div>
                        <ContactDetails></ContactDetails>
                      </div>
          )}
      </div>
  );
}

export default ContactDetailsPage;