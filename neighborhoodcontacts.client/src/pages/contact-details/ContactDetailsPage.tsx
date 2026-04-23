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


import { useEffect, useState } from "react";
import { useParams } from "react-router-dom";
import { useAuth } from "../../context/AuthContext";
import SetNewPassword from "../../components/set-new-password/SetNewPasswordComponent";
import ContactForm from "../../components/contact/ContactForm";

type ContactDetails = {
  id: string;
  username?: string;
  contactName: string;
  contactEmail?: string;
  contactNumber?: string;
  aboutMe?: string;
  propertyId?: string;
  propertyAddress?: string;
  isActive?: boolean;
  isVisible?: boolean;
  isAdmin?: boolean;
};

function ContactDetailsPage() {
  const { id } = useParams<{ id: string }>();
  const [contact, setContact] = useState<ContactDetails | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const { user, isSignedIn, refresh } = useAuth();
  const [passwordSuccess, setPasswordSuccess] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;

    // Use async to not get ESLint'ed.
    const load = async () => {
      if (!id) return;
      setContact(null);
      setError(null);
      setLoading(true);
      try {
        const res = await fetch(`/api/users/${id}`, { credentials: "include" });
        if (!res.ok) throw new Error("Failed to load contact details");
        const data: ContactDetails = await res.json();
        if (!cancelled) {
          setContact(data);
        }
      } catch (err) {
        if (!cancelled) setError((err as Error).message);
      } finally {
        if (!cancelled) setLoading(false);
      }
    };

    load();
    return () => {
      cancelled = true;
    };
  }, [id]);

  if (loading) return <div>Loading contact details…</div>;
  if (error) return <div className="text-danger">Error: {error}</div>;
  if (!contact) return <div>Contact not found.</div>;

  return (
    <div className="container py-3">
      <h2>Contact Details</h2>
      <ContactForm
        contact={contact}
        canEdit={!!isSignedIn && !!user?.id && (user.id === contact.id || !!user?.isAdmin)}
        isAdmin={!!user?.isAdmin}
        onSave={async (updated) => {
          try {
            // Use /api/users/me for the signed-in owner; admins should use the admin contact API
            const url = user?.id === contact.id ? `/api/users/me` : `/api/admin/contacts/${contact.id}`;
            const res = await fetch(url, {
              method: "PUT",
              credentials: "include",
              headers: { "Content-Type": "application/json" },
              body: JSON.stringify(updated),
            });

            if (!res.ok) {
              let payload: { error?: string; message?: string } = {};
              try {
                payload = await res.json();
              } catch (error) {
                console.error("Failed to parse error response:", error);
              }
              throw new Error(payload.error || payload.message || res.statusText || "Failed to save contact:");
            }

            // Many update endpoints return 204 No Content — avoid calling res.json() on empty responses
            if (res.status === 204) {
              setContact((c) => ({ ...(c as ContactDetails), ...updated }));
            } else {
              const data = (await res.json()) as typeof contact;
              setContact(data);
            }
          } catch (err) {
            throw err instanceof Error ? err : new Error(String(err));
          }
        }}
      />
      {isSignedIn && user?.id && user.id === contact.id && (
        <section className="mt-4">
          <h3>Change password</h3>
          {passwordSuccess && <div className="text-success mb-2">{passwordSuccess}</div>}
          <SetNewPassword
            requireCurrentPassword={true}
            onSubmit={async ({ currentPassword, newPassword }) => {
              setPasswordSuccess(null);
              try {
                const res = await fetch(`/api/users/me/password`, {
                  method: "PUT",
                  credentials: "include",
                  headers: { "Content-Type": "application/json" },
                  body: JSON.stringify({ CurrentPassword: currentPassword ?? "", NewPassword: newPassword }),
                });
                if (!res.ok) {
                  type ErrorPayload = { error?: string; message?: string };
                  let payload: ErrorPayload = {};
                  try {
                    payload = (await res.json()) as ErrorPayload;
                  } catch {
                    payload = {};
                  }
                  throw new Error(payload.error || payload.message || res.statusText || "Failed to change password");
                }
                setPasswordSuccess("Password changed successfully.");
                await refresh();
              } catch (err) {
                throw err instanceof Error ? err : new Error(String(err));
              }
            }}
          />
        </section>
      )}
    </div>
  );
}

export default ContactDetailsPage;