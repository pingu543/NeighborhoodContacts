import React, { useEffect, useState } from "react";

// Property Group.
// - Dropdown to select property group (shows the name of the group).
//  The first item in the drop down is "All contacts" which shows all contacts and properties (will not include a property group in the endpoint call).
//  Selecting a property group will show the contacts for that group and the properties in that group.
// - Edit property group button. Edit the currently selected property group.
// - Delete property group button. Deletes the currently selected property group, and all properties and contacts in that group. Give clear warning.
// - Add property group button. Opens a text entry field to enter the name of the new property group, and a confirm button to create it. Also has a cancel button.


type Contact = {
    id: string;
    username: string
    contactName: string,
    contactNumber: string,
    contactEmail: string,
    propertyAddress: string,
    isActive: boolean,
    isVisible: boolean
};

type Property = {
  id: string;
  address: string;
  // add other fields if needed (display name, owner, etc.)
};

type Props = {
  onChange?: (groupId: string | null) => void;
};

const ContactControl: React.FC<Props> = ({ onChange }) => {
    const [contacts, setContacts] = useState<Contact[]>([]);
    const [properties, setProperties] = useState<Property[]>([]);
    const [loading, setLoading] = useState(true);
    const [propertiesLoading, setPropertiesLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);
    const [selectedId, setSelectedId] = useState<string>("");
    const [adding, setAdding] = useState(false);
    const [newContact, setNewContact] = useState<Contact>();
    const [newContactName, setNewContactName] = useState<string>("");
    const [newUsername, setNewUsername] = useState<string>("");
    const [newContactNumber, setNewContactNumber] = useState<string>("");
    const [newContactEmail, setNewContactEmail] = useState<string>("");
    const [newPropertyId, setNewPropertyId] = useState<string>("");
    const [newIsActive, setNewIsActive] = useState(true);
    const [newIsVisible, setNewIsVisible] = useState(true);
    const [editing, setEditing] = useState(false);
    const [editName, setEditName] = useState("");
    const [newPassword, setNewPassword] = useState<string>("");

  useEffect(() => {
    let mounted = true;
    (async () => {
      setLoading(true);
      setError(null);
      try {
        const res = await fetch("/api/admin/contacts", { credentials: "include" });
        if (!res.ok) throw new Error(`Failed to load contacts (${res.status})`);
        const list: Contact[] = await res.json();
        if (!mounted) return;
        setContacts(list);
      } catch (err) {
        if (mounted) setError(err instanceof Error ? err.message : String(err));
      } finally {
        if (mounted) setLoading(false);
      }
    })();
    return () => { mounted = false; };
  }, []);

  useEffect(() => {
    let mounted = true;
    (async () => {
      setPropertiesLoading(true);
      try {
        const res = await fetch("/api/admin/properties", { credentials: "include" });
        if (!res.ok) throw new Error(`Failed to load properties (${res.status})`);
        const list: Property[] = await res.json();
        if (!mounted) return;
        setProperties(list);
      } catch (err) {
        if (mounted) setError(err instanceof Error ? err.message : String(err));
      } finally {
        if (mounted) setPropertiesLoading(false);
      }
    })();
    return () => { mounted = false; };
  }, []);

  useEffect(() => {
    const payload = selectedId === "" ? null : selectedId;
    onChange?.(payload);
    window.dispatchEvent(new CustomEvent("admin:Contact Changed", { detail: { propertyGroupId: payload } }));
  }, [selectedId, onChange]);

  const refresh = async () => {
    setLoading(true);
    setError(null);
    try {
      const res = await fetch("/api/admin/contacts", { credentials: "include" });
      if (!res.ok) throw new Error(`Failed to load contacts (${res.status})`);
        const list: Contact[] = await res.json();
      setContacts(list);
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setLoading(false);
    }
  };

  const refreshProperties = async () => {
    setPropertiesLoading(true);
    try {
      const res = await fetch("/api/admin/properties", { credentials: "include" });
      if (!res.ok) throw new Error(`Failed to load properties (${res.status})`);
      const list: Property[] = await res.json();
      setProperties(list);
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setPropertiesLoading(false);
    }
  };

  const handleAdd = async () => {
    if (!newUsername.trim()) return setError("Username is required.");
    if (!newPassword.trim()) return setError("Password is required.");
    if (!newContactName.trim()) return setError("ContactName is required.");
    if (!newContactNumber.trim()) return setError("ContactNumber is required.");
    if (!newContactEmail.trim()) return setError("ContactEmail is required.");
    if (!newPropertyId.trim()) return setError("Property selection is required.");
    setError(null);

    try {
      const res = await fetch("/api/admin/contacts", {
        method: "POST",
        credentials: "include",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          username: newUsername.trim(),
          password: newPassword.trim(),
          contactName: newContactName.trim(),
          contactNumber: newContactNumber.trim(),
          contactEmail: newContactEmail.trim(),
          propertyId: newPropertyId.trim()
        }),
      });
      if (!res.ok) {
        const body = await res.json().catch(() => null);
        throw new Error(body?.error ?? `Create failed (${res.status})`);
      }
      await refresh();
      await refreshProperties();
      setNewUsername("");
      setNewPassword("");
      setNewContactName("");
      setNewContactEmail("");
      setNewContactNumber("");
      setNewPropertyId("");
      setAdding(false);
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    }
  };

  const startEdit = () => {
    if (!selectedId) return;
    const g = contacts.find((x) => x.id === selectedId);
    if (!g) return;
    setEditName(g.username);
    setEditing(true);
  };

  const confirmEdit = async () => {
    if (!selectedId) return;
    if (!editName.trim()) return setError("Name is required.");
    setError(null);
    try {
      const res = await fetch(`/api/admin/contacts/${selectedId}`, {
        method: "PUT",
        credentials: "include",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ name: editName.trim() }),
      });
      if (!res.ok) {
        const body = await res.json().catch(() => null);
        throw new Error(body?.error ?? `Update failed (${res.status})`);
      }
      await refresh();
      setEditing(false);
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    }
  };

  const handleDelete = async () => {
    if (!selectedId) return;
    const group = contacts.find((g) => g.id === selectedId);
    const name = group?.username ?? "this group";
    if (!window.confirm(`Delete property group "${name}"? This will fail if the group has properties. This operation is permanent.`)) return;
    setError(null);
    try {
      const res = await fetch(`/api/admin/contacts/${selectedId}`, {
        method: "DELETE",
        credentials: "include",
      });
      if (!res.ok) {
        const body = await res.json().catch(() => null);
        throw new Error(body?.error ?? `Delete failed (${res.status})`);
      }
      setSelectedId("");
      await refresh();
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    }
  };

  return (
    <div className="d-flex align-items-center gap-2">
      <label className="form-label mb-0 me-2">Contact Control </label>

      {loading ? (
        <div>Loading groups…</div>
      ) : error ? (
        <div className="text-danger">{error}</div>
      ) : (
        <select className="form-select form-select-sm w-auto" value={selectedId} onChange={(e) => setSelectedId(e.target.value)}>
          <option value="">All contacts</option>
          {contacts.map((g) => <option key={g.id} value={g.id}>{g.username}</option>)}
        </select>
      )}

      <div className="ms-2">
        <button className="btn btn-sm btn-outline-secondary me-1" onClick={startEdit} disabled={!selectedId || editing || adding}>Edit</button>
        <button className="btn btn-sm btn-outline-danger me-1" onClick={handleDelete} disabled={!selectedId || editing || adding}>Delete</button>
        <button className="btn btn-sm btn-primary" onClick={() => { setAdding((a) => !a); setEditing(false); setError(null); }}>
          {adding ? "Cancel" : "Add"}
        </button>
      </div>

      {adding && (
        <div className="mt-2 d-flex gap-2 align-items-center">
          <input className="form-control form-control-sm w-auto" placeholder="Username" value={newUsername} onChange={(e) => setNewUsername(e.target.value)} />
          <input className="form-control form-control-sm w-auto" placeholder="Password" type="password" value={newPassword} onChange={(e) => setNewPassword(e.target.value)} />
          <input className="form-control form-control-sm w-auto" placeholder="Name" value={newContactName} onChange={(e) => setNewContactName(e.target.value)} />
          <input className="form-control form-control-sm w-auto" placeholder="Phone Number" value={newContactNumber} onChange={(e) => setNewContactNumber(e.target.value)} />
          <input className="form-control form-control-sm w-auto" placeholder="Email" value={newContactEmail} onChange={(e) => setNewContactEmail(e.target.value)} />
          {propertiesLoading ? (
            <select className="form-select form-select-sm w-auto" disabled>
              <option>Loading properties…</option>
            </select>
          ) : (
            <select className="form-select form-select-sm w-auto" value={newPropertyId} onChange={(e) => setNewPropertyId(e.target.value)}>
              <option value="">Select property</option>
              {properties.map(p => <option key={p.id} value={p.id}>{p.address}</option>)}
            </select>
          )}
          <button className="btn btn-sm btn-success" onClick={handleAdd}>Confirm</button>
          <button className="btn btn-sm btn-outline-secondary" onClick={() => {
            setAdding(false);
            setNewUsername("");
            setNewPassword("");
            setNewContactName("");
            setNewContactEmail("");
            setNewContactNumber("");
            setNewPropertyId("");
            setError(null);
          }}>Cancel</button>
        </div>
      )}

      {editing && (
        <div className="mt-2 d-flex gap-2 align-items-center">
          <input className="form-control form-control-sm w-auto" value={editName} onChange={(e) => setEditName(e.target.value)} />
          <button className="btn btn-sm btn-success" onClick={confirmEdit}>Confirm</button>
          <button className="btn btn-sm btn-outline-secondary" onClick={() => { setEditing(false); setEditName(""); setError(null); }}>Cancel</button>
        </div>
      )}
    </div>
  );
};

export default ContactControl;