import React, { useEffect, useState } from "react";

// Property Group.
// - Dropdown to select property group (shows the name of the group).
//  The first item in the drop down is "All contacts" which shows all contacts and properties (will not include a property group in the endpoint call).
//  Selecting a property group will show the contacts for that group and the properties in that group.
// - Edit property group button. Edit the currently selected property group.
// - Delete property group button. Deletes the currently selected property group, and all properties and contacts in that group. Give clear warning.
// - Add property group button. Opens a text entry field to enter the name of the new property group, and a confirm button to create it. Also has a cancel button.

type PropertyGroup = { id: string; name: string };

type Props = {
  onChange?: (groupId: string | null) => void;
};

const PropertyGroupControl: React.FC<Props> = ({ onChange }) => {
  const [groups, setGroups] = useState<PropertyGroup[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [selectedId, setSelectedId] = useState<string>("");
  const [adding, setAdding] = useState(false);
  const [newName, setNewName] = useState("");
  const [editing, setEditing] = useState(false);
  const [editName, setEditName] = useState("");

  useEffect(() => {
    let mounted = true;
    (async () => {
      setLoading(true);
      setError(null);
      try {
        const res = await fetch("/api/admin/property-groups", { credentials: "include" });
        if (!res.ok) throw new Error(`Failed to load property groups (${res.status})`);
        const list: PropertyGroup[] = await res.json();
        if (!mounted) return;
        setGroups(list);
      } catch (err) {
        if (mounted) setError(err instanceof Error ? err.message : String(err));
      } finally {
        if (mounted) setLoading(false);
      }
    })();
    return () => { mounted = false; };
  }, []);

  useEffect(() => {
    const payload = selectedId === "" ? null : selectedId;
    onChange?.(payload);
    window.dispatchEvent(new CustomEvent("admin:propertyGroupChanged", { detail: { propertyGroupId: payload } }));
  }, [selectedId, onChange]);

  const refresh = async () => {
    setLoading(true);
    setError(null);
    try {
      const res = await fetch("/api/admin/property-groups", { credentials: "include" });
      if (!res.ok) throw new Error(`Failed to load property groups (${res.status})`);
      const list: PropertyGroup[] = await res.json();
      setGroups(list);
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setLoading(false);
    }
  };

  const handleAdd = async () => {
    if (!newName.trim()) return setError("Name is required.");
    setError(null);
    try {
      const res = await fetch("/api/admin/property-groups", {
        method: "POST",
        credentials: "include",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ name: newName.trim() }),
      });
      if (!res.ok) {
        const body = await res.json().catch(() => null);
        throw new Error(body?.error ?? `Create failed (${res.status})`);
      }
      await refresh();
      setNewName("");
      setAdding(false);
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    }
  };

  const startEdit = () => {
    if (!selectedId) return;
    const g = groups.find((x) => x.id === selectedId);
    if (!g) return;
    setEditName(g.name);
    setEditing(true);
  };

  const confirmEdit = async () => {
    if (!selectedId) return;
    if (!editName.trim()) return setError("Name is required.");
    setError(null);
    try {
      const res = await fetch(`/api/admin/property-groups/${selectedId}`, {
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
    const group = groups.find((g) => g.id === selectedId);
    const name = group?.name ?? "this group";
    if (!window.confirm(`Delete property group "${name}"? This will fail if the group has properties. This operation is permanent.`)) return;
    setError(null);
    try {
      const res = await fetch(`/api/admin/property-groups/${selectedId}`, {
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
      <label className="form-label mb-0 me-2">Property group</label>

      {loading ? (
        <div>Loading groups…</div>
      ) : error ? (
        <div className="text-danger">{error}</div>
      ) : (
        <select className="form-select form-select-sm w-auto" value={selectedId} onChange={(e) => setSelectedId(e.target.value)}>
          <option value="">All property groups</option>
          {groups.map((g) => <option key={g.id} value={g.id}>{g.name}</option>)}
        </select>
      )}

      <div className="ms-2">
        <button className="btn btn-sm btn-outline-secondary me-1" onClick={startEdit} disabled={!selectedId || editing || adding}>Edit</button>
        <button className="btn btn-sm btn-outline-danger me-1" onClick={handleDelete} disabled={!selectedId || editing || adding}>Delete</button>
        <button className="btn btn-sm btn-primary" onClick={() => { setAdding((a) => !a); setEditing(false); setError(null); setNewName(""); }}>
          {adding ? "Cancel" : "Add"}
        </button>
      </div>

      {adding && (
        <div className="mt-2 d-flex gap-2 align-items-center">
          <input className="form-control form-control-sm w-auto" placeholder="New group name" value={newName} onChange={(e) => setNewName(e.target.value)} />
          <button className="btn btn-sm btn-success" onClick={handleAdd}>Confirm</button>
          <button className="btn btn-sm btn-outline-secondary" onClick={() => { setAdding(false); setNewName(""); setError(null); }}>Cancel</button>
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

export default PropertyGroupControl;