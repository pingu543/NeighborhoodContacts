import React, { useState, useEffect, useCallback } from "react";
import type { User } from "./AuthContext";
import { AuthContext } from "./AuthContext";

// AuthProvider is responsible for fetching and providing the current user info to the app.

// Fetches the current user from the backend. Returns null if not signed in or on error.
async function fetchCurrentUser(signal?: AbortSignal): Promise<User | null> {
    try {
        const res = await fetch("/api/users/me", { credentials: "include", signal });
        if (!res.ok) return null;
        return (await res.json().catch(() => null)) as User | null;
    } catch {
        return null;
    }
}

// AuthProvider component that wraps the app and provides auth state and refresh function.
// It fetches the current user on mount and whenever refresh is called, and updates the context accordingly.
// The context value includes:
// - user: the current user object or null if not signed in
// - isSignedIn: boolean indicating if the user is signed in (null while checking)
// - isAdmin: boolean indicating if the user has admin privileges (exposed for convenience)
// - refresh: function to re-fetch the current user and update the context
export const AuthProvider: React.FC<{ children: React.ReactNode; }> = ({ children }) => {
    const [user, setUser] = useState<User | null>(null);
    const [isSignedIn, setIsSignedIn] = useState<boolean | null>(null);

    // Helper to update both user and isSignedIn state together
    const setAuthState = (u: User | null) => {
        setUser(u);
        setIsSignedIn(Boolean(u));
    };

    // On mount, fetch the current user to determine if signed in. Use an AbortController to avoid setting state if unmounted.
    useEffect(() => {
        const ac = new AbortController();

        const loadOnMount = async () => {
            try {
                const json = await fetchCurrentUser(ac.signal);
                if (ac.signal.aborted) return;
                setAuthState(json);
            } catch (err) {
                const errorName = (err as { name?: string }).name;
                if (errorName === "AbortError") return;
                setAuthState(null);
            }
        };

        void loadOnMount();

        return () => {
            ac.abort();
        };
    }, []);

    // Refresh function to re-fetch the current user and update the context. This can be called after sign-in/sign-out actions to sync state.
    const refresh = useCallback(async (): Promise<User | null> => {
        const json = await fetchCurrentUser();
        setAuthState(json);
        return json;
    }, []);

    // Provide the user, isSignedIn, isAdmin, and refresh function in the context value for use by child components.
    return (
        <AuthContext.Provider value={{ user, isSignedIn, isAdmin: Boolean(user?.isAdmin), refresh }}>
            {children}
        </AuthContext.Provider>
    );
};