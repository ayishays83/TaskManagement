import { BrowserRouter, Navigate, Route, Routes } from "react-router-dom";
import Layout from "./components/Layout";
import ProtectedRoute from "./components/ProtectedRoute";
import AuthProvider from "./context/AuthProvider";
import DashboardPage from "./pages/DashboardPage";
import LoginPage from "./pages/LoginPage";
import TeamsPage from "./pages/TeamsPage";

export default function App() {
    return (
        <BrowserRouter>
            <AuthProvider>
                <Routes>
                    <Route path="/login" element={<LoginPage />} />

                    <Route element={<ProtectedRoute />}>
                        <Route element={<Layout />}>
                            <Route index element={<DashboardPage />} />
                            <Route element={<ProtectedRoute roles={["Admin", "Manager"]} />}>
                                <Route path="teams" element={<TeamsPage />} />
                            </Route>
                        </Route>
                    </Route>

                    <Route path="*" element={<Navigate to="/" replace />} />
                </Routes>
            </AuthProvider>
        </BrowserRouter>
    );
}