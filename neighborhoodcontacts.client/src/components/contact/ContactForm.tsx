import { useEffect, useState } from "react";
import type { ChangeEvent } from "react";

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

type Property = { id: string; address: string; propertyGroupId: string; propertyGroupName?: string };
type PropertyGroup = { id: string; name: string };

type Props = {
    contact: ContactDetails;
    canEdit: boolean; // permission to edit (owner or admin)
    isAdmin?: boolean; // whether current user is admin (to show admin-only fields)
    onSave: (updated: ContactDetails) => Promise<void>;
};

export default function ContactForm({ contact, canEdit, isAdmin, onSave }: Props) {
    const [form, setForm] = useState<ContactDetails>(contact);
    const [editing, setEditing] = useState(false);
    const [saving, setSaving] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const [success, setSuccess] = useState<string | null>(null);

    const [properties, setProperties] = useState<Property[]>([]);
    const [groups, setGroups] = useState<PropertyGroup[]>([]);
    const [loadingProps, setLoadingProps] = useState(true);
    const [propsError, setPropsError] = useState<string | null>(null);

    // UI state: selected property group for filtering property dropdown
    const [selectedGroupId, setSelectedGroupId] = useState<string>("");

    useEffect(() => setForm(contact), [contact]);

    // Load properties and groups
    useEffect(() => {
        let mounted = true;
        (async () => {
            setLoadingProps(true);
            setPropsError(null);
            try {
                const [propsRes, groupsRes] = await Promise.all([
                    fetch("/api/admin/properties", { credentials: "include" }),
                    fetch("/api/admin/property-groups", { credentials: "include" })
                ]);
                if (!propsRes.ok) throw new Error(`Failed to load properties (${propsRes.status})`);
                if (!groupsRes.ok) throw new Error(`Failed to load property groups (${groupsRes.status})`);
                const propsList: Property[] = await propsRes.json();
                const groupsList: PropertyGroup[] = await groupsRes.json();
                if (!mounted) return;
                setProperties(propsList);
                setGroups(groupsList);

                // If contact already has a property, set the group filter to that property's group
                const initialProp = propsList.find((p) => p.id === contact.propertyId);
                setSelectedGroupId(initialProp?.propertyGroupId ?? "");
            } catch (err) {
                if (!mounted) return;
                setPropsError(err instanceof Error ? err.message : String(err));
            } finally {
                if (mounted) setLoadingProps(false);
            }
        })();
        return () => {
            mounted = false;
        };
    }, [contact.propertyId]);

    // Keep selectedGroupId in sync when property changes externally
    useEffect(() => {
        const p = properties.find((x) => x.id === form.propertyId);
        setSelectedGroupId(p?.propertyGroupId ?? "");
    }, [properties, form.propertyId]);

    // Generic update handler for text inputs and checkboxes
    const update = (e: ChangeEvent<HTMLInputElement | HTMLTextAreaElement>) => {
        const target = e.target;
        const name = (target as HTMLInputElement | HTMLTextAreaElement).name;
        if (target instanceof HTMLInputElement) {
            if (target.type === "checkbox") {
                setForm((s) => ({ ...s, [name]: target.checked }));
                return;
            }
            setForm((s) => ({ ...s, [name]: target.value }));
            return;
        }
        setForm((s) => ({ ...s, [name]: (target as HTMLTextAreaElement).value }));
    };

    const onPropertyChange = (e: ChangeEvent<HTMLSelectElement>) => {
        const pid = e.target.value;
        const p = properties.find((x) => x.id === pid);
        setForm((s) => ({
            ...s,
            propertyId: pid || undefined,
            propertyAddress: p ? p.address : undefined
        } as ContactDetails));
    };

    const onGroupChange = (e: ChangeEvent<HTMLSelectElement>) => {
        const gid = e.target.value;
        setSelectedGroupId(gid);

        // If no group selected, clear property selection (property dropdown should only show properties for the selected group)
        if (!gid) {
            setForm((s) => ({ ...s, propertyId: undefined, propertyAddress: undefined } as ContactDetails));
            return;
        }

        // If the current property is not in the newly selected group, clear it
        const currentProp = properties.find((p) => p.id === form.propertyId);
        if (currentProp && currentProp.propertyGroupId !== gid) {
            setForm((s) => ({ ...s, propertyId: undefined, propertyAddress: undefined } as ContactDetails));
        }
    };

    const toggleEditing = () => {
        setError(null);
        setSuccess(null);
        setEditing((v) => !v);
        setForm(contact);
        // reset selectedGroupId based on contact property when toggling editing off
        const p = properties.find((x) => x.id === contact.propertyId);
        setSelectedGroupId(p?.propertyGroupId ?? "");
    };

    const handleSave = async () => {
        setSaving(true);
        setError(null);
        setSuccess(null);
        try {
            await onSave(form);
            setSuccess("Saved successfully.");
            setEditing(false);
        } catch (err) {
            setError(err instanceof Error ? err.message : String(err));
        } finally {
            setSaving(false);
        }
    };

    return (
        <section>
            <div className="mb-2 d-flex justify-content-between align-items-start">
                <h3 className="m-0">Details</h3>
                {canEdit && (
                    <div>
                        {!editing && (
                            <button className="btn btn-sm btn-outline-primary" onClick={toggleEditing}>
                                Edit
                            </button>
                        )}
                        {editing && (
                            <>
                                <button className="btn btn-sm btn-primary me-2" onClick={handleSave} disabled={saving}>
                                    Save
                                </button>
                                <button className="btn btn-sm btn-outline-secondary" onClick={toggleEditing} disabled={saving}>
                                    Cancel
                                </button>
                            </>
                        )}
                    </div>
                )}
            </div>

            {error && <div className="text-danger mb-2">Error: {error}</div>}
            {success && <div className="text-success mb-2">{success}</div>}

            <div className="row">
                <div className="mb-3 col-12 col-md-6">
                    <label className="form-label" htmlFor={`contactName-${contact.id}`}>Name</label>
                    <input id={`contactName-${contact.id}`} name="contactName" value={form.contactName ?? ""} onChange={update} className="form-control" readOnly={!editing} />
                </div>

                <div className="mb-3 col-12 col-md-6">
                    <label className="form-label" htmlFor={`contactEmail-${contact.id}`}>Email</label>
                    <input id={`contactEmail-${contact.id}`} name="contactEmail" value={form.contactEmail ?? ""} onChange={update} className="form-control" readOnly={!editing} />
                </div>

                <div className="mb-3 col-12 col-md-6">
                    <label className="form-label" htmlFor={`contactNumber-${contact.id}`}>Number</label>
                    <input id={`contactNumber-${contact.id}`} name="contactNumber" value={form.contactNumber ?? ""} onChange={update} className="form-control" readOnly={!editing} />
                </div>

                <div className="mb-3 col-12">
                    <label className="form-label" htmlFor={`aboutMe-${contact.id}`}>About Me</label>
                    <textarea id={`aboutMe-${contact.id}`} name="aboutMe" value={form.aboutMe ?? ""} onChange={update} className="form-control" rows={4} readOnly={!editing} />
                </div>

                {/* Property group dropdown */}
                <div className="mb-3 col-12 col-md-6">
                    <label className="form-label" htmlFor={`propertyGroup-${contact.id}`}>Property Group</label>
                    {loadingProps ? (
                        <select id={`propertyGroup-${contact.id}`} className="form-select" disabled>
                            <option>Loading…</option>
                        </select>
                    ) : propsError ? (
                        <div className="text-danger">{propsError}</div>
                    ) : (
                        <select
                            id={`propertyGroup-${contact.id}`}
                            className="form-select"
                            value={selectedGroupId ?? ""}
                            onChange={onGroupChange}
                            disabled={!editing}
                        >
                            <option value="">All groups</option>
                            {groups.map((g) => (
                                <option key={g.id} value={g.id}>
                                    {g.name}
                                </option>
                            ))}
                        </select>
                    )}
                </div>

                {/* only show properties for the selected group */}
                <div className="mb-3 col-12 col-md-6">
                    <label className="form-label" htmlFor={`property-${contact.id}`}>Property</label>
                    {loadingProps ? (
                        <select id={`property-${contact.id}`} className="form-select" disabled>
                            <option>Loading…</option>
                        </select>
                    ) : propsError ? (
                        <div className="text-danger">{propsError}</div>
                    ) : (
                        <select
                            id={`property-${contact.id}`}
                            className="form-select"
                            value={form.propertyId ?? ""}
                            onChange={onPropertyChange}
                            disabled={!editing}
                        >
                            {/* When no group is selected, instruct user to pick a group first */}
                            {!selectedGroupId ? (
                                <option value="">Select a property group</option>
                            ) : (
                                <>
                                    <option value="">No property</option>
                                    {properties
                                        .filter((p) => p.propertyGroupId === selectedGroupId)
                                        .map((p) => (
                                            <option key={p.id} value={p.id}>
                                                {p.address}
                                            </option>
                                        ))}
                                </>
                            )}
                        </select>
                    )}
                </div>

                {isAdmin && (
                    <div className="mb-3 col-12 d-flex gap-3">
                        <div className="form-check">
                            <input id={`isActive-${contact.id}`} className="form-check-input" type="checkbox" name="isActive" checked={!!form.isActive} onChange={(e) => setForm((s) => ({ ...s, isActive: (e.target as HTMLInputElement).checked }))} disabled={!editing} />
                            <label className="form-check-label" htmlFor={`isActive-${contact.id}`}>Active</label>
                        </div>
                        <div className="form-check">
                            <input id={`isVisible-${contact.id}`} className="form-check-input" type="checkbox" name="isVisible" checked={!!form.isVisible} onChange={(e) => setForm((s) => ({ ...s, isVisible: (e.target as HTMLInputElement).checked }))} disabled={!editing} />
                            <label className="form-check-label" htmlFor={`isVisible-${contact.id}`}>Visible</label>
                        </div>
                        <div className="form-check">
                            <input id={`isAdmin-${contact.id}`} className="form-check-input" type="checkbox" name="isAdmin" checked={!!form.isAdmin} onChange={(e) => setForm((s) => ({ ...s, isAdmin: (e.target as HTMLInputElement).checked }))} disabled={!editing} />
                            <label className="form-check-label" htmlFor={`isAdmin-${contact.id}`}>Admin</label>
                        </div>
                    </div>
                )}
            </div>
        </section>
    );
}
