export const STATUSES = ["To Do", "In Progress", "Done"];
export const PRIORITIES = ["Low", "Medium", "High"];

// The API returns UTC timestamps without a zone suffix, so add one before parsing.
export function parseUtc(value) {
    if (!value) return null;
    const hasZone = /([zZ]|[+-]\d{2}:?\d{2})$/.test(value);
    return new Date(hasZone ? value : `${value}Z`);
}

export function formatDate(value) {
    if (!value) return "Not set";
    return new Date(value).toLocaleDateString(undefined, {
        day: "numeric",
        month: "short",
        year: "numeric",
    });
}

export function formatDateTime(value) {
    const date = parseUtc(value);
    if (!date) return "";
    return date.toLocaleString(undefined, {
        day: "numeric",
        month: "short",
        hour: "2-digit",
        minute: "2-digit",
    });
}

export function isOverdue(task) {
    if (!task.deadline || task.status === "Done") return false;
    const today = new Date();
    today.setHours(0, 0, 0, 0);
    return new Date(task.deadline) < today;
}

export function slug(value) {
    return value.toLowerCase().replace(/\s+/g, "-");
}

export function initials(name) {
    return name
        .split(" ")
        .filter(Boolean)
        .slice(0, 2)
        .map((part) => part[0].toUpperCase())
        .join("");
}