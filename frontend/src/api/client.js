import axios from "axios";

const API_URL = import.meta.env.VITE_API_URL || "https://localhost:7019/api";

export const TOKEN_KEY = "tm_token";
export const USER_KEY = "tm_user";
export const EXPIRY_KEY = "tm_expires";

const client = axios.create({ baseURL: API_URL });

let unauthorizedHandler = null;

export function setUnauthorizedHandler(handler) {
    unauthorizedHandler = handler;
}

client.interceptors.request.use((config) => {
    const token = localStorage.getItem(TOKEN_KEY);
    if (token) {
        config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
});

client.interceptors.response.use(
    (response) => response,
    (error) => {
        const url = error.config?.url || "";
        const isAuthCall = url.includes("/Auth/");

        // An expired or invalid token on a protected call logs the user out.
        if (error.response?.status === 401 && !isAuthCall && unauthorizedHandler) {
            unauthorizedHandler();
        }
        return Promise.reject(error);
    }
);

export function getErrorMessage(error, fallback = "Something went wrong. Please try again.") {
    if (error.response) {
        const data = error.response.data;
        if (data?.message) return data.message;
        if (typeof data === "string" && data.trim()) return data;

        switch (error.response.status) {
            case 401:
                return "Your session has expired. Please log in again.";
            case 403:
                return "You do not have permission to do that.";
            case 404:
                return "The requested item was not found.";
            default:
                return fallback;
        }
    }
    if (error.request) {
        return "Cannot reach the server. Check that the API is running.";
    }
    return error.message || fallback;
}

export default client;