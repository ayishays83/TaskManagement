import { useCallback, useEffect, useState } from "react";
import client, { getErrorMessage } from "../api/client";
import CreateTaskModal from "../components/CreateTaskModal";
import TaskDetailModal from "../components/TaskDetailModal";
import { useAuth } from "../hooks/useAuth";
import { PRIORITIES, STATUSES, formatDate, isOverdue, slug } from "../utils/format";

const EMPTY_FILTERS = {
    status: "",
    priority: "",
    deadlineFrom: "",
    deadlineTo: "",
    assignedToId: "",
};

export default function DashboardPage() {
    const { user } = useAuth();
    const canManage = user.role === "Admin" || user.role === "Manager";

    const [summary, setSummary] = useState(null);
    const [perUser, setPerUser] = useState([]);
    const [tasks, setTasks] = useState([]);
    const [assignable, setAssignable] = useState([]);
    const [filters, setFilters] = useState(EMPTY_FILTERS);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState("");
    const [toast, setToast] = useState("");
    const [showCreate, setShowCreate] = useState(false);
    const [selectedTask, setSelectedTask] = useState(null);

    const fetchTasks = useCallback(async () => {
        const params = Object.fromEntries(
            Object.entries(filters).filter(([, value]) => value !== "")
        );
        const response = await client.get("/Task", { params });
        return response.data;
    }, [filters]);

    const fetchOverview = useCallback(async () => {
        const requests = [client.get("/Dashboard/summary")];
        if (canManage) requests.push(client.get("/Dashboard/per-user"));

        const [summaryResponse, perUserResponse] = await Promise.all(requests);
        return {
            summary: summaryResponse.data,
            perUser: perUserResponse ? perUserResponse.data : [],
        };
    }, [canManage]);

    useEffect(() => {
        let active = true;

        fetchTasks()
            .then((data) => {
                if (active) {
                    setTasks(data);
                    setError("");
                }
            })
            .catch((err) => {
                if (active) setError(getErrorMessage(err));
            })
            .finally(() => {
                if (active) setLoading(false);
            });

        return () => {
            active = false;
        };
    }, [fetchTasks]);

    useEffect(() => {
        let active = true;

        fetchOverview()
            .then((overview) => {
                if (active) {
                    setSummary(overview.summary);
                    setPerUser(overview.perUser);
                }
            })
            .catch((err) => {
                if (active) setError(getErrorMessage(err));
            });

        return () => {
            active = false;
        };
    }, [fetchOverview]);

    useEffect(() => {
        if (!canManage) return undefined;
        let active = true;

        client
            .get("/Users/assignable")
            .then((response) => {
                if (active) setAssignable(response.data);
            })
            .catch(() => {
                // The assignee list is only needed for filters and task creation.
            });

        return () => {
            active = false;
        };
    }, [canManage]);

    useEffect(() => {
        if (!toast) return undefined;
        const timer = setTimeout(() => setToast(""), 4000);
        return () => clearTimeout(timer);
    }, [toast]);

    const refresh = useCallback(async () => {
        try {
            const [tasksData, overview] = await Promise.all([fetchTasks(), fetchOverview()]);
            setTasks(tasksData);
            setSummary(overview.summary);
            setPerUser(overview.perUser);
            setError("");
        } catch (err) {
            setError(getErrorMessage(err));
        }
    }, [fetchTasks, fetchOverview]);

    const changeStatus = async (task, status) => {
        try {
            await client.patch(`/Task/${task.id}/status`, { status });
            setToast(`"${task.title}" moved to ${status}.`);
            await refresh();
        } catch (err) {
            setError(getErrorMessage(err));
        }
    };

    const updateFilter = (field) => (event) =>
        setFilters({ ...filters, [field]: event.target.value });

    const hasFilters = Object.values(filters).some((value) => value !== "");

    const statCards = summary
        ? [
            { label: "Total tasks", value: summary.totalTasks, filter: "", tone: "total" },
            { label: "To Do", value: summary.toDo, filter: "To Do", tone: "todo" },
            { label: "In Progress", value: summary.inProgress, filter: "In Progress", tone: "progress" },
            { label: "Done", value: summary.done, filter: "Done", tone: "done" },
        ]
        : [];

    return (
        <>
            <div className="page-head">
                <div>
                    <h1 className="page-title">Dashboard</h1>
                    <p className="muted">
                        {canManage ? "Overview of your team's tasks" : "Overview of your assigned tasks"}
                    </p>
                </div>
                {canManage && (
                    <button type="button" className="btn btn-primary" onClick={() => setShowCreate(true)}>
                        + Create task
                    </button>
                )}
            </div>

            {toast && <div className="alert alert-success" role="status">{toast}</div>}
            {error && <div className="alert alert-error" role="alert">{error}</div>}

            {summary && (
                <section aria-label="Task summary">
                    <div className="stats-grid">
                        {statCards.map((card) => (
                            <button
                                key={card.label}
                                type="button"
                                className={`stat stat-${card.tone}`}
                                aria-pressed={filters.status === card.filter}
                                onClick={() => setFilters({ ...filters, status: card.filter })}
                            >
                                <span className="stat-label">{card.label}</span>
                                <span className="stat-value">{card.value}</span>
                            </button>
                        ))}
                        <div className="stat stat-overdue">
                            <span className="stat-label">Overdue</span>
                            <span className="stat-value">{summary.overdue}</span>
                        </div>
                    </div>
                    <p className="muted priority-line">
                        Priority: {summary.highPriority} high &middot; {summary.mediumPriority} medium &middot; {summary.lowPriority} low
                    </p>
                </section>
            )}

            {canManage && perUser.length > 0 && (
                <section className="card" aria-labelledby="per-user-heading">
                    <h2 id="per-user-heading" className="section-title">Status per user</h2>
                    <div className="table-wrap">
                        <table className="data">
                            <thead>
                                <tr>
                                    <th scope="col">User</th>
                                    <th scope="col">To Do</th>
                                    <th scope="col">In Progress</th>
                                    <th scope="col">Done</th>
                                    <th scope="col">Overdue</th>
                                    <th scope="col">Total</th>
                                </tr>
                            </thead>
                            <tbody>
                                {perUser.map((row) => (
                                    <tr key={row.userId}>
                                        <th scope="row">{row.userName}</th>
                                        <td>{row.toDo}</td>
                                        <td>{row.inProgress}</td>
                                        <td>{row.done}</td>
                                        <td className={row.overdue > 0 ? "overdue-text" : ""}>{row.overdue}</td>
                                        <td>{row.total}</td>
                                    </tr>
                                ))}
                            </tbody>
                        </table>
                    </div>
                </section>
            )}

            <section className="card" aria-labelledby="tasks-heading">
                <h2 id="tasks-heading" className="section-title">
                    {canManage ? "Tasks" : "My tasks"}
                </h2>

                <form className="filters" onSubmit={(event) => event.preventDefault()} aria-label="Task filters">
                    <div className="field">
                        <label htmlFor="f-status">Status</label>
                        <select id="f-status" value={filters.status} onChange={updateFilter("status")}>
                            <option value="">All</option>
                            {STATUSES.map((s) => (
                                <option key={s}>{s}</option>
                            ))}
                        </select>
                    </div>

                    <div className="field">
                        <label htmlFor="f-priority">Priority</label>
                        <select id="f-priority" value={filters.priority} onChange={updateFilter("priority")}>
                            <option value="">All</option>
                            {PRIORITIES.map((p) => (
                                <option key={p}>{p}</option>
                            ))}
                        </select>
                    </div>

                    <div className="field">
                        <label htmlFor="f-from">Deadline from</label>
                        <input id="f-from" type="date" value={filters.deadlineFrom} onChange={updateFilter("deadlineFrom")} />
                    </div>

                    <div className="field">
                        <label htmlFor="f-to">Deadline to</label>
                        <input id="f-to" type="date" value={filters.deadlineTo} onChange={updateFilter("deadlineTo")} />
                    </div>

                    {canManage && (
                        <div className="field">
                            <label htmlFor="f-assignee">Assignee</label>
                            <select id="f-assignee" value={filters.assignedToId} onChange={updateFilter("assignedToId")}>
                                <option value="">Everyone</option>
                                {assignable.map((u) => (
                                    <option key={u.id} value={u.id}>
                                        {u.fullName}
                                    </option>
                                ))}
                            </select>
                        </div>
                    )}

                    <div className="field filter-actions">
                        <button
                            type="button"
                            className="btn btn-secondary"
                            onClick={() => setFilters(EMPTY_FILTERS)}
                            disabled={!hasFilters}
                        >
                            Clear filters
                        </button>
                    </div>
                </form>

                {loading ? (
                    <p className="muted">Loading tasks...</p>
                ) : tasks.length === 0 ? (
                    <div className="empty">
                        <p>{hasFilters ? "No tasks match these filters." : "No tasks yet."}</p>
                        {canManage && !hasFilters && <p className="muted">Use "Create task" to add the first one.</p>}
                    </div>
                ) : (
                    <ul className="task-list">
                        {tasks.map((task) => {
                            const overdue = isOverdue(task);

                            return (
                                <li key={task.id} className="task-card">
                                    <div className="task-main">
                                        <button type="button" className="task-title" onClick={() => setSelectedTask(task)}>
                                            {task.title}
                                        </button>
                                        {task.description && <p className="task-desc">{task.description}</p>}
                                        <div className="task-meta">
                                            <span className={`badge badge-${task.priority.toLowerCase()}`}>
                                                {task.priority} priority
                                            </span>
                                            <span className={overdue ? "overdue-text" : ""}>
                                                Due: {formatDate(task.deadline)}
                                                {overdue ? " (overdue)" : ""}
                                            </span>
                                            <span>Assigned to: {task.assignedToName}</span>
                                            <span>Created by: {task.createdByName}</span>
                                        </div>
                                    </div>

                                    <div className="task-side">
                                        <label className="sr-only" htmlFor={`status-${task.id}`}>
                                            Status for {task.title}
                                        </label>
                                        <select
                                            id={`status-${task.id}`}
                                            className={`status-select status-${slug(task.status)}`}
                                            value={task.status}
                                            onChange={(event) => changeStatus(task, event.target.value)}
                                        >
                                            {STATUSES.map((s) => (
                                                <option key={s}>{s}</option>
                                            ))}
                                        </select>
                                        <button type="button" className="btn btn-secondary btn-sm" onClick={() => setSelectedTask(task)}>
                                            Details &amp; comments
                                        </button>
                                    </div>
                                </li>
                            );
                        })}
                    </ul>
                )}
            </section>

            {showCreate && (
                <CreateTaskModal
                    assignable={assignable}
                    onClose={() => setShowCreate(false)}
                    onCreated={() => {
                        setShowCreate(false);
                        setToast("Task created and the assignee was notified.");
                        refresh();
                    }}
                />
            )}

            {selectedTask && (
                <TaskDetailModal
                    task={selectedTask}
                    onClose={() => setSelectedTask(null)}
                    onChanged={refresh}
                />
            )}
        </>
    );
}