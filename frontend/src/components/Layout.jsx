import { NavLink, Outlet } from "react-router-dom";
import { useAuth } from "../hooks/useAuth";
import { initials } from "../utils/format";
import NotificationBell from "./NotificationBell";

export default function Layout() {
    const { user, logout } = useAuth();
    const canManage = user.role === "Admin" || user.role === "Manager";

    return (
        <>
            <a className="skip-link" href="#main">
                Skip to content
            </a>

            <header className="topbar">
                <div className="topbar-inner">
                    <div className="brand">
                        <span className="brand-mark" aria-hidden="true">TF</span>
                        <span>TaskFlow</span>
                    </div>

                    <nav className="main-nav" aria-label="Main">
                        <NavLink to="/" end>Dashboard</NavLink>
                        {canManage && <NavLink to="/teams">Teams</NavLink>}
                    </nav>

                    <div className="topbar-actions">
                        <NotificationBell />
                        <div className="user-chip">
                            <span className="avatar" aria-hidden="true">{initials(user.fullName)}</span>
                            <span className="user-meta">
                                <strong>{user.fullName}</strong>
                                <small>{user.role}</small>
                            </span>
                        </div>
                        <button type="button" className="btn btn-ghost-light" onClick={() => logout()}>
                            Log out
                        </button>
                    </div>
                </div>
            </header>

            <main id="main" className="container">
                <Outlet />
            </main>
        </>
    );
}