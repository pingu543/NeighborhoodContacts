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

export const AuthContext = createContext<AuthCtx | undefined>(undefined);

export const useAuth = (): AuthCtx => {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error("useAuth must be used within AuthProvider");
  return ctx;
};