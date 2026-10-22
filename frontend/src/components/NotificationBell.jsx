import { useEffect, useRef, useState } from "react";
import client from "../api/client";
import { formatDateTime } from "../utils/format";

async function fetchNotifications() {
    const response = await client.get("/Notification");
    return response.data;
}

export default function NotificationBell() {
    const [open, setOpen] = useState(false);
    const [data, setData] = useState({ unreadCount: 0, items: [] });
    const wrapRef = useRef(null);

    // Poll for new notifications. Errors are ignored because the bell is not critical.
    useEffect(() => {
        let active = true;

        const poll = () =>
            fetchNotifications()
                .then((result) => {
                    if (active) setData(result);
                })
                .catch(() => { });

        poll();
        const timer = setInterval(poll, 30000);

        return () => {
            active = false;
            clearInterval(timer);
        };
    }, []);

    useEffect(() => {
        if (!open) return undefined;

        const onMouseDown = (event) => {
            if (!wrapRef.current?.contains(event.target)) setOpen(false);
        };
        const onKeyDown = (event) => {
            if (event.key === "Escape") setOpen(false);
        };

        document.addEventListener("mousedown", onMouseDown);
        document.addEventListener("keydown", onKeyDown);
        return () => {
            document.removeEventListener("mousedown", onMouseDown);
            document.removeEventListener("keydown", onKeyDown);
        };
    }, [open]);

    const toggle = () => {
        if (!open) {
            fetchNotifications()
                .then(setData)
                .catch(() => { });
        }
        setOpen(!open);
    };

    const markRead = async (item) => {
        if (item.isRead) return;
        try {
            await client.post(`/Notification/${item.id}/read`);
        } catch {
            return;
        }
        setData((current) => ({
            unreadCount: Math.max(0, current.unreadCount - 1),
            items: current.items.map((n) => (n.id === item.id ? { ...n, isRead: true } : n)),
        }));
    };

    const markAllRead = async () => {
        try {
            await client.post("/Notification/read-all");
            setData((current) => ({
                unreadCount: 0,
                items: current.items.map((n) => ({ ...n, isRead: true })),
            }));
        } catch {
            // Ignore; the user can try again.
        }
    };

    return (
        <div className="bell-wrap" ref={wrapRef}>
            <button
                type="button"
                className="bell-btn"
                onClick={toggle}
                aria-haspopup="true"
                aria-expanded={open}
                aria-label={`Notifications, ${data.unreadCount} unread`}
            >
                <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
                    <path d="M18 8a6 6 0 0 0-12 0c0 7-3 9-3 9h18s-3-2-3-9" />
                    <path d="M13.7 21a2 2 0 0 1-3.4 0" />
                </svg>
                {data.unreadCount > 0 && <span className="bell-count">{data.unreadCount}</span>}
            </button>

            {open && (
                <div className="bell-panel" role="region" aria-label="Notifications">
                    <div className="bell-head">
                        <strong>Notifications</strong>
                        <button type="button" className="link-btn" onClick={markAllRead} disabled={data.unreadCount === 0}>
                            Mark all read
                        </button>
                    </div>

                    {data.items.length === 0 ? (
                        <p className="bell-empty">No notifications yet.</p>
                    ) : (
                        <ul>
                            {data.items.map((item) => (
                                <li key={item.id}>
                                    <button
                                        type="button"
                                        className={item.isRead ? "bell-item" : "bell-item unread"}
                                        onClick={() => markRead(item)}
                                    >
                                        <span>{item.message}</span>
                                        <small>{formatDateTime(item.createdAt)}</small>
                                    </button>
                                </li>
                            ))}
                        </ul>
                    )}
                </div>
            )}
        </div>
    );
}