import { useState } from "react";
import { Navigate } from "react-router-dom";
import { getErrorMessage } from "../api/client";
import { useAuth } from "../hooks/useAuth";

const DEMO_ACCOUNTS = [
    { role: "Admin", email: "admin@taskflow.com", password: "Admin@123" },
    { role: "Manager", email: "manager@taskflow.com", password: "Manager@123" },
    { role: "User", email: "user1@taskflow.com", password: "User@1234" },
];

export default function LoginPage() {
    const { user, login, register, sessionNotice } = useAuth();
    const [isRegister, setIsRegister] = useState(false);
    const [form, setForm] = useState({ fullName: "", email: "", password: "" });
    const [error, setError] = useState("");
    const [info, setInfo] = useState("");
    const [loading, setLoading] = useState(false);

    if (user) return <Navigate to="/" replace />;

    const update = (field) => (event) =>
        setForm({ ...form, [field]: event.target.value });

    const switchMode = () => {
        setIsRegister(!isRegister);
        setError("");
        setInfo("");
    };

    const submit = async (event) => {
        event.preventDefault();
        setLoading(true);
        setError("");
        setInfo("");

        try {
            if (isRegister) {
                await register(form.fullName.trim(), form.email.trim(), form.password);
                setIsRegister(false);
                setForm({ ...form, fullName: "", password: "" });
                setInfo("Account created. Please log in.");
            } else {
                await login(form.email.trim(), form.password);
            }
        } catch (err) {
            setError(getErrorMessage(err));
        } finally {
            setLoading(false);
        }
    };

    return (
        <div className="auth-page">
            <div className="auth-card">
                <div className="auth-brand">
                    <span className="brand-mark" aria-hidden="true">TF</span>
                    <h1>TaskFlow</h1>
                    <p className="muted">Team Task Management System</p>
                </div>

                <h2>{isRegister ? "Create your account" : "Welcome back"}</h2>

                {(error || info || sessionNotice) && (
                    <div
                        className={error ? "alert alert-error" : "alert alert-info"}
                        role={error ? "alert" : "status"}
                    >
                        {error || info || sessionNotice}
                    </div>
                )}

                <form onSubmit={submit}>
                    {isRegister && (
                        <div className="field">
                            <label htmlFor="fullName">Full name</label>
                            <input
                                id="fullName"
                                value={form.fullName}
                                onChange={update("fullName")}
                                autoComplete="name"
                                maxLength={100}
                                required
                            />
                        </div>
                    )}

                    <div className="field">
                        <label htmlFor="email">Email address</label>
                        <input
                            id="email"
                            type="email"
                            value={form.email}
                            onChange={update("email")}
                            autoComplete="email"
                            required
                        />
                    </div>

                    <div className="field">
                        <label htmlFor="password">Password</label>
                        <input
                            id="password"
                            type="password"
                            value={form.password}
                            onChange={update("password")}
                            autoComplete={isRegister ? "new-password" : "current-password"}
                            minLength={8}
                            required
                        />
                        {isRegister && <p className="hint">At least 8 characters.</p>}
                    </div>

                    <button type="submit" className="btn btn-primary btn-block" disabled={loading}>
                        {loading ? "Please wait..." : isRegister ? "Register" : "Log in"}
                    </button>
                </form>

                <p className="switch-mode">
                    {isRegister ? "Already have an account?" : "New to TaskFlow?"}{" "}
                    <button type="button" className="link-btn" onClick={switchMode}>
                        {isRegister ? "Log in" : "Create an account"}
                    </button>
                </p>

                {!isRegister && (
                    <div className="demo-box">
                        <p>Demo accounts (click to fill):</p>
                        <div className="demo-list">
                            {DEMO_ACCOUNTS.map((account) => (
                                <button
                                    key={account.role}
                                    type="button"
                                    className="btn btn-secondary btn-sm"
                                    onClick={() =>
                                        setForm({ ...form, email: account.email, password: account.password })
                                    }
                                >
                                    {account.role}
                                </button>
                            ))}
                        </div>
                    </div>
                )}
            </div>
        </div>
    );
}