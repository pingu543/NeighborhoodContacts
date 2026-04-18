import React, { useState } from "react";

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

  const handleSignIn = () => {
    if (username.trim()) {
      setIsSignedIn(true);
      setPassword("");
    } else {
      window.alert("Please enter a username or email to sign in (placeholder).");
    }
  };

  const handleSignOut = () => {
    setIsSignedIn(false);
    setUsername("");
    setIsAdmin(false);
  };

  const handleForgotPassword = (e: React.MouseEvent<HTMLAnchorElement>) => {
    e.preventDefault();
    window.alert("Our deepest condolences but ur fked lol");
  };

  return (
    <div className="d-flex align-items-center gap-2" role="region" aria-label="Account">
      {!isSignedIn ? (
        // Use a non-form container but keep semantic role="form"
        <div
          className="d-flex align-items-center gap-2"
          role="form"
          // allow Enter to trigger sign-in for keyboard users
          onKeyDown={(e) => {
            if (e.key === "Enter") {
              e.preventDefault();
              handleSignIn();
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
          />
          <input
            type="password"
            className="form-control form-control-sm"
            placeholder="Password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            aria-label="Password"
          />
          <button type="button" className="btn btn-sm btn-primary" onClick={handleSignIn}>
            Sign in
          </button>

          <a
            href="#"
            className="btn btn-link btn-sm p-0"
            onClick={handleForgotPassword}
            aria-label="Forgot password"
          >
            Forgot?
          </a>
        </div>
      ) : (
        <div className="d-flex align-items-center gap-2">
          <span className="small mb-0">
            {isAdmin ? "Admin " : ""}
            {username}
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
