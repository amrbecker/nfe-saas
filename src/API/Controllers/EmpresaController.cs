using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
            empresa.RegimeTributario,
            empresa.AmbienteSefaz,
            empresa.UltimoNumeronFe,
            empresa.UltimoNumeronFCe,
            certificadoValidade = empresa.CertificadoValidade,
            certificadoCnpj = empresa.CertificadoCnpj,
            certificadoValido = empresa.CertificadoValido()
        });
    }

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

    [HttpPost("certificado/upload")]
    public async Task<IActionResult> UploadCertificado(IFormFile arquivo, [FromForm] string senha)
    {
        if (arquivo == null || arquivo.Length == 0)
            return BadRequest(new { message = "Arquivo não enviado." });

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
}
