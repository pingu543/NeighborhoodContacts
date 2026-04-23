import React, { useEffect, useState } from "react";


export type ContactDetails = {
    id: string;
    contactName: string;
    contactEmail?: string;
    contactNumber?: string;
    AboutMe?: string;
    NewPassword?: string;
};

export type AdminContactDetails = {
    id: string;
    contactName: string;
    contactEmail?: string;
    contactNumber?: string;
    AboutMe?: string;
    NewPassword?: string;
    isVisible?: boolean;
    isAcitive?: boolean;
    isAdmin?: boolean;
};

type Props = {
    preferAdmin?: boolean;
    pageSize?: number;
    onSelect?: (id: string) => void;
    onError?: (msg: string) => void;
};

// Fetch and displlay the contact details
const ContactDetails: React.FC<Props> = ({ preferAdmin = false, pageSize = 10, onSelect, onError }) => {
    const [contact, setContact] = useState<ContactDetails>();
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);


    useEffect(() => {
        let mounted = true;
        (async () => {
            setLoading(true);
            setError(null);
            try {
                // Build endpoint: if preferAdmin, optionally include propertyGroupId query param
                let endpoint = preferAdmin ? "/api/admin/contacts" : "/api/contacts";
                if (preferAdmin && selectedGroupId) {
                    const qp = new URLSearchParams({ propertyGroupId: selectedGroupId });
                    endpoint = `${endpoint}?${qp.toString()}`;
                }

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
    if (contact) return <div>No contact found.</div>;

    return (
        <div>
            {contact.map((d) => (
                <div>
                    <div></div>  <button></button>

                </div>
            
            
            ))}

        </div> >
  );
}

export default ContactDetails;