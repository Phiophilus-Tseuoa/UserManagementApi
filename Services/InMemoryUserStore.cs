using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using UserManagementAPI.Models;

namespace UserManagementAPI.Services
{
    public class InMemoryUserStore : IUserStore
    {
        private readonly ConcurrentDictionary<int, User> _users = new();
        private readonly ConcurrentDictionary<string, int> _emailIndex =
            new(StringComparer.OrdinalIgnoreCase); // email -> userId
        private int _nextId = 0;

        public InMemoryUserStore()
        {
            // Seed
            TryCreate(new CreateUserRequest { FirstName = "Ada", LastName = "Lovelace", Email = "ada@example.com", IsActive = true }, out _, out _);
            TryCreate(new CreateUserRequest { FirstName = "Alan", LastName = "Turing", Email = "alan@example.com", IsActive = true }, out _, out _);
        }

        public IEnumerable<User> Query(out int totalCount, int pageNumber, int pageSize, bool? isActive, string? q)
        {
            var values = _users.Values.AsEnumerable();

            if (isActive.HasValue)
                values = values.Where(u => u.IsActive == isActive.Value);

            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim();
                values = values.Where(u =>
                    u.FirstName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    u.LastName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    u.Email.Contains(term, StringComparison.OrdinalIgnoreCase));
            }

            // Stable order
            values = values.OrderBy(u => u.Id);

            totalCount = values.Count();
            return values.Skip((pageNumber - 1) * pageSize)
                         .Take(pageSize)
                         .ToList(); // materialize snapshot
        }

        public bool TryGet(int id, out User? user) => _users.TryGetValue(id, out user);

        public bool TryCreate(CreateUserRequest req, out User created, out string? error)
        {
            created = default!;
            error = Validate(req.FirstName, req.LastName, req.Email, out var first, out var last, out var emailNorm);
            if (error is not null) return false;

            // Reserve ID
            var id = Interlocked.Increment(ref _nextId);

            // Deduplicate email atomically
            if (!_emailIndex.TryAdd(emailNorm, id))
            {
                error = "Email already exists.";
                return false;
            }

            var user = new User
            {
                Id = id,
                FirstName = first,
                LastName = last,
                Email = emailNorm,
                IsActive = req.IsActive,
                CreatedAt = DateTime.UtcNow
            };

            if (!_users.TryAdd(id, user))
            {
                // roll back email index if user add fails
                _emailIndex.TryRemove(emailNorm, out _);
                error = "Failed to create user.";
                return false;
            }

            created = user;
            return true;
        }

        public bool TryUpdate(int id, UpdateUserRequest req, out string? error)
        {
            error = Validate(req.FirstName, req.LastName, req.Email, out var first, out var last, out var emailNorm);
            if (error is not null) return false;

            if (!_users.TryGetValue(id, out var existing))
            {
                error = "User not found.";
                return false;
            }

            // Email change handling with index update
            var oldEmail = existing.Email;
            if (!oldEmail.Equals(emailNorm, StringComparison.OrdinalIgnoreCase))
            {
                // Ensure new email is free
                if (_emailIndex.ContainsKey(emailNorm))
                {
                    error = "Email already exists.";
                    return false;
                }

                // Update index: remove old, add new
                _emailIndex.TryRemove(oldEmail, out _);
                if (!_emailIndex.TryAdd(emailNorm, id))
                {
                    // try to revert old mapping if add fails
                    _emailIndex.TryAdd(oldEmail, id);
                    error = "Failed to update email.";
                    return false;
                }
            }

            // Apply updates
            var updated = new User
            {
                Id = existing.Id,
                FirstName = first,
                LastName = last,
                Email = emailNorm,
                IsActive = req.IsActive,
                CreatedAt = existing.CreatedAt
            };

            _users[id] = updated;
            return true;
        }

        public bool TryDelete(int id)
        {
            if (!_users.TryRemove(id, out var removed))
                return false;

            _emailIndex.TryRemove(removed.Email, out _);
            return true;
        }

        private static string? Validate(string firstName, string lastName, string email,
            out string first, out string last, out string normalizedEmail)
        {
            first = (firstName ?? string.Empty).Trim();
            last = (lastName ?? string.Empty).Trim();
            normalizedEmail = (email ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(first) || first.Length > 100) return "First name is required and must be <= 100 characters.";
            if (string.IsNullOrWhiteSpace(last) || last.Length > 100) return "Last name is required and must be <= 100 characters.";
            if (string.IsNullOrWhiteSpace(normalizedEmail) || normalizedEmail.Length > 256) return "Email is required and must be <= 256 characters.";

            // Quick format check (you still have [EmailAddress] on DTOs)
            try
            {
                var addr = new System.Net.Mail.MailAddress(normalizedEmail);
                normalizedEmail = addr.Address; // normalized
            }
            catch
            {
                return "Email format is invalid.";
            }

            return null;
        }
    }
}
