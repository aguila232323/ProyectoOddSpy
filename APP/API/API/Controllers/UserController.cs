using API.Data;
using API.Models.Entities;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly AppDbContext dbContext;

        public UserController(AppDbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        [HttpGet]
        public IActionResult GetAllUsers()
        {
            return Ok(dbContext.Users.ToList());
        }

        [HttpGet("{id:guid}")]
        public IActionResult GetUser(Guid id)
        {
            var user = dbContext.Users.Find(id);
            if (user == null)
            {
                return NotFound();
            }

            return Ok(user);
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequestDTO loginRequest)
        {
            var user = dbContext.Users.FirstOrDefault(u => u.Email == loginRequest.Email);
            if (user == null)
            {
                return Unauthorized("Usuario no encontrado.");
            }

            if (user.Password != loginRequest.Password)
            {
                return Unauthorized("Contraseña incorrecta.");
            }

            return Ok(new
            {
                message = "Login exitoso",
                user = new
                {
                    user.Id,
                    user.Username,
                    user.Email
                }
            });
        }

        [HttpPost]
        public IActionResult AddUser(AddUserDTO addUserDTO)
        {
            var user = new User()
            {
                Email = addUserDTO.Email,
                Username = addUserDTO.Username,
                Password = addUserDTO.Password,
            };

            dbContext.Users.Add(user);
            dbContext.SaveChanges();

            return Ok(user);
        }
    }
}
