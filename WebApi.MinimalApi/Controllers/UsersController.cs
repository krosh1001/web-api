using System.Data;
using AutoMapper;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
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
    [HttpHead("{userId}")]
    [Produces("application/json", "application/xml")]
    public ActionResult<UserDto> GetUserById([FromRoute] Guid userId)
    {
        var user = userRepository.FindById(userId);
        if (user is null)
            return NotFound();

        // Поддержка HEAD-запросов (без возврата тела ответа)
        if (HttpMethods.IsHead(Request.Method))
            return Ok();

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

    [HttpPut("{userId}")]
    public IActionResult UpdateUser([FromRoute] Guid userId, [FromBody] UserToPutDto data)
    {
        if (data is null)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            if (ModelState.ContainsKey(nameof(userId)))
            {
                return BadRequest();
            }

            return UnprocessableEntity(ModelState);
        }

        var entity = mapper.Map(data, new UserEntity(userId));
        userRepository.UpdateOrInsert(entity, out var isInserted);

        if (isInserted)
        {
            return CreatedAtRoute(nameof(GetUserById), new { userId = entity.Id }, entity.Id);
        }

        return NoContent();
    }

    [HttpPatch("{userId}")]
    public IActionResult PartiallyUpdateUser([FromRoute] Guid userId,
        [FromBody] JsonPatchDocument<UserToPutDto> patchDoc)
    {
        if (patchDoc is null)
        {
            return BadRequest();
        }

        var user = userRepository.FindById(userId);
        if (user is null)
        {
            return NotFound();
        }

        var data = new UserToPutDto
        {
            Login = user.Login,
            FirstName = user.FirstName,
            LastName = user.LastName
        };

        patchDoc.ApplyTo(data, ModelState);
        TryValidateModel(data);

        if (!ModelState.IsValid)
        {
            return UnprocessableEntity(ModelState);
        }

        mapper.Map(data, user);
        userRepository.Update(user);

        return NoContent();
    }

    [HttpDelete("{userId}")]
    public IActionResult DeleteUser([FromRoute] Guid userId)
    {
        var user = userRepository.FindById(userId);
        if (user is null)
        {
            return NotFound();
        }

        userRepository.Delete(userId);
        return NoContent();
    }
}