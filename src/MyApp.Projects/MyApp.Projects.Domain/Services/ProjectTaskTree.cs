using MyApp.Projects.Domain.Entities;

namespace MyApp.Projects.Domain.Services;

/// <summary>Pure helpers for the parent/child task tree of a project.</summary>
public static class ProjectTaskTree
{
    /// <summary>
    /// Returns <see langword="true"/> when assigning <paramref name="newParentId"/> as parent of
    /// <paramref name="taskId"/> would create a cycle (the new parent is the task itself or one of its descendants).
    /// </summary>
    /// <param name="tasks">All tasks of the project.</param>
    /// <param name="taskId">The task being moved.</param>
    /// <param name="newParentId">The candidate parent.</param>
    public static bool WouldCreateCycle(IEnumerable<ProjectTask> tasks, Guid taskId, Guid? newParentId)
    {
        if (newParentId is null)
            return false;
        if (newParentId.Value == taskId)
            return true;

        var parentById = tasks.ToDictionary(t => t.Id, t => t.ParentTaskId);
        var current = newParentId;
        var guard = 0;
        while (current.HasValue && guard++ <= parentById.Count)
        {
            if (current.Value == taskId)
                return true;
            current = parentById.TryGetValue(current.Value, out var parent) ? parent : null;
        }

        return false;
    }

    /// <summary>Returns the ids of all descendants of <paramref name="taskId"/> (excluding itself).</summary>
    /// <param name="tasks">All tasks of the project.</param>
    /// <param name="taskId">The root task.</param>
    public static IReadOnlyCollection<Guid> GetDescendantIds(IEnumerable<ProjectTask> tasks, Guid taskId)
    {
        var childrenByParent = tasks
            .Where(t => t.ParentTaskId.HasValue)
            .GroupBy(t => t.ParentTaskId!.Value)
            .ToDictionary(g => g.Key, g => g.Select(t => t.Id).ToList());

        var result = new List<Guid>();
        var stack = new Stack<Guid>();
        stack.Push(taskId);
        var visited = new HashSet<Guid> { taskId };
        while (stack.Count > 0)
        {
            var id = stack.Pop();
            if (!childrenByParent.TryGetValue(id, out var children))
                continue;
            foreach (var child in children.Where(visited.Add))
            {
                result.Add(child);
                stack.Push(child);
            }
        }

        return result;
    }

    /// <summary>Returns <see langword="true"/> when the task has at least one child.</summary>
    /// <param name="tasks">All tasks of the project.</param>
    /// <param name="taskId">The task.</param>
    public static bool HasChildren(IEnumerable<ProjectTask> tasks, Guid taskId) =>
        tasks.Any(t => t.ParentTaskId == taskId);

    /// <summary>
    /// Rolls values up the tree: the result for each task is its own value plus the values of all descendants.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="tasks">All tasks of the project.</param>
    /// <param name="ownValues">The task's own values (missing = zero).</param>
    /// <param name="add">The addition operator.</param>
    /// <param name="zero">The zero value factory.</param>
    public static Dictionary<Guid, T> RollUp<T>(
        IReadOnlyCollection<ProjectTask> tasks,
        IReadOnlyDictionary<Guid, T> ownValues,
        Func<T, T, T> add,
        Func<T> zero)
    {
        var childrenByParent = tasks
            .Where(t => t.ParentTaskId.HasValue)
            .GroupBy(t => t.ParentTaskId!.Value)
            .ToDictionary(g => g.Key, g => g.Select(t => t.Id).ToList());

        var memo = new Dictionary<Guid, T>();

        T Compute(Guid id, HashSet<Guid> path)
        {
            if (memo.TryGetValue(id, out var cached))
                return cached;
            if (!path.Add(id))
                throw new InvalidOperationException("Cycle detected in project task tree.");

            var total = ownValues.TryGetValue(id, out var own) ? own : zero();
            if (childrenByParent.TryGetValue(id, out var children))
            {
                foreach (var child in children)
                    total = add(total, Compute(child, path));
            }

            path.Remove(id);
            memo[id] = total;
            return total;
        }

        foreach (var task in tasks)
            Compute(task.Id, new HashSet<Guid>());

        return memo;
    }
}
