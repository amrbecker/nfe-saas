using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NfeSaas.Application.Commands.ConfiguracaoEmpresaCommands;
using NfeSaas.Application.Commands.EmpresaCommands;
using NfeSaas.Application.DTOs;
using NfeSaas.Application.Interfaces;
using NfeSaas.Domain.Interfaces;

namespace NfeSaas.API.Controllers;

[Authorize]
[Route("api/empresa")]
public class EmpresaController : BaseApiController
{
    private readonly IEmpresaRepository _empresaRepo;
    private readonly ICertificadoService _certService;
    private readonly IUnitOfWork _uow;

    public EmpresaController(IEmpresaRepository empresaRepo, ICertificadoService certService, IUnitOfWork uow)
    {
        _empresaRepo = empresaRepo;
        _certService = certService;
        _uow = uow;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var empresa = await _empresaRepo.GetByIdAsync(EmpresaId);
        if (empresa == null) return NotFound();

        return Ok(new
        {
            empresa.Id,
            empresa.RazaoSocial,
            empresa.NomeFantasia,
            empresa.Cnpj,
            empresa.InscricaoEstadual,
            empresa.Email,
            empresa.Telefone,
            empresa.Logradouro,
            empresa.Numero,
            empresa.Bairro,
            empresa.Cidade,
            empresa.Uf,
            empresa.Cep,
            empresa.Cnae,
            empresa.CodigoMunicipio,
            empresa.RegimeTributario,
            empresa.AmbienteSefaz,
            empresa.UltimoNumeronFe,
            empresa.UltimoNumeronFCe,
            certificadoValidade = empresa.CertificadoValidade,
            certificadoCnpj = empresa.CertificadoCnpj,
            certificadoValido = empresa.CertificadoValido(),
            cscId = empresa.CscId,
            temCscToken = !string.IsNullOrEmpty(empresa.CscToken)
        });
    }

    [Authorize(Roles = "Admin")]
    [HttpPut]
    public async Task<IActionResult> Atualizar([FromBody] UpdateEmpresaDto dto)
    {
        var result = await Mediator.Send(new UpdateEmpresaCommand(EmpresaId, dto));
        if (!result.Sucesso) return BadRequest(new { message = result.Erro });
        return NoContent();
    }

    [HttpGet("/api/diagnostics/xsd")]
    [AllowAnonymous]
    public IActionResult GetXsdStatus([FromServices] IXsdValidationService xsd) =>
        Ok(new
        {
            xsd.TemSchemasCarregados,
            xsd.TotalSchemasCarregados,
            xsd.ErrosCarga
        });

    [HttpGet("certificado/status")]
    public async Task<IActionResult> GetCertificadoStatus()
    {
        var empresa = await _empresaRepo.GetByIdAsync(EmpresaId);
        if (empresa == null) return NotFound();

        if (empresa.CertificadoBytes == null)
            return Ok(new CertificadoStatusDto(false, null, null, null, "Nenhum certificado configurado."));

        var info = _certService.ValidarCertificado(empresa.CertificadoBytes, empresa.CertificadoSenha!);
        return Ok(new CertificadoStatusDto(info.Valido, info.NomeTitular, info.Cnpj, info.Validade, info.MensagemErro));
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("certificado/upload")]
    [RequestSizeLimit(256 * 1024)] // PFX A1 raramente passa de ~10 KB; 256 KB cobre A3 com folga.
    public async Task<IActionResult> UploadCertificado(IFormFile arquivo, [FromForm] string senha)
    {
        if (arquivo == null || arquivo.Length == 0)
            return BadRequest(new { message = "Arquivo não enviado." });

        if (arquivo.Length > 256 * 1024)
            return BadRequest(new { message = "Arquivo excede o tamanho máximo permitido (256 KB)." });

        if (!arquivo.FileName.EndsWith(".pfx", StringComparison.OrdinalIgnoreCase) &&
            !arquivo.FileName.EndsWith(".p12", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { message = "Apenas arquivos .pfx ou .p12 são aceitos." });

        using var ms = new MemoryStream();
        await arquivo.CopyToAsync(ms);
        var bytes = ms.ToArray();

        var info = _certService.ValidarCertificado(bytes, senha);
        if (!info.Valido)
            return BadRequest(new { message = info.MensagemErro ?? "Certificado inválido ou senha incorreta." });

        var empresa = await _empresaRepo.GetByIdAsync(EmpresaId);
        if (empresa == null) return NotFound();

        empresa.AtualizarCertificado(bytes, senha, info.Validade, info.Cnpj ?? empresa.Cnpj);
        await _empresaRepo.UpdateAsync(empresa);
        await _uow.SaveChangesAsync();

        return Ok(new CertificadoStatusDto(true, info.NomeTitular, info.Cnpj, info.Validade, null));
    }

    [HttpGet("configuracao")]
    public async Task<IActionResult> GetConfiguracao()
    {
        var result = await Mediator.Send(new GetConfiguracaoEmpresaQuery(EmpresaId));
        if (result == null) return NoContent();
        return Ok(result);
    }

    [HttpPost("configuracao")]
    public async Task<IActionResult> SalvarConfiguracao([FromBody] ConfiguracaoEmpresaDto dto)
    {
        var result = await Mediator.Send(new SalvarConfiguracaoEmpresaCommand(EmpresaId, dto));
        if (result == null) return BadRequest(new { message = "Não foi possível salvar a configuração." });
        return Ok(result);
    }
}
