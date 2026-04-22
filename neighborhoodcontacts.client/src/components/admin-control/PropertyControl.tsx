import React, { useEffect, useState, useCallback } from "react";

type Property = { id: string; address: string; propertyGroupId: string; propertyGroupName?: string };
type PropertyGroup = { id: string; name: string };

const PropertyControl: React.FC = () => {
    const [properties, setProperties] = useState<Property[]>([]);
    const [groups, setGroups] = useState<PropertyGroup[]>([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);

    const [selectedId, setSelectedId] = useState<string>("");
    const [adding, setAdding] = useState(false);
    const [editing, setEditing] = useState(false);

    const [newAddress, setNewAddress] = useState("");
    const [newGroupId, setNewGroupId] = useState<string>("");

    const [editAddress, setEditAddress] = useState("");
    const [editGroupId, setEditGroupId] = useState<string>("");

    const [currentFilterGroup, setCurrentFilterGroup] = useState<string | null>(null);

    useEffect(() => {
        // listen for property group changes from PropertyGroupControl
        const handler = (e: Event) => {
            const ce = e as CustomEvent<{ propertyGroupId?: string | null }>;
            const id = ce?.detail?.propertyGroupId ?? null;
            setCurrentFilterGroup(id);
        };
        window.addEventListener("admin:propertyGroupChanged", handler);
        return () => window.removeEventListener("admin:propertyGroupChanged", handler);
    }, []);

    // Wrap refreshAll in useCallback to satisfy ESLint
    const refreshAll = useCallback(async () => {
        setLoading(true);
        setError(null);
        try {
            const q = currentFilterGroup ? `?propertyGroupId=${encodeURIComponent(currentFilterGroup)}` : "";
            const [propsRes, groupsRes] = await Promise.all([
                fetch(`/api/admin/properties${q}`, { credentials: "include" }),
                fetch("/api/admin/property-groups", { credentials: "include" })
            ]);

            if (!propsRes.ok) throw new Error(`Failed to load properties (${propsRes.status})`);
            if (!groupsRes.ok) throw new Error(`Failed to load property groups (${groupsRes.status})`);

            const propsList: Property[] = await propsRes.json();
            const groupsList: PropertyGroup[] = await groupsRes.json();

            setProperties(propsList);
            setGroups(groupsList);

            // If a group filter is active, preselect it for new property
            setNewGroupId(currentFilterGroup ?? (groupsList[0]?.id ?? ""));

            // Reset selections when list changes
            setSelectedId("");
        } catch (err) {
            setError(err instanceof Error ? err.message : String(err));
        } finally {
            setLoading(false);
        }
    }, [currentFilterGroup]);

    // Refresh again whenever the current filter group changes, to show relevant properties
    useEffect(() => {
        refreshAll();
    }, [currentFilterGroup, refreshAll]);

    const handleAdd = async () => {
        if (!newAddress.trim()) return setError("Address is required.");
        if (!newGroupId) return setError("Property group is required.");
        setError(null);
        try {
            const res = await fetch("/api/admin/properties", {
                method: "POST",
                credentials: "include",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({ Address: newAddress.trim(), PropertyGroupId: newGroupId })
            });
            if (!res.ok) {
                const body = await res.json().catch(() => null);
                throw new Error(body?.error ?? `Create failed (${res.status})`);
            }
            setNewAddress("");
            setAdding(false);
            await refreshAll();
        } catch (err) {
            setError(err instanceof Error ? err.message : String(err));
        }
    };

    const startEdit = () => {
        if (!selectedId) return;
        const p = properties.find((x) => x.id === selectedId);
        if (!p) return;
        setEditAddress(p.address);
        setEditGroupId(p.propertyGroupId);
        setEditing(true);
        setAdding(false);
        setError(null);
    };

    const confirmEdit = async () => {
        if (!selectedId) return;
        if (!editAddress.trim()) return setError("Address is required.");
        if (!editGroupId) return setError("Property group is required.");
        setError(null);
        try {
            const res = await fetch(`/api/admin/properties/${selectedId}`, {
                method: "PUT",
                credentials: "include",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({ Address: editAddress.trim(), PropertyGroupId: editGroupId })
            });
            if (!res.ok) {
                const body = await res.json().catch(() => null);
                throw new Error(body?.error ?? `Update failed (${res.status})`);
            }
            setEditing(false);
            await refreshAll();
        } catch (err) {
            setError(err instanceof Error ? err.message : String(err));
        }
    };

    const handleDelete = async () => {
        if (!selectedId) return;
        const p = properties.find((x) => x.id === selectedId);
        const addr = p?.address ?? "this property";
        if (!window.confirm(`Delete property "${addr}"? This will fail if the property has users. This operation is permanent.`)) return;
        setError(null);
        try {
            const res = await fetch(`/api/admin/properties/${selectedId}`, {
                method: "DELETE",
                credentials: "include"
            });
            if (!res.ok) {
                const body = await res.json().catch(() => null);
                throw new Error(body?.error ?? `Delete failed (${res.status})`);
            }
            setSelectedId("");
            await refreshAll();
        } catch (err) {
            setError(err instanceof Error ? err.message : String(err));
        }
    };

    useEffect(() => {
        // when selected property changes, emit event for contacts list to filter by property
        const payload = selectedId === "" ? null : selectedId;
        window.dispatchEvent(new CustomEvent("admin:propertyChanged", { detail: { propertyId: payload } }));
    }, [selectedId]);

    return (
        <div className="d-flex align-items-center gap-2 mt-2">
            <label className="form-label mb-0 me-2">Property</label>

            {loading ? (
                <div>Loading properties…</div>
            ) : error ? (
                <div className="text-danger">{error}</div>
            ) : (
                <select className="form-select form-select-sm w-auto" value={selectedId} onChange={(e) => setSelectedId(e.target.value)}>
                    <option value="">All properties</option>
                    {properties.map((p) => (
                        <option key={p.id} value={p.id}>
                            {p.address}{p.propertyGroupName ? ` — ${p.propertyGroupName}` : ""}
                        </option>
                    ))}
                </select>
            )}

            <div className="ms-2">
                <button className="btn btn-sm btn-outline-secondary me-1" onClick={startEdit} disabled={!selectedId || editing || adding}>Edit</button>
                <button className="btn btn-sm btn-outline-danger me-1" onClick={handleDelete} disabled={!selectedId || editing || adding}>Delete</button>
                <button className="btn btn-sm btn-primary" onClick={() => { setAdding((a) => !a); setEditing(false); setError(null); setNewAddress(""); }} >
                    {adding ? "Cancel" : "Add"}
                </button>
            </div>

            {adding && (
                <div className="mt-2 d-flex gap-2 align-items-center">
                    <input className="form-control form-control-sm w-auto" placeholder="Full address" value={newAddress} onChange={(e) => setNewAddress(e.target.value)} />
                    <select className="form-select form-select-sm w-auto" value={newGroupId} onChange={(e) => setNewGroupId(e.target.value)}>
                        <option value="">Select group</option>
                        {groups.map((g) => <option key={g.id} value={g.id}>{g.name}</option>)}
                    </select>
                    <button className="btn btn-sm btn-success" onClick={handleAdd}>Confirm</button>
                    <button className="btn btn-sm btn-outline-secondary" onClick={() => { setAdding(false); setNewAddress(""); setError(null); }}>Cancel</button>
                </div>
            )}

            {editing && (
                <div className="mt-2 d-flex gap-2 align-items-center">
                    <input className="form-control form-control-sm w-auto" value={editAddress} onChange={(e) => setEditAddress(e.target.value)} />
                    <select className="form-select form-select-sm w-auto" value={editGroupId} onChange={(e) => setEditGroupId(e.target.value)}>
                        <option value="">Select group</option>
                        {groups.map((g) => <option key={g.id} value={g.id}>{g.name}</option>)}
                    </select>
                    <button className="btn btn-sm btn-success" onClick={confirmEdit}>Confirm</button>
                    <button className="btn btn-sm btn-outline-secondary" onClick={() => { setEditing(false); setEditAddress(""); setEditGroupId(""); setError(null); }}>Cancel</button>
                </div>
            )}
        </div>
    );
};

export default PropertyControl;