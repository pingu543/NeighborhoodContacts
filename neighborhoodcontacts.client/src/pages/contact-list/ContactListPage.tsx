import { useState } from "react";
import { useNavigate } from "react-router-dom";
import ContactsList from "../../components/contacts/ContactsList";
import AdminControl from "../../components/admin-control/AdminControlComponent";
import { useAuth } from "../../context/AuthContext";

// This page shows a list of contacts. It also has a button to download the contacts as a PDF.
// Will only show the included components if signed in.
// Will show admin control if user is an admin.
// Will tell contact list to prefer admin endpoint if user is an admin.

function ContactListPage() {
    const { isSignedIn, isAdmin } = useAuth();
    const [error, setError] = useState<string | null>(null);
    const navigate = useNavigate();

    const handleSelect = (id: string) => navigate(`/contacts/${id}`);

    return (
        <div className="container py-3">
            {isAdmin && <AdminControl />}

            <main>
                {error && <div className="text-danger mb-2">Error: {error}</div>}

                {isSignedIn === null ? (
                    <div>Checking sign-in…</div>
                ) : isSignedIn === false ? (
                    <div className="mb-3">Please sign in to view contacts.</div>
                ) : (
                    <div>
                        <ContactsList preferAdmin={isAdmin} onSelect={handleSelect} pageSize={10} onError={(m) => setError(m)} />
                        <div className="mt-3">
                            <button className="btn btn-sm btn-primary" onClick={() => {
                                void fetch("/api/contacts/download-pdf", { credentials: "include" })
                                    .then(() => alert("Download requested"))
                                    .catch(() => alert("Download failed"));
                            }}>
                                Download PDF
                            </button>
                        </div>
                    </div>
                )}
            </main>
        </div>
    );
}

export default ContactListPage;