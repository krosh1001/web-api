using System.Data;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualBasic.FileIO;
using WebApi.MinimalApi.Domain;
using WebApi.MinimalApi.Models;
namespace WebApi.MinimalApi.Controllers;



[Route("api/[controller]")]
[ApiController]
public class UsersController : Controller
{
    private readonly IUserRepository userRepository;
    private readonly IMapper mapper;

    public UsersController(IUserRepository userRepository, IMapper mapper)
    {
        this.userRepository = userRepository;
        this.mapper = mapper;
    }

    [HttpGet("{userId}", Name = nameof(GetUserById))]
    [Produces("application/json", "application/xml")]
    public ActionResult<UserDto> GetUserById([FromRoute] Guid userId)
    {
        var user = userRepository.FindById(userId);
        if (user is null)
            return NotFound();
        var dto = mapper.Map<UserDto>(user);
    
        return Ok(dto);
    }
    
    [HttpPost]
    public IActionResult CreateUser([FromBody] UserToPostDto data)
    {
        if (data is null)
        {
            return BadRequest();
        }

        // Проверяем, что логин состоит только из букв и цифр
        if (data.Login != null && !data.Login.All(char.IsLetterOrDigit))
        {
            ModelState.AddModelError("login", "Login must contain only letters and digits");
        }

        if (!ModelState.IsValid)
        {
            return UnprocessableEntity(ModelState);
        }

        var entity = mapper.Map<UserEntity>(data);
        var createdUser = userRepository.Insert(entity);

        return CreatedAtRoute(
            nameof(GetUserById),
            new { userId = createdUser.Id },
            mapper.Map<UserDto>(createdUser));
    }
}

