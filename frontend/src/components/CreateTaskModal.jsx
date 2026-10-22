import { useState } from "react";
import client, { getErrorMessage } from "../api/client";
import { PRIORITIES } from "../utils/format";
import Modal from "./Modal";

const EMPTY_FORM = {
    title: "",
    description: "",
    priority: "Medium",
    deadline: "",
    assignedToId: "",
};

export default function CreateTaskModal({ assignable, onClose, onCreated }) {
    const [form, setForm] = useState(EMPTY_FORM);
    const [submitting, setSubmitting] = useState(false);
    const [error, setError] = useState("");

    const update = (field) => (event) =>
        setForm({ ...form, [field]: event.target.value });

    const submit = async (event) => {
        event.preventDefault();

        if (!form.title.trim()) {
            setError("Title is required.");
            return;
        }
        if (!form.assignedToId) {
            setError("Please choose who the task is assigned to.");
            return;
        }

        setSubmitting(true);
        setError("");

        try {
            await client.post("/Task", {
                title: form.title.trim(),
                description: form.description.trim(),
                priority: form.priority,
                deadline: form.deadline || null,
                assignedToId: Number(form.assignedToId),
            });
            onCreated();
        } catch (err) {
            setError(getErrorMessage(err));
            setSubmitting(false);
        }
    };

    const today = new Date().toISOString().slice(0, 10);

    return (
        <Modal title="Create task" onClose={onClose}>
            <form onSubmit={submit} noValidate>
                {error && <div className="alert alert-error" role="alert">{error}</div>}

                <div className="field">
                    <label htmlFor="task-title">Title</label>
                    <input id="task-title" value={form.title} onChange={update("title")} maxLength={150} required autoFocus />
                </div>

                <div className="field">
                    <label htmlFor="task-description">Description</label>
                    <textarea id="task-description" value={form.description} onChange={update("description")} maxLength={1000} />
                </div>

                <div className="form-row">
                    <div className="field">
                        <label htmlFor="task-priority">Priority</label>
                        <select id="task-priority" value={form.priority} onChange={update("priority")}>
                            {PRIORITIES.map((p) => (
                                <option key={p}>{p}</option>
                            ))}
                        </select>
                    </div>

                    <div className="field">
                        <label htmlFor="task-deadline">Deadline</label>
                        <input id="task-deadline" type="date" min={today} value={form.deadline} onChange={update("deadline")} />
                    </div>
                </div>

                <div className="field">
                    <label htmlFor="task-assignee">Assign to</label>
                    <select id="task-assignee" value={form.assignedToId} onChange={update("assignedToId")} required>
                        <option value="">Select a person</option>
                        {assignable.map((u) => (
                            <option key={u.id} value={u.id}>
                                {u.fullName} ({u.role})
                            </option>
                        ))}
                    </select>
                    {assignable.length === 0 && (
                        <p className="hint">No one to assign yet. Add members to your team on the Teams page first.</p>
                    )}
                </div>

                <div className="modal-actions">
                    <button type="button" className="btn btn-secondary" onClick={onClose}>
                        Cancel
                    </button>
                    <button type="submit" className="btn btn-primary" disabled={submitting}>
                        {submitting ? "Saving..." : "Create task"}
                    </button>
                </div>
            </form>
        </Modal>
    );
}