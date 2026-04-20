import React, { useState } from "react";

interface SignInResponse {
    ok?: boolean;
    username?: string;
    isAdmin?: boolean;
    roles?: string[];
    message?: string;
    error?: string;
}

// When not signed in:
// - Show the sign in form
//  Username/email
//  Password
//  Sign in button
//  Forget password link (opens a browser warning showing "our deepest condolences but ur fked lol")
// When signed in:
// - Show the user's username and a sign out button
// - If the user is an admin, prepend "Admin" to the user's username

const AccountComponent: React.FC = () => {
    const [isSignedIn, setIsSignedIn] = useState(false);
    const [username, setUsername] = useState("");
    const [password, setPassword] = useState("");
    const [isAdmin, setIsAdmin] = useState(false);
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
            // Response body expected: { username, isAdmin }.
            const res = await fetch("/api/auth/sign-in", {
                method: "POST",
                credentials: "include", // even though sign in does not require existing credentials, this ensures the server can set the HttpOnly cookie in the response
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({ username, password }),
            });

            const payload: SignInResponse = await res
                .json()
                .catch(() => ({} as SignInResponse));

            if (!res.ok) {
                setError(payload?.message || payload?.error || "Sign-in failed. Please check your credentials.");
            } else {
                // Use returned username/isAdmin to update UI.
                setIsSignedIn(true);
                setIsAdmin(Boolean(payload?.isAdmin || (payload?.roles || []).includes?.("Admin")));
                setUsername(payload?.username || username);
                setPassword("");
            }
        } catch (err) {
            setError("Network error while signing in. Please try again: " + (err instanceof Error ? err.message : String(err)));
        } finally {
            setLoading(false);
        }
    };

    const handleSignOut = async () => {
        setError(null);
        try {
            // Clear server-side cookie by calling sign-out endpoint.
            await fetch("/api/auth/sign-out", {
                method: "POST",
                credentials: "include", // ensure cookie is sent so server can clear it
            });
        } catch {
            // even if sign-out call fails, clear client state to avoid stale UI
        } finally {
            // Clear client UI state
            setIsSignedIn(false);
            setUsername("");
            setIsAdmin(false);
            setPassword("");
        }
    };

    const handleForgotPassword = (e: React.MouseEvent<HTMLAnchorElement>) => {
        e.preventDefault();
        window.alert("Our deepest condolences but ur fked lol");
    };

    return (
        <div className="d-flex align-items-center gap-2" role="region" aria-label="Account">
            {!isSignedIn ? (
                <div
                    className="d-flex align-items-center gap-2"
                    role="form"
                    // allow Enter to trigger sign-in for keyboard users
                    onKeyDown={(e) => {
                        if (e.key === "Enter" && !loading) {
                            e.preventDefault();
                            void handleSignIn();
                        }
                    }}
                >
                    <input
                        type="text"
                        className="form-control form-control-sm"
                        placeholder="Username / Email"
                        value={username}
                        onChange={(e) => setUsername(e.target.value)}
                        aria-label="Username or email"
                        disabled={loading}
                    />
                    <input
                        type="password"
                        className="form-control form-control-sm"
                        placeholder="Password"
                        value={password}
                        onChange={(e) => setPassword(e.target.value)}
                        aria-label="Password"
                        disabled={loading}
                    />
                    <button
                        type="button"
                        className="btn btn-sm btn-primary"
                        onClick={() => void handleSignIn()}
                        disabled={loading}
                        aria-disabled={loading}
                    >
                        {loading ? "Signing in..." : "Sign in"}
                    </button>

                    <a
                        href="#"
                        className="btn btn-link btn-sm p-0"
                        onClick={handleForgotPassword}
                        aria-label="Forgot password"
                    >
                        Forgot?
                    </a>

                    {error && (
                        <div className="small text-danger ms-2" role="alert" aria-live="polite">
                            {error}
                        </div>
                    )}
                </div>
            ) : (
                <div className="d-flex align-items-center gap-2">
                    <span className="small mb-0">
                        Hello, {isAdmin ? `Admin ${username}` : username}.
                    </span>
                    <button className="btn btn-sm btn-outline-secondary" onClick={handleSignOut}>
                        Sign out
                    </button>
                </div>
            )}
        </div>
    );
};

export default AccountComponent;
