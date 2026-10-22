import { useCallback, useEffect, useState } from "react";
import client, { getErrorMessage } from "../api/client";
import { useAuth } from "../hooks/useAuth";

const ROLES = ["Admin", "Manager", "User"];

export default function TeamsPage() {
    const { user } = useAuth();
    const isAdmin = user.role === "Admin";

    const [teams, setTeams] = useState([]);
    const [users, setUsers] = useState([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState("");
    const [toast, setToast] = useState("");
    const [newTeam, setNewTeam] = useState({ name: "", managerId: "" });
    const [editing, setEditing] = useState(null);
    const [memberChoice, setMemberChoice] = useState({});

    const fetchAll = useCallback(async () => {
        const [teamsResponse, usersResponse] = await Promise.all([
            client.get("/Teams"),
            client.get("/Users"),
        ]);
        return { teams: teamsResponse.data, users: usersResponse.data };
    }, []);

    useEffect(() => {
        let active = true;

        fetchAll()
            .then((data) => {
                if (active) {
                    setTeams(data.teams);
                    setUsers(data.users);
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
    }, [fetchAll]);

    useEffect(() => {
        if (!toast) return undefined;
        const timer = setTimeout(() => setToast(""), 4000);
        return () => clearTimeout(timer);
    }, [toast]);

    // Runs an API action, then reloads the page data.
    const run = async (action, successMessage) => {
        try {
            await action();
            const data = await fetchAll();
            setTeams(data.teams);
            setUsers(data.users);
            setError("");
            setToast(successMessage);
            return true;
        } catch (err) {
            setError(getErrorMessage(err));
            return false;
        }
    };

    const managers = users.filter((u) => u.role === "Manager");

    const createTeam = async (event) => {
        event.preventDefault();
        const ok = await run(
            () =>
                client.post("/Teams", {
                    name: newTeam.name.trim(),
                    managerId: newTeam.managerId ? Number(newTeam.managerId) : null,
                }),
            "Team created."
        );
        if (ok) setNewTeam({ name: "", managerId: "" });
    };

    const saveEdit = async (event) => {
        event.preventDefault();
        const ok = await run(
            () =>
                client.put(`/Teams/${editing.id}`, {
                    name: editing.name.trim(),
                    managerId: editing.managerId ? Number(editing.managerId) : null,
                }),
            "Team updated."
        );
        if (ok) setEditing(null);
    };

    const deleteTeam = (team) => {
        if (!window.confirm(`Delete the team "${team.name}"? Its members will be left without a team.`)) {
            return;
        }
        run(() => client.delete(`/Teams/${team.id}`), "Team deleted.");
    };

    const addMember = async (team) => {
        const userId = memberChoice[team.id];
        if (!userId) return;

        const ok = await run(
            () => client.post(`/Teams/${team.id}/members`, { userId: Number(userId) }),
            "Member added."
        );
        if (ok) setMemberChoice({ ...memberChoice, [team.id]: "" });
    };

    const removeMember = (team, member) =>
        run(() => client.delete(`/Teams/${team.id}/members/${member.id}`), "Member removed.");

    const changeRole = (target, role) =>
        run(() => client.patch(`/Users/${target.id}/role`, { role }), `${target.fullName} is now ${role}.`);

    const addableUsers = (team) =>
        users.filter((u) => u.role !== "Admin" && u.teamId !== team.id);

    return (
        <>
            <div className="page-head">
                <div>
                    <h1 className="page-title">Teams</h1>
                    <p className="muted">
                        {isAdmin ? "Create teams, assign managers and manage roles" : "Manage the members of your team"}
                    </p>
                </div>
            </div>

            {toast && <div className="alert alert-success" role="status">{toast}</div>}
            {error && <div className="alert alert-error" role="alert">{error}</div>}

            {isAdmin && (
                <section className="card" aria-labelledby="new-team-heading">
                    <h2 id="new-team-heading" className="section-title">Create a team</h2>
                    <form className="inline-form" onSubmit={createTeam}>
                        <div className="field">
                            <label htmlFor="team-name">Team name</label>
                            <input
                                id="team-name"
                                value={newTeam.name}
                                onChange={(e) => setNewTeam({ ...newTeam, name: e.target.value })}
                                maxLength={100}
                                required
                            />
                        </div>
                        <div className="field">
                            <label htmlFor="team-manager">Manager</label>
                            <select
                                id="team-manager"
                                value={newTeam.managerId}
                                onChange={(e) => setNewTeam({ ...newTeam, managerId: e.target.value })}
                            >
                                <option value="">No manager yet</option>
                                {managers.map((m) => (
                                    <option key={m.id} value={m.id}>
                                        {m.fullName}
                                    </option>
                                ))}
                            </select>
                        </div>
                        <div className="field">
                            <button type="submit" className="btn btn-primary">Create team</button>
                        </div>
                    </form>
                </section>
            )}

            {loading ? (
                <p className="muted">Loading teams...</p>
            ) : teams.length === 0 ? (
                <div className="card empty">
                    <p>No teams to show.</p>
                    <p className="muted">
                        {isAdmin ? "Create the first team above." : "An admin needs to assign you as a team manager."}
                    </p>
                </div>
            ) : (
                <div className="team-grid">
                    {teams.map((team) => {
                        const isEditing = editing?.id === team.id;
                        const choices = addableUsers(team);

                        return (
                            <article key={team.id} className="card team-card">
                                {isEditing ? (
                                    <form className="edit-form" onSubmit={saveEdit}>
                                        <div className="field">
                                            <label htmlFor={`edit-name-${team.id}`}>Team name</label>
                                            <input
                                                id={`edit-name-${team.id}`}
                                                value={editing.name}
                                                onChange={(e) => setEditing({ ...editing, name: e.target.value })}
                                                maxLength={100}
                                                required
                                            />
                                        </div>
                                        <div className="field">
                                            <label htmlFor={`edit-manager-${team.id}`}>Manager</label>
                                            <select
                                                id={`edit-manager-${team.id}`}
                                                value={editing.managerId}
                                                onChange={(e) => setEditing({ ...editing, managerId: e.target.value })}
                                            >
                                                <option value="">No manager</option>
                                                {managers.map((m) => (
                                                    <option key={m.id} value={m.id}>
                                                        {m.fullName}
                                                    </option>
                                                ))}
                                            </select>
                                        </div>
                                        <div className="row-actions">
                                            <button type="submit" className="btn btn-primary btn-sm">Save</button>
                                            <button type="button" className="btn btn-secondary btn-sm" onClick={() => setEditing(null)}>
                                                Cancel
                                            </button>
                                        </div>
                                    </form>
                                ) : (
                                    <div className="team-head">
                                        <div>
                                            <h2 className="team-name">{team.name}</h2>
                                            <p className="muted">Manager: {team.managerName || "Not assigned"}</p>
                                        </div>
                                        {isAdmin && (
                                            <div className="row-actions">
                                                <button
                                                    type="button"
                                                    className="btn btn-secondary btn-sm"
                                                    onClick={() =>
                                                        setEditing({
                                                            id: team.id,
                                                            name: team.name,
                                                            managerId: team.managerId ? String(team.managerId) : "",
                                                        })
                                                    }
                                                >
                                                    Edit
                                                </button>
                                                <button type="button" className="btn btn-danger btn-sm" onClick={() => deleteTeam(team)}>
                                                    Delete
                                                </button>
                                            </div>
                                        )}
                                    </div>
                                )}

                                <h3 className="members-title">Members ({team.members.length})</h3>
                                {team.members.length === 0 ? (
                                    <p className="muted">No members yet.</p>
                                ) : (
                                    <ul className="member-list">
                                        {team.members.map((member) => (
                                            <li key={member.id} className="member">
                                                <div>
                                                    <strong>{member.fullName}</strong>
                                                    <small>
                                                        {member.role} &middot; {member.email}
                                                    </small>
                                                </div>
                                                <button
                                                    type="button"
                                                    className="btn btn-secondary btn-sm"
                                                    onClick={() => removeMember(team, member)}
                                                    aria-label={`Remove ${member.fullName} from ${team.name}`}
                                                >
                                                    Remove
                                                </button>
                                            </li>
                                        ))}
                                    </ul>
                                )}

                                <div className="add-member">
                                    <label htmlFor={`add-${team.id}`}>Add a member</label>
                                    <div className="add-member-row">
                                        <select
                                            id={`add-${team.id}`}
                                            value={memberChoice[team.id] || ""}
                                            onChange={(e) => setMemberChoice({ ...memberChoice, [team.id]: e.target.value })}
                                        >
                                            <option value="">Select a user</option>
                                            {choices.map((u) => (
                                                <option key={u.id} value={u.id}>
                                                    {u.fullName}
                                                    {u.teamName ? ` (in ${u.teamName})` : ""}
                                                </option>
                                            ))}
                                        </select>
                                        <button
                                            type="button"
                                            className="btn btn-primary"
                                            onClick={() => addMember(team)}
                                            disabled={!memberChoice[team.id]}
                                        >
                                            Add
                                        </button>
                                    </div>
                                </div>
                            </article>
                        );
                    })}
                </div>
            )}

            {isAdmin && users.length > 0 && (
                <section className="card" aria-labelledby="roles-heading">
                    <h2 id="roles-heading" className="section-title">Users and roles</h2>
                    <p className="muted">Role changes apply the next time the user logs in.</p>
                    <div className="table-wrap">
                        <table className="data">
                            <thead>
                                <tr>
                                    <th scope="col">Name</th>
                                    <th scope="col">Email</th>
                                    <th scope="col">Team</th>
                                    <th scope="col">Role</th>
                                </tr>
                            </thead>
                            <tbody>
                                {users.map((u) => (
                                    <tr key={u.id}>
                                        <th scope="row">{u.fullName}</th>
                                        <td>{u.email}</td>
                                        <td>{u.teamName || "None"}</td>
                                        <td>
                                            <label className="sr-only" htmlFor={`role-${u.id}`}>
                                                Role for {u.fullName}
                                            </label>
                                            <select
                                                id={`role-${u.id}`}
                                                value={u.role}
                                                disabled={u.id === user.id}
                                                onChange={(e) => changeRole(u, e.target.value)}
                                            >
                                                {ROLES.map((r) => (
                                                    <option key={r}>{r}</option>
                                                ))}
                                            </select>
                                        </td>
                                    </tr>
                                ))}
                            </tbody>
                        </table>
                    </div>
                </section>
            )}
        </>
    );
}