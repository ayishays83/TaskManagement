import { useEffect, useState } from "react";
import client, { getErrorMessage } from "../api/client";
import { STATUSES, formatDate, formatDateTime, isOverdue } from "../utils/format";
import Modal from "./Modal";

export default function TaskDetailModal({ task, onClose, onChanged }) {
    const [current, setCurrent] = useState(task);
    const [comments, setComments] = useState([]);
    const [loading, setLoading] = useState(true);
    const [content, setContent] = useState("");
    const [posting, setPosting] = useState(false);
    const [error, setError] = useState("");

    useEffect(() => {
        let active = true;

        client
            .get(`/Comment/${task.id}`)
            .then((response) => {
                if (active) setComments(response.data);
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
    }, [task.id]);

    const changeStatus = async (status) => {
        setError("");
        try {
            const response = await client.patch(`/Task/${current.id}/status`, { status });
            setCurrent(response.data);
            onChanged();
        } catch (err) {
            setError(getErrorMessage(err));
        }
    };

    const postComment = async (event) => {
        event.preventDefault();
        if (!content.trim()) return;

        setPosting(true);
        setError("");

        try {
            const response = await client.post(`/Comment/${current.id}`, { content: content.trim() });
            setComments((existing) => [...existing, response.data]);
            setContent("");
        } catch (err) {
            setError(getErrorMessage(err));
        } finally {
            setPosting(false);
        }
    };

    const overdue = isOverdue(current);

    return (
        <Modal title={current.title} onClose={onClose} wide>
            {error && <div className="alert alert-error" role="alert">{error}</div>}

            {current.description && <p className="detail-desc">{current.description}</p>}

            <dl className="detail-grid">
                <div>
                    <dt>Priority</dt>
                    <dd><span className={`badge badge-${current.priority.toLowerCase()}`}>{current.priority}</span></dd>
                </div>
                <div>
                    <dt>Deadline</dt>
                    <dd className={overdue ? "overdue-text" : ""}>
                        {formatDate(current.deadline)}
                        {overdue ? " (overdue)" : ""}
                    </dd>
                </div>
                <div>
                    <dt>Assigned to</dt>
                    <dd>{current.assignedToName}</dd>
                </div>
                <div>
                    <dt>Created by</dt>
                    <dd>{current.createdByName}</dd>
                </div>
                <div>
                    <dt><label htmlFor="detail-status">Status</label></dt>
                    <dd>
                        <select id="detail-status" value={current.status} onChange={(e) => changeStatus(e.target.value)}>
                            {STATUSES.map((s) => (
                                <option key={s}>{s}</option>
                            ))}
                        </select>
                    </dd>
                </div>
            </dl>

            <section aria-labelledby="comments-heading">
                <h3 id="comments-heading">Comments</h3>

                {loading ? (
                    <p className="muted">Loading comments...</p>
                ) : comments.length === 0 ? (
                    <p className="muted">No comments yet. Start the conversation below.</p>
                ) : (
                    <ul className="comments" aria-live="polite">
                        {comments.map((c) => (
                            <li key={c.id} className="comment">
                                <div className="comment-head">
                                    <strong>{c.userName}</strong>
                                    <small>{formatDateTime(c.createdAt)}</small>
                                </div>
                                <p>{c.content}</p>
                            </li>
                        ))}
                    </ul>
                )}

                <form onSubmit={postComment} className="comment-form">
                    <label htmlFor="comment-input">Add a comment</label>
                    <textarea
                        id="comment-input"
                        value={content}
                        onChange={(e) => setContent(e.target.value)}
                        maxLength={1000}
                        placeholder="Write a comment..."
                    />
                    <div className="comment-foot">
                        <small className="muted">{content.length}/1000</small>
                        <button type="submit" className="btn btn-primary" disabled={posting || !content.trim()}>
                            {posting ? "Posting..." : "Post comment"}
                        </button>
                    </div>
                </form>
            </section>
        </Modal>
    );
}