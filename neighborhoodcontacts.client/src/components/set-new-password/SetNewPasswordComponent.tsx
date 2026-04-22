import { useState } from 'react';

interface Props {
    requireCurrentPassword?: boolean;
    onSubmit: (payload: { currentPassword?: string; newPassword: string }) => Promise<void>;
    onCancel?: () => void;
}


// SetNewPassword component that allows users to set a new password.
// Optionally requiring the current password for verification.
// Normal users will have requireCurrentPassword=true, while admins setting a password for another user will have it false.

export default function SetNewPassword({ requireCurrentPassword, onSubmit, onCancel }: Props) {
    const [currentPassword, setCurrentPassword] = useState('');
    const [newPassword, setNewPassword] = useState('');
    const [confirm, setConfirm] = useState('');
    const [error, setError] = useState<string | null>(null);
    const [isSubmitting, setIsSubmitting] = useState(false);

    const handleSubmit = async () => {
        setError(null);
        if (newPassword !== confirm) {
            setError('Passwords do not match');
            return;
        }
        setIsSubmitting(true);
        try {
            await onSubmit({ newPassword, ...(requireCurrentPassword ? { currentPassword } : {}) });
        } catch (err) {
            setError("Failed to set password: " + (err instanceof Error ? err.message : String(err)));
        } finally {
            setIsSubmitting(false);
        }
    };

    return (
        <div role="form" aria-label="Set new password">
            {requireCurrentPassword && (
                <div>
                    <label>Current password</label>
                    <input
                        type="password"
                        value={currentPassword}
                        onChange={e => setCurrentPassword(e.target.value)}
                        autoComplete="current-password"
                    />
                </div>
            )}
            <div>
                <label>New password</label>
                <input
                    type="password"
                    value={newPassword}
                    onChange={e => setNewPassword(e.target.value)}
                    autoComplete="new-password"
                />
            </div>
            <div>
                <label>Confirm password</label>
                <input
                    type="password"
                    value={confirm}
                    onChange={e => setConfirm(e.target.value)}
                    autoComplete="new-password"
                />
            </div>
            {error && <div role="alert">{error}</div>}
            <button type="button" onClick={handleSubmit} disabled={isSubmitting}>
                {isSubmitting ? 'Setting...' : 'Set password'}
            </button>
            {onCancel && (
                <button type="button" onClick={onCancel} disabled={isSubmitting}>
                    Cancel
                </button>
            )}
        </div>
    );
}