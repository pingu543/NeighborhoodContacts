import React, { useState } from "react";
import { useNavigate } from "react-router-dom";
import { useAuth } from "../../context/AuthContext";

// When not signed in:
// - Show the sign in form
//  Username/email
//  Password
//  Sign in button
//  Forget password link (opens a browser warning showing "our deepest condolences but ur fked lol")
// When signed in:
// - Show the user's username and a sign out button
// - If the user is an admin, prepend "Admin" to the user's username

interface SignInResponse {
    ok?: boolean;
    username?: string;
    isAdmin?: boolean;
    roles?: string[];
    message?: string;
    error?: string;
}

const AccountComponent: React.FC = () => {
    const { user, isSignedIn, refresh } = useAuth();
    const navigate = useNavigate();
    const [username, setUsername] = useState("");
    const [password, setPassword] = useState("");
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState<string | null>(null);

    const handleSignIn = async () => {
        if (!username.trim()) {
            setError("Please enter a username or email to sign in.");
            return;
        }

        setLoading(true);
        setError(null);

        try {
            const res = await fetch("/api/auth/sign-in", {
                method: "POST",
                credentials: "include",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({ username, password }),
            });

            const payload: SignInResponse = await res.json().catch(() => ({} as SignInResponse));

            if (!res.ok) {
                setError(payload?.message || payload?.error || "Sign-in failed. Please check your credentials.");
            } else {
                // server should set auth cookie; refresh shared auth state
                await refresh();
                setUsername("");
                setPassword("");
            }
        } catch (err) {
            setError("Network error while signing in: " + (err instanceof Error ? err.message : String(err)));
        } finally {
            setLoading(false);
        }
    };

    const handleSignOut = async () => {
        setError(null);
        try {
            await fetch("/api/auth/sign-out", {
                method: "POST",
                credentials: "include",
            });
        } catch {
            // ignore
        } finally {
            await refresh();
        }
    };

    const goToMyProfile = () => {
        if (!user?.id) return;
        navigate(`/contacts/${user.id}`);
    };

    // show header UI based on shared auth state
    return (
        <div className="d-flex flex-nowrap" role="region" aria-label="Account">
            {isSignedIn ? (
                <div className="d-flex align-items-center">
                    <span className="small me-2">Hello, {user?.isAdmin ? `Admin ${user?.username}` : user?.username}.</span>
                    <button className="btn btn-outline-secondary me-2" onClick={goToMyProfile}>My profile</button>
                    <button className="btn btn-outline-secondary" onClick={handleSignOut}>Sign out</button>
                </div>
            ) : (
                <div
                    className="d-flex align-items-center flex-nowrap"
                    role="form"
                    onKeyDown={(e) => {
                        if (e.key === "Enter" && !loading) {
                            e.preventDefault();
                            void handleSignIn();
                        }
                    }}
                >
                    {error && <div className="small text-danger text-nowrap me-2" role="alert" aria-live="polite">{error}</div>}
                    <input type="text" className="form-control" placeholder="Username / Email" value={username} onChange={(e) => setUsername(e.target.value)} disabled={loading} />
                    <input type="password" className="form-control mx-2" placeholder="Password" value={password} onChange={(e) => setPassword(e.target.value)} disabled={loading} />
                    <button type="button" className="btn btn-primary text-nowrap" onClick={() => void handleSignIn()} disabled={loading}>
                        {loading ? "Signing in..." : "Sign in"}
                    </button>
                </div>
            )}
        </div>
    );
};

export default AccountComponent;
