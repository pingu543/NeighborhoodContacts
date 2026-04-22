import { createContext, useContext } from "react";

export type User = {
  id: string;
  username?: string;
  isAdmin?: boolean;
  [key: string]: unknown;
} | null;

type AuthCtx = {
  user: User;
  isSignedIn: boolean | null; // null = checking
  isAdmin: boolean;
  refresh: () => Promise<User | null>;
};

// AuthContext provides the current user info and auth-related functions to the app.
// This is what other components should use to access auth state and actions.
// The actual fetching and state management is done in AuthProvider.
// Use refresh to re-fetch the current user after sign-in/sign-out actions to keep state in sync.

export const AuthContext = createContext<AuthCtx | undefined>(undefined);

export const useAuth = (): AuthCtx => {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error("useAuth must be used within AuthProvider");
  return ctx;
};