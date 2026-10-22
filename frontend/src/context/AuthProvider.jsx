import { useCallback, useEffect, useMemo, useState } from "react";
import client, {
    EXPIRY_KEY,
    TOKEN_KEY,
    USER_KEY,
    setUnauthorizedHandler,
} from "../api/client";
import { AuthContext } from "./authContext";

function clearStorage() {
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(USER_KEY);
    localStorage.removeItem(EXPIRY_KEY);
}

function readSession() {
    const token = localStorage.getItem(TOKEN_KEY);
    const rawUser = localStorage.getItem(USER_KEY);
    const expiresAt = localStorage.getItem(EXPIRY_KEY);

    if (!token || !rawUser) return null;

    if (expiresAt && new Date(expiresAt).getTime() <= Date.now()) {
        clearStorage();
        return null;
    }

    try {
        return { token, expiresAt, user: JSON.parse(rawUser) };
    } catch {
        clearStorage();
        return null;
    }
}

export default function AuthProvider({ children }) {
    const [session, setSession] = useState(() => readSession());
    const [sessionNotice, setSessionNotice] = useState("");

    const logout = useCallback((notice) => {
        clearStorage();
        setSession(null);
        setSessionNotice(typeof notice === "string" ? notice : "");
    }, []);

    // Any 401 from a protected API call ends the session.
    useEffect(() => {
        setUnauthorizedHandler(() =>
            logout("Your session has expired. Please log in again.")
        );
        return () => setUnauthorizedHandler(null);
    }, [logout]);

    // Log out automatically when the token expires.
    useEffect(() => {
        if (!session?.expiresAt) return undefined;

        const remaining = new Date(session.expiresAt).getTime() - Date.now();
        const timer = setTimeout(
            () => logout("Your session has expired. Please log in again."),
            Math.max(remaining, 0)
        );

        return () => clearTimeout(timer);
    }, [session, logout]);

    const login = useCallback(async (email, password) => {
        const { data } = await client.post("/Auth/login", { email, password });

        localStorage.setItem(TOKEN_KEY, data.token);
        localStorage.setItem(USER_KEY, JSON.stringify(data.user));
        localStorage.setItem(EXPIRY_KEY, data.expiresAt);

        setSessionNotice("");
        setSession({ token: data.token, expiresAt: data.expiresAt, user: data.user });
    }, []);

    const register = useCallback(async (fullName, email, password) => {
        await client.post("/Auth/register", { fullName, email, password });
    }, []);

    const value = useMemo(
        () => ({
            user: session?.user ?? null,
            sessionNotice,
            login,
            register,
            logout,
        }),
        [session, sessionNotice, login, register, logout]
    );

    return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}