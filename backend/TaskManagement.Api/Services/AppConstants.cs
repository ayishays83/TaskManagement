namespace TaskManagement.Api.Services
{
    public static class AppRoles
    {
        public const string Admin = "Admin";
        public const string Manager = "Manager";
        public const string User = "User";
        public const string AdminOrManager = Admin + "," + Manager;

        public static readonly string[] All = { Admin, Manager, User };
    }

    public static class TaskStatuses
    {
        public const string ToDo = "To Do";
        public const string InProgress = "In Progress";
        public const string Done = "Done";

        public static readonly string[] All = { ToDo, InProgress, Done };
    }

    public static class TaskPriorities
    {
        public const string Low = "Low";
        public const string Medium = "Medium";
        public const string High = "High";

        public static readonly string[] All = { Low, Medium, High };
    }
}