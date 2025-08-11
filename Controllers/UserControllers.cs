using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using UserManagementAPI.Models;
using UserManagementAPI.Services;

namespace UserManagementAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly IUserStore _store;
        private readonly ILogger<UsersController> _logger;

        public UsersController(IUserStore store, ILogger<UsersController> logger)
        {
            _store = store;
            _logger = logger;
        }

        // GET: api/users?PageNumber=1&PageSize=20&isActive=true&q=ada
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<User>), StatusCodes.Status200OK)]
        public ActionResult<IEnumerable<User>> GetAll(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] bool? isActive = null,
            [FromQuery] string? q = null)
        {
            try
            {
                pageNumber = Math.Max(1, pageNumber);
                pageSize = Math.Clamp(pageSize, 1, 100);

                var users = _store.Query(out var total, pageNumber, pageSize, isActive, q);
                Response.Headers["X-Total-Count"] = total.ToString();
                Response.Headers["X-Page-Number"] = pageNumber.ToString();
                Response.Headers["X-Page-Size"] = pageSize.ToString();

                return Ok(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get users. page={Page} size={Size} isActive={IsActive} q={Q}",
                    pageNumber, pageSize, isActive, q);
                return Problem("Failed to retrieve users.");
            }
        }

        // GET: api/users/{id}
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(User), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult<User> GetById(int id)
        {
            try
            {
                if (_store.TryGet(id, out var user) && user is not null)
                    return Ok(user);

                return NotFound(new { message = "User not found." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get user {Id}", id);
                return Problem("Failed to retrieve user.");
            }
        }

        // POST: api/users
        [HttpPost]
        [ProducesResponseType(typeof(User), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public ActionResult<User> Create([FromBody] CreateUserRequest request)
        {
            try
            {
                if (!ModelState.IsValid) return ValidationProblem(ModelState);

                if (_store.TryCreate(request, out var created, out var error))
                    return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);

                if (error == "Email already exists.")
                    return Conflict(new { message = error });

                return BadRequest(new { message = error ?? "Failed to create user." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create user.");
                return Problem("Failed to create user.");
            }
        }

        // PUT: api/users/{id}
        [HttpPut("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public IActionResult Update(int id, [FromBody] UpdateUserRequest request)
        {
            try
            {
                if (!ModelState.IsValid) return ValidationProblem(ModelState);

                var ok = _store.TryUpdate(id, request, out var error);
                if (ok) return NoContent();

                if (error == "User not found.") return NotFound(new { message = error });
                if (error == "Email already exists.") return Conflict(new { message = error });

                return BadRequest(new { message = error ?? "Failed to update user." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update user {Id}", id);
                return Problem("Failed to update user.");
            }
        }

        // DELETE: api/users/{id}
        [HttpDelete("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult Delete(int id)
        {
            try
            {
                if (_store.TryDelete(id))
                    return NoContent();

                return NotFound(new { message = "User not found." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete user {Id}", id);
                return Problem("Failed to delete user.");
            }
        }
    }
}
