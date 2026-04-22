import React, { useEffect, useState } from "react";

export type Contact = {
    id: string;
    contactName: string;
    contactEmail?: string;
    contactNumber?: string;
    propertyAddress?: string;
};

type Props = {
    preferAdmin?: boolean;
    pageSize?: number;
    onSelect?: (id: string) => void;
    onError?: (msg: string) => void;
};

// ContactsList component that fetches and displays a paginated list of contacts.
// It can optionally use an admin endpoint for fetching if preferAdmin is true.

const ContactsList: React.FC<Props> = ({ preferAdmin = false, pageSize = 10, onSelect, onError }) => {
    const [contacts, setContacts] = useState<Contact[]>([]);
    const [loading, setLoading] = useState(true);
    const [page, setPage] = useState(1);
    const [error, setError] = useState<string | null>(null);

    useEffect(() => {
        let mounted = true;
        (async () => {
            setLoading(true);
            setError(null);
            try {
                const endpoint = preferAdmin ? "/api/admin/contacts" : "/api/contacts";
                const res = await fetch(endpoint, { credentials: "include" });

                if (!res.ok) {
                    throw new Error(`Failed to load contacts (${res.status})`);
                }

                const list = await res.json();
                if (!mounted) return;
                setContacts(list);
            } catch (err) {
                const msg = err instanceof Error ? err.message : String(err);
                if (mounted) {
                    setError(msg);
                    onError?.(msg);
                }
            } finally {
                if (mounted) setLoading(false);
            }
        })();

        return () => {
            mounted = false;
        };
    }, [preferAdmin, onError]);

    if (loading) return <div>Loading contacts…</div>;
    if (error) return <div className="text-danger">Error: {error}</div>;
    if (contacts.length === 0) return <div>No contacts found.</div>;

    const totalPages = Math.max(1, Math.ceil(contacts.length / pageSize));
    const pageItems = contacts.slice((page - 1) * pageSize, page * pageSize);

    return (
        <div>
            <ul className="list-group mb-3">
                {pageItems.map((c) => (
                    <li
                        key={c.id}
                        className="list-group-item list-group-item-action"
                        role="button"
                        onClick={() => onSelect?.(c.id)}
                    >
                        <div className="d-flex justify-content-between">
                            <div>
                                <div className="fw-semibold">{c.contactName}</div>
                                <div className="small text-muted">{c.propertyAddress ?? "No address"}</div>
                            </div>
                            <div className="text-end small">
                                <div>{c.contactEmail}</div>
                                <div>{c.contactNumber}</div>
                            </div>
                        </div>
                    </li>
                ))}
            </ul>

            <div className="d-flex justify-content-between align-items-center mb-3">
                <div>Page {page} of {totalPages}</div>
                <div>
                    <button
                        className="btn btn-sm btn-outline-secondary me-2"
                        onClick={() => setPage((p) => Math.max(1, p - 1))}
                        disabled={page === 1}
                    >
                        Previous
                    </button>
                    <button
                        className="btn btn-sm btn-outline-secondary"
                        onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
                        disabled={page === totalPages}
                    >
                        Next
                    </button>
                </div>
            </div>
        </div>
    );
};

export default ContactsList;