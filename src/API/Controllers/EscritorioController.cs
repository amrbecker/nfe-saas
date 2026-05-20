using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NfeSaas.Application.Commands.EscritorioCommands;
using NfeSaas.Application.DTOs;
using NfeSaas.Application.Queries;

namespace NfeSaas.API.Controllers;

[Route("api/escritorio")]
[ApiController]
public class EscritorioController : BaseApiController
{
    // Auto-cadastro público — qualquer pessoa pode criar um escritório.
    // Todo novo escritório recebe 30 dias de trial do plano escolhido.
    [HttpPost("registrar")]
    public async Task<IActionResult> Registrar([FromBody] CreateEscritorioDto dto)
    {
        var result = await Mediator.Send(new CreateEscritorioCommand(dto));
        if (result == null) return Conflict(new { message = "CNPJ já cadastrado ou plano inválido (escolha Básico, Profissional ou Enterprise)." });
        return Ok(result);
    }

    // Cadastra o próprio escritório como Empresa emitente. Idempotente — se o CNPJ do
    // escritório já existe como empresa nele, retorna a existente.
    [Authorize(Roles = "Admin")]
    [HttpPost("cadastrar-como-empresa")]
    public async Task<IActionResult> CadastrarEscritorioComoEmpresa([FromBody] CadastrarEscritorioComoEmpresaDto dto)
    {
        var cmd = new CadastrarEscritorioComoEmpresaCommand(
            EscritorioId,
            dto.InscricaoEstadual,
            dto.Logradouro, dto.Numero, dto.Bairro, dto.Cidade, dto.Uf,
            dto.Cep, dto.CodigoMunicipio,
            dto.RegimeTributario, dto.AmbienteSefaz, dto.Cnae);
        var result = await Mediator.Send(cmd);
        if (result == null)
            return BadRequest(new { message = "Não foi possível cadastrar o escritório como empresa (verifique IE, UF, CEP, CNAE)." });
        return Ok(result);
    }

    // Ativa o plano pago do próprio escritório (admin do escritório).
    // Em produção, esta rota deve ser substituída por webhook do gateway de pagamento.
    [Authorize(Roles = "Admin")]
    [HttpPost("ativar-plano")]
    public async Task<IActionResult> AtivarPlano([FromBody] AtivarPlanoPagoDto dto)
    {
        var ok = await Mediator.Send(new AtivarPlanoPagoCommand(EscritorioId, dto.AtivoAteUtc, dto.ValorPago));
        if (!ok) return BadRequest(new { message = "Não foi possível ativar o plano (data inválida ou escritório não encontrado)." });
        return NoContent();
    }

    // === EMPRESAS ===

    [Authorize]
    [HttpGet("empresas")]
    public async Task<IActionResult> GetEmpresas()
    {
        var empresas = await Mediator.Send(new GetEmpresasQuery(EscritorioId));
        return Ok(empresas);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("empresas")]
    public async Task<IActionResult> CriarEmpresa([FromBody] CreateEmpresaDto dto)
    {
        var result = await Mediator.Send(new CreateEmpresaCommand(EscritorioId, dto));
        if (result == null) return BadRequest(new { message = "Dados inválidos ou escritório não encontrado." });
        return Ok(result);
    }

    // === USUÁRIOS ===

    [Authorize(Roles = "Admin")]
    [HttpGet("usuarios")]
    public async Task<IActionResult> GetUsuarios()
    {
        var usuarios = await Mediator.Send(new GetUsuariosQuery(EscritorioId));
        return Ok(usuarios);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("usuarios")]
    public async Task<IActionResult> CriarUsuario([FromBody] CreateUsuarioDto dto)
    {
        var result = await Mediator.Send(new CreateUsuarioCommand(EscritorioId, dto));
        if (result == null) return Conflict(new { message = "E-mail já cadastrado." });
        return Ok(result);
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("usuarios/{id:guid}")]
    public async Task<IActionResult> AtualizarUsuario(Guid id, [FromBody] UpdateUsuarioDto dto)
    {
        var result = await Mediator.Send(new UpdateUsuarioCommand(EscritorioId, id, dto));
        if (result == null) return NotFound();
        return Ok(result);
    }

    [Authorize(Roles = "Admin")]
    [HttpPatch("usuarios/{id:guid}/toggle-ativo")]
    public async Task<IActionResult> ToggleAtivoUsuario(Guid id)
    {
        var result = await Mediator.Send(new ToggleAtivoUsuarioCommand(EscritorioId, id));
        if (result == null) return NotFound();
        return Ok(result);
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("usuarios/{id:guid}")]
    public async Task<IActionResult> ExcluirUsuario(Guid id)
    {
        var ok = await Mediator.Send(new DeleteUsuarioCommand(EscritorioId, id));
        if (!ok) return NotFound();
        return NoContent();
    }
}
