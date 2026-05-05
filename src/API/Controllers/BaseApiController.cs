using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace NfeSaas.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public abstract class BaseApiController : ControllerBase
{
    protected IMediator Mediator => HttpContext.RequestServices.GetRequiredService<IMediator>();

    protected Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    protected Guid EmpresaId => Guid.Parse(User.FindFirstValue("empresa_id")!);
    protected string UserRole => User.FindFirstValue(ClaimTypes.Role) ?? "User";
}
